using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VegaFileConstructor.Data;
using VegaFileConstructor.Models;
using VegaFileConstructor.ViewModels;

namespace VegaFileConstructor.Controllers;

[Authorize]
public class GenerationsController(ApplicationDbContext db, IWebHostEnvironment env) : Controller
{
    public async Task<IActionResult> Index(GenerationFilterVm filter)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var query = db.Generations
            .Include(g => g.Template)
            .Where(g => g.UserId == userId);

        if (filter.TemplateId.HasValue) query = query.Where(g => g.TemplateId == filter.TemplateId);
        if (filter.Status.HasValue) query = query.Where(g => g.Status == filter.Status);
        if (filter.DateFrom.HasValue) query = query.Where(g => g.CreatedAt >= filter.DateFrom);
        if (filter.DateTo.HasValue) query = query.Where(g => g.CreatedAt <= filter.DateTo.Value.AddDays(1));

        ViewBag.Templates = await db.DocumentTemplates.Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync();
        return View(await query.OrderByDescending(g => g.CreatedAt).ToListAsync());
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var generation = await db.Generations
            .Include(g => g.Template)
            .Include(g => g.FieldValues)
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        if (generation == null) return NotFound();
        return View(generation);
    }

    public async Task<IActionResult> Download(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var generation = await db.Generations.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
        if (generation == null || generation.Status != GenerationStatus.Generated || string.IsNullOrEmpty(generation.OutputFilePath))
            return NotFound();

        var filePath = Path.Combine(env.WebRootPath, generation.OutputFilePath.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(filePath)) return NotFound();
        return PhysicalFile(filePath, "application/pdf", $"{generation.Id}.pdf");
    }
}
