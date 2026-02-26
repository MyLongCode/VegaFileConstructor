using System.ComponentModel.DataAnnotations;

namespace VegaFileConstructor.Models;

public class Generation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string UserId { get; set; } = string.Empty;

    public Guid TemplateId { get; set; }
    public DocumentTemplate? Template { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public GenerationStatus Status { get; set; } = GenerationStatus.Created;
    public string? OutputFilePath { get; set; }
    public string? ErrorMessage { get; set; }

    public ICollection<GenerationFieldValue> FieldValues { get; set; } = new List<GenerationFieldValue>();
}
