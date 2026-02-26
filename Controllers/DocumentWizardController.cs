using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VegaFileConstructor.Data;
using VegaFileConstructor.Models;
using VegaFileConstructor.Services;
using VegaFileConstructor.ViewModels;

namespace VegaFileConstructor.Controllers;

[Authorize]
public class DocumentWizardController(ApplicationDbContext db, IGenerationWorkflowService workflowService) : Controller
{
    public async Task<IActionResult> Step1(string? search, bool activeOnly = true)
    {
        var query = db.DocumentTemplates.AsQueryable();
        if (activeOnly) query = query.Where(t => t.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(t => t.Name.Contains(search));

        var vm = new TemplateSelectionVm
        {
            Search = search,
            ActiveOnly = activeOnly,
            Templates = await query.OrderBy(t => t.Name).ToListAsync()
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Step2(Guid templateId)
    {
        var template = await db.DocumentTemplates
            .Include(t => t.FieldDefinitions)
            .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive);
        if (template == null) return NotFound();

        var vm = new WizardStep2Vm
        {
            TemplateId = template.Id,
            TemplateName = template.Name,
            TemplateVersion = template.Version,
            Fields = template.FieldDefinitions.OrderBy(f => f.Order).Select(f => new DynamicFieldInputVm
            {
                Key = f.Key,
                Label = f.Label,
                Group = f.Group,
                HelpText = f.HelpText,
                Placeholder = f.Placeholder,
                IsRequired = f.IsRequired,
                MinLength = f.MinLength,
                MaxLength = f.MaxLength,
                DataType = f.DataType,
                Order = f.Order,
                Value = f.DataType == TemplateDataType.Date ? DateTime.Today.ToString("yyyy-MM-dd") : null
            }).ToList()
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Step3(WizardStep2Vm vm)
    {
        var defs = await db.TemplateFieldDefinitions.Where(f => f.TemplateId == vm.TemplateId).ToListAsync();
        foreach (var field in vm.Fields)
        {
            var def = defs.FirstOrDefault(d => d.Key == field.Key);
            if (def == null) continue;
            if (def.IsRequired && string.IsNullOrWhiteSpace(field.Value))
                ModelState.AddModelError($"Fields[{vm.Fields.IndexOf(field)}].Value", $"Поле '{def.Label}' обязательно");
            if (def.MaxLength.HasValue && (field.Value?.Length ?? 0) > def.MaxLength.Value)
                ModelState.AddModelError($"Fields[{vm.Fields.IndexOf(field)}].Value", $"Максимум {def.MaxLength.Value} символов");
        }

        if (!ModelState.IsValid)
            return View("Step2", vm);

        var template = await db.DocumentTemplates.FirstAsync(t => t.Id == vm.TemplateId);
        var confirm = new WizardConfirmVm
        {
            TemplateId = vm.TemplateId,
            TemplateName = template.Name,
            TemplateVersion = template.Version,
            Values = vm.Fields.ToDictionary(f => f.Key, f => f.Value ?? string.Empty)
        };
        TempData["ConfirmData"] = JsonSerializer.Serialize(confirm);
        return View(confirm);
    }

    [HttpPost]
    public async Task<IActionResult> Generate()
    {
        var confirmData = TempData["ConfirmData"]?.ToString();
        if (string.IsNullOrWhiteSpace(confirmData))
            return RedirectToAction(nameof(Step1));

        var confirm = JsonSerializer.Deserialize<WizardConfirmVm>(confirmData)!;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var generation = await workflowService.CreateAndGenerateAsync(userId, confirm.TemplateId, confirm.Values);

        return RedirectToAction("Details", "Generations", new { id = generation.Id });
    }
}
