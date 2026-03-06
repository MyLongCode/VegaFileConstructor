using VegaFileConstructor.Models;
using VegaFileConstructor.ViewModels;

namespace VegaFileConstructor.Services;

public interface IPdfEditService
{
    Task<PdfEditOperation> CreateOperationAsync(string userId, IFormFile file, long maxSizeBytes);
    Task<PdfEditOperation> SaveReplacementsAsync(string userId, Guid operationId, IReadOnlyList<PdfEditReplacementRowViewModel> rows);
    Task<PdfEditOperation> ProcessAsync(string userId, Guid operationId);
    Task<PdfEditOperation?> GetByIdAsync(string userId, Guid id);
    Task<List<PdfEditOperation>> GetHistoryAsync(string userId, PdfEditStatus? status, DateTime? dateFrom, DateTime? dateTo, string? fileName);
}
