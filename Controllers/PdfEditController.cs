using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VegaFileConstructor.Models;
using VegaFileConstructor.Services;
using VegaFileConstructor.ViewModels;

namespace VegaFileConstructor.Controllers;

[Authorize]
[Route("pdf-edit")]
public class PdfEditController(IPdfEditService pdfEditService, IWebHostEnvironment env) : Controller
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024;

    [HttpGet("")]
    public IActionResult Index()
    {
        return View(new PdfEditUploadViewModel());
    }

    [HttpPost("upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(PdfEditUploadViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("Index", vm);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            var op = await pdfEditService.CreateOperationAsync(userId, vm.File!, MaxFileSizeBytes);
            return RedirectToAction(nameof(Edit), new { id = op.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(vm.File), ex.Message);
            return View("Index", vm);
        }
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var op = await pdfEditService.GetByIdAsync(userId, id);
        if (op == null) return NotFound();

        var vm = new PdfEditWizardViewModel
        {
            OperationId = op.Id,
            OriginalFileName = op.OriginalFileName,
            Replacements = op.Replacements
                .OrderBy(x => x.Order)
                .Select(x => new PdfEditReplacementRowViewModel { OldValue = x.OldValue, NewValue = x.NewValue })
                .ToList()
        };

        if (vm.Replacements.Count == 0)
            vm.Replacements.Add(new PdfEditReplacementRowViewModel());

        return View(vm);
    }

    [HttpPost("confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(PdfEditWizardViewModel vm)
    {
        if (!vm.OperationId.HasValue)
            return RedirectToAction(nameof(Index));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            var op = await pdfEditService.SaveReplacementsAsync(userId, vm.OperationId.Value, vm.Replacements);
            var refreshed = await pdfEditService.GetByIdAsync(userId, op.Id);
            return View(refreshed);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var op = await pdfEditService.GetByIdAsync(userId, vm.OperationId.Value);
            vm.OriginalFileName = op?.OriginalFileName ?? vm.OriginalFileName;
            return View("Edit", vm);
        }
    }

    [HttpPost("process/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await pdfEditService.ProcessAsync(userId, id);
        return RedirectToAction(nameof(Result), new { id });
    }

    [HttpGet("result/{id:guid}")]
    public async Task<IActionResult> Result(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var op = await pdfEditService.GetByIdAsync(userId, id);
        if (op == null) return NotFound();

        var vm = new PdfEditResultViewModel
        {
            OperationId = op.Id,
            OriginalFileName = op.OriginalFileName,
            OutputFileName = op.OutputFilePath is null ? string.Empty : Path.GetFileName(op.OutputFilePath),
            Status = op.Status,
            TotalRequestedReplacements = op.TotalRequestedReplacements,
            TotalFoundOccurrences = op.TotalFoundOccurrences,
            TotalAppliedReplacements = op.TotalAppliedReplacements,
            ErrorMessage = op.ErrorMessage,
            Replacements = op.Replacements.OrderBy(x => x.Order).Select(x => new PdfEditResultReplacementViewModel
            {
                Order = x.Order,
                OldValue = x.OldValue,
                NewValue = x.NewValue,
                FoundCount = x.FoundCount,
                AppliedCount = x.AppliedCount
            }).ToList()
        };

        vm.NotFoundRows = vm.Replacements.Where(x => x.FoundCount == 0).Select(x => x.OldValue).ToList();
        return View(vm);
    }

    [HttpGet("history")]
    public async Task<IActionResult> History(PdfEditHistoryViewModel filters)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var items = await pdfEditService.GetHistoryAsync(userId, filters.Status, filters.DateFrom, filters.DateTo, filters.FileName);
        filters.Items = items.Select(x => new PdfEditHistoryItemViewModel
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt,
            OriginalFileName = x.OriginalFileName,
            Status = x.Status,
            TotalRequestedReplacements = x.TotalRequestedReplacements,
            TotalFoundOccurrences = x.TotalFoundOccurrences,
            TotalAppliedReplacements = x.TotalAppliedReplacements
        }).ToList();
        return View(filters);
    }

    [HttpGet("details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var op = await pdfEditService.GetByIdAsync(userId, id);
        if (op == null) return NotFound();
        return View(op);
    }

    [HttpGet("download/{id:guid}")]
    public async Task<IActionResult> Download(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var op = await pdfEditService.GetByIdAsync(userId, id);
        if (op == null || op.Status != PdfEditStatus.Completed || string.IsNullOrWhiteSpace(op.OutputFilePath))
            return NotFound();

        var abs = Path.Combine(env.ContentRootPath, "storage", op.OutputFilePath.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(abs))
            return NotFound();

        return PhysicalFile(abs, "application/pdf", $"edited_{op.OriginalFileName}");
    }
}
