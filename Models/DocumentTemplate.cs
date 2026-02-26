using System.ComponentModel.DataAnnotations;

namespace VegaFileConstructor.Models;

public class DocumentTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, StringLength(80, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(40, MinimumLength = 2)]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    [Required]
    public string TemplateFilePath { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [Required]
    public string Version { get; set; } = "1.0";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TemplateFieldDefinition> FieldDefinitions { get; set; } = new List<TemplateFieldDefinition>();
    public ICollection<TemplateFieldPlacement> FieldPlacements { get; set; } = new List<TemplateFieldPlacement>();
}
