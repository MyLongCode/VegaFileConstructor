using Microsoft.EntityFrameworkCore;
using VegaFileConstructor.Data;
using VegaFileConstructor.Models;

namespace VegaFileConstructor.Services;

public class GenerationWorkflowService(
    ApplicationDbContext db,
    IPdfGeneratorService pdfGenerator,
    IWebHostEnvironment env) : IGenerationWorkflowService
{
    public async Task<Generation> CreateAndGenerateAsync(string userId, Guid templateId, Dictionary<string, string> values)
    {
        var template = await db.DocumentTemplates
            .Include(t => t.FieldPlacements)
            .Include(t => t.FieldDefinitions)
            .FirstAsync(t => t.Id == templateId);

        var generation = new Generation
        {
            UserId = userId,
            TemplateId = templateId,
            Status = GenerationStatus.Created,
            FieldValues = values.Select(v => new GenerationFieldValue { FieldKey = v.Key, Value = v.Value }).ToList()
        };

        db.Generations.Add(generation);
        await db.SaveChangesAsync();

        try
        {
            var outputRel = Path.Combine("generated", userId, $"{generation.Id}.pdf").Replace('\\', '/');
            var outputAbs = Path.Combine(env.WebRootPath, outputRel.Replace('/', Path.DirectorySeparatorChar));
            await pdfGenerator.GenerateAsync(template, template.FieldPlacements, values, outputAbs);
            generation.OutputFilePath = outputRel;
            generation.Status = GenerationStatus.Generated;
        }
        catch (Exception ex)
        {
            generation.Status = GenerationStatus.Failed;
            generation.ErrorMessage = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
        }

        await db.SaveChangesAsync();
        return generation;
    }
}
