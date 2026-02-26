using VegaFileConstructor.Models;

namespace VegaFileConstructor.Services;

public interface IPdfGeneratorService
{
    Task<string> GenerateAsync(DocumentTemplate template, IEnumerable<TemplateFieldPlacement> placements, Dictionary<string, string> values, string outputPath);
}
