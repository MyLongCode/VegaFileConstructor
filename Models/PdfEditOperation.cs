using System.ComponentModel.DataAnnotations;

namespace VegaFileConstructor.Models;

public class PdfEditOperation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    public string OriginalFilePath { get; set; } = string.Empty;

    public string? OutputFilePath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public PdfEditStatus Status { get; set; } = PdfEditStatus.Uploaded;

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    public int TotalRequestedReplacements { get; set; }
    public int TotalFoundOccurrences { get; set; }
    public int TotalAppliedReplacements { get; set; }

    public ICollection<PdfEditReplacement> Replacements { get; set; } = new List<PdfEditReplacement>();
}
