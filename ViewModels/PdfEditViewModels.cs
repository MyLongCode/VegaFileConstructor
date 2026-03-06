using System.ComponentModel.DataAnnotations;
using VegaFileConstructor.Models;

namespace VegaFileConstructor.ViewModels;

public class PdfEditUploadViewModel
{
    [Required(ErrorMessage = "Загрузите PDF-файл")]
    public IFormFile? File { get; set; }
}

public class PdfEditReplacementRowViewModel
{
    [MaxLength(500)]
    public string? OldValue { get; set; }

    [MaxLength(500)]
    public string? NewValue { get; set; }
}

public class PdfEditWizardViewModel
{
    public Guid? OperationId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public List<PdfEditReplacementRowViewModel> Replacements { get; set; } = new();
}

public class PdfEditResultReplacementViewModel
{
    public int Order { get; set; }
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public int FoundCount { get; set; }
    public int AppliedCount { get; set; }
}

public class PdfEditResultViewModel
{
    public Guid OperationId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string OutputFileName { get; set; } = string.Empty;
    public PdfEditStatus Status { get; set; }
    public int TotalRequestedReplacements { get; set; }
    public int TotalFoundOccurrences { get; set; }
    public int TotalAppliedReplacements { get; set; }
    public List<PdfEditResultReplacementViewModel> Replacements { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public List<string> NotFoundRows { get; set; } = new();
}

public class PdfEditHistoryItemViewModel
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public PdfEditStatus Status { get; set; }
    public int TotalRequestedReplacements { get; set; }
    public int TotalFoundOccurrences { get; set; }
    public int TotalAppliedReplacements { get; set; }
}

public class PdfEditHistoryViewModel
{
    public string? FileName { get; set; }
    public PdfEditStatus? Status { get; set; }
    [DataType(DataType.Date)]
    public DateTime? DateFrom { get; set; }
    [DataType(DataType.Date)]
    public DateTime? DateTo { get; set; }
    public List<PdfEditHistoryItemViewModel> Items { get; set; } = new();
}
