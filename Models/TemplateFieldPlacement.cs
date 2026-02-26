namespace VegaFileConstructor.Models;

public class TemplateFieldPlacement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TemplateId { get; set; }
    public DocumentTemplate? Template { get; set; }

    public string FieldKey { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public float X { get; set; }
    public float Y { get; set; }
    public float FontSize { get; set; } = 10;
    public float? MaxWidth { get; set; }
    public TextAlign Align { get; set; } = TextAlign.Left;
}
