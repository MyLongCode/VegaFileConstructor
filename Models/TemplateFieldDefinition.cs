using System.ComponentModel.DataAnnotations;

namespace VegaFileConstructor.Models;

public class TemplateFieldDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TemplateId { get; set; }
    public DocumentTemplate? Template { get; set; }

    [Required]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Label { get; set; } = string.Empty;

    public string? HelpText { get; set; }
    public TemplateDataType DataType { get; set; } = TemplateDataType.Text;
    public bool IsRequired { get; set; } = true;
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public int Order { get; set; }
    public string? Group { get; set; }
    public string? Placeholder { get; set; }
}
