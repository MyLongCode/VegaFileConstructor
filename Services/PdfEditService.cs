using Microsoft.EntityFrameworkCore;
using iText.Kernel.Pdf;
using VegaFileConstructor.Data;
using VegaFileConstructor.Models;
using VegaFileConstructor.ViewModels;

namespace VegaFileConstructor.Services;

public class PdfEditService(
    ApplicationDbContext db,
    IWebHostEnvironment env,
    IPdfTextReplaceService pdfTextReplaceService) : IPdfEditService
{
    private const string ImageMarker = "img:";

    public async Task<PdfEditOperation> CreateOperationAsync(string userId, IFormFile file, long maxSizeBytes)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("Загрузите PDF-файл");
        }

        if (file.Length > maxSizeBytes)
        {
            throw new InvalidOperationException("Файл слишком большой");
        }

        var ext = Path.GetExtension(file.FileName);
        if (!".pdf".Equals(ext, StringComparison.OrdinalIgnoreCase) ||
            (file.ContentType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) != true && file.ContentType != "application/octet-stream"))
        {
            throw new InvalidOperationException("Допускаются только PDF-файлы");
        }

        var op = new PdfEditOperation
        {
            UserId = userId,
            OriginalFileName = SanitizeFileName(file.FileName),
            Status = PdfEditStatus.Uploaded,
            UpdatedAt = DateTime.UtcNow
        };

        db.PdfEditOperations.Add(op);
        await db.SaveChangesAsync();

        var baseDir = Path.Combine(env.ContentRootPath, "storage", "pdf-edit", userId, op.Id.ToString());
        Directory.CreateDirectory(baseDir);
        var originalAbs = Path.Combine(baseDir, "original.pdf");
        await using (var stream = File.Create(originalAbs))
        {
            await file.CopyToAsync(stream);
        }

        EnsureReadablePdf(originalAbs);

        op.OriginalFilePath = BuildStorageRelativePath(userId, op.Id, "original.pdf");
        op.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return op;
    }

    public async Task<PdfEditOperation> SaveReplacementsAsync(string userId, Guid operationId, IReadOnlyList<PdfEditReplacementRowViewModel> rows)
    {
        var operation = await db.PdfEditOperations
            .FirstOrDefaultAsync(x => x.Id == operationId && x.UserId == userId)
            ?? throw new InvalidOperationException("Операция не найдена");

        var filtered = rows
            .Select((x, idx) => new { Row = x, Order = idx + 1 })
            .Where(x => !string.IsNullOrWhiteSpace(x.Row.OldValue) || !string.IsNullOrWhiteSpace(x.Row.NewValue) || x.Row.UseImage || x.Row.NewImageFile is not null)
            .ToList();

        if (filtered.Count == 0)
        {
            throw new InvalidOperationException("Добавьте хотя бы одну строку для замены");
        }

        var preparedRows = new List<PdfEditReplacement>();
        foreach (var item in filtered)
        {
            var oldValue = item.Row.OldValue?.Trim();
            if (string.IsNullOrWhiteSpace(oldValue))
            {
                throw new InvalidOperationException("OldValue обязательно для каждой заполненной строки");
            }

            if (item.Row.UseImage)
            {
                var imagePath = await ResolveImagePathAsync(userId, operationId, item.Order, item.Row);
                preparedRows.Add(new PdfEditReplacement
                {
                    OperationId = operation.Id,
                    Order = item.Order,
                    OldValue = oldValue,
                    NewValue = $"{ImageMarker}{imagePath}"
                });
                continue;
            }

            var newValue = item.Row.NewValue?.Trim();
            if (string.IsNullOrWhiteSpace(newValue))
                throw new InvalidOperationException("NewValue обязателен для текстовой замены");
            if (oldValue.Length > 500 || newValue.Length > 500)
                throw new InvalidOperationException("Длина поля замены не должна превышать 500 символов");

            preparedRows.Add(new PdfEditReplacement
            {
                OperationId = operation.Id,
                Order = item.Order,
                OldValue = oldValue,
                NewValue = newValue
            });
        }

        var existingRows = await db.PdfEditReplacements
            .Where(x => x.OperationId == operation.Id)
            .ToListAsync();
        if (existingRows.Count > 0)
        {
            db.PdfEditReplacements.RemoveRange(existingRows);
            await db.SaveChangesAsync();
        }

        db.PdfEditReplacements.AddRange(preparedRows);
        operation.TotalRequestedReplacements = preparedRows.Count;
        operation.TotalFoundOccurrences = 0;
        operation.TotalAppliedReplacements = 0;
        operation.Status = PdfEditStatus.Uploaded;
        operation.ErrorMessage = null;
        operation.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        operation.Replacements = preparedRows;
        return operation;
    }

    public async Task<PdfEditOperation> ProcessAsync(string userId, Guid operationId)
    {
        var operation = await db.PdfEditOperations
            .Include(x => x.Replacements.OrderBy(r => r.Order))
            .FirstOrDefaultAsync(x => x.Id == operationId && x.UserId == userId)
            ?? throw new InvalidOperationException("Операция не найдена");

        if (operation.Replacements.Count == 0)
            throw new InvalidOperationException("Добавьте хотя бы одну строку для замены");

        operation.Status = PdfEditStatus.Processing;
        operation.UpdatedAt = DateTime.UtcNow;
        operation.ErrorMessage = null;
        await db.SaveChangesAsync();

        try
        {
            var originalAbs = ToAbsoluteStoragePath(operation.OriginalFilePath);
            var outputAbs = ToAbsoluteStoragePath(BuildStorageRelativePath(userId, operation.Id, "result.pdf"));

            var summary = await pdfTextReplaceService.ReplaceTextAsync(
                originalAbs,
                outputAbs,
                operation.Replacements
                    .OrderBy(x => x.Order)
                    .Select(x => new PdfTextReplacementInput(x.Order, x.OldValue, x.NewValue))
                    .ToList());

            foreach (var row in summary.Rows)
            {
                var entity = operation.Replacements.First(x => x.Order == row.Order);
                entity.FoundCount = row.FoundCount;
                entity.AppliedCount = row.AppliedCount;
            }

            operation.TotalFoundOccurrences = summary.TotalFoundOccurrences;
            operation.TotalAppliedReplacements = summary.TotalAppliedReplacements;
            operation.OutputFilePath = BuildStorageRelativePath(userId, operation.Id, "result.pdf");
            operation.Status = PdfEditStatus.Completed;
        }
        catch (Exception ex)
        {
            operation.Status = PdfEditStatus.Failed;
            operation.ErrorMessage = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
        }

        operation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return operation;
    }

    public Task<PdfEditOperation?> GetByIdAsync(string userId, Guid id)
    {
        return db.PdfEditOperations
            .Include(x => x.Replacements.OrderBy(r => r.Order))
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id);
    }

    public async Task<List<PdfEditOperation>> GetHistoryAsync(string userId, PdfEditStatus? status, DateTime? dateFrom, DateTime? dateTo, string? fileName)
    {
        var query = db.PdfEditOperations
            .Where(x => x.UserId == userId)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);
        if (dateFrom.HasValue)
            query = query.Where(x => x.CreatedAt >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            query = query.Where(x => x.CreatedAt <= dateTo.Value.Date.AddDays(1).AddSeconds(-1));
        if (!string.IsNullOrWhiteSpace(fileName))
            query = query.Where(x => x.OriginalFileName.Contains(fileName));

        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
    }

    private async Task<string> ResolveImagePathAsync(string userId, Guid operationId, int order, PdfEditReplacementRowViewModel row)
    {
        if (row.NewImageFile is not null && row.NewImageFile.Length > 0)
        {
            if (row.NewImageFile.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) != true)
            {
                throw new InvalidOperationException("Для замены изображением загрузите файл-картинку");
            }

            var ext = Path.GetExtension(row.NewImageFile.FileName);
            if (string.IsNullOrWhiteSpace(ext))
            {
                ext = ".png";
            }

            var imagesDir = Path.Combine(env.WebRootPath, "uploads", "pdf-edit-replacements", userId, operationId.ToString());
            Directory.CreateDirectory(imagesDir);
            var imageName = $"r{order}_{Guid.NewGuid():N}{ext}";
            var imageAbsPath = Path.Combine(imagesDir, imageName);

            await using var stream = File.Create(imageAbsPath);
            await row.NewImageFile.CopyToAsync(stream);

            return $"/uploads/pdf-edit-replacements/{userId}/{operationId}/{imageName}";
        }

        if (!string.IsNullOrWhiteSpace(row.ExistingImagePath))
        {
            return row.ExistingImagePath;
        }

        throw new InvalidOperationException("Для замены изображением нужно загрузить картинку");
    }

    private static string SanitizeFileName(string fileName)
    {
        var justName = Path.GetFileName(fileName);
        var invalid = Path.GetInvalidFileNameChars();
        var chars = justName.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        return new string(chars);
    }

    private static void EnsureReadablePdf(string filePath)
    {
        try
        {
            using var reader = new PdfReader(filePath);
            using var doc = new PdfDocument(reader);
            _ = doc.GetNumberOfPages();
        }
        catch
        {
            throw new InvalidOperationException("Не удалось прочитать PDF");
        }
    }

    private static string BuildStorageRelativePath(string userId, Guid operationId, string fileName)
        => Path.Combine("pdf-edit", userId, operationId.ToString(), fileName).Replace('\\', '/');

    private string ToAbsoluteStoragePath(string relativePath)
        => Path.Combine(env.ContentRootPath, "storage", relativePath.Replace('/', Path.DirectorySeparatorChar));
}
