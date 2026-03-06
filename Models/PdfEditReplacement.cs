using System.ComponentModel.DataAnnotations;

namespace VegaFileConstructor.Models;

public class PdfEditReplacement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OperationId { get; set; }
    public PdfEditOperation? Operation { get; set; }

    public int Order { get; set; }

    [Required]
    [MaxLength(500)]
    public string OldValue { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string NewValue { get; set; } = string.Empty;

    public int FoundCount { get; set; }
    public int AppliedCount { get; set; }
}
