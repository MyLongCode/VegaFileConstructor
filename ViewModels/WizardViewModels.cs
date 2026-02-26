using System.ComponentModel.DataAnnotations;
using VegaFileConstructor.Models;

namespace VegaFileConstructor.ViewModels;

public class TemplateSelectionVm
{
    public string? Search { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public List<DocumentTemplate> Templates { get; set; } = [];
}

public class DynamicFieldInputVm
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Group { get; set; }
    public string? HelpText { get; set; }
    public string? Placeholder { get; set; }
    public TemplateDataType DataType { get; set; }
    public bool IsRequired { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public int Order { get; set; }
    public string? Value { get; set; }
}

public class WizardStep2Vm
{
    public Guid TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateVersion { get; set; } = string.Empty;
    public List<DynamicFieldInputVm> Fields { get; set; } = [];
}

public class WizardConfirmVm
{
    public Guid TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateVersion { get; set; } = string.Empty;
    public Dictionary<string, string> Values { get; set; } = [];
}

public class GenerationFilterVm
{
    public Guid? TemplateId { get; set; }
    public GenerationStatus? Status { get; set; }
    [DataType(DataType.Date)]
    public DateTime? DateFrom { get; set; }
    [DataType(DataType.Date)]
    public DateTime? DateTo { get; set; }
}
