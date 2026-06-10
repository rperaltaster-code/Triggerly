namespace Triggerly.Shared.Models;

public enum FormFieldType { Text, Number, Date, Dropdown, Checkbox }

public class FormField
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public FormFieldType Type { get; set; }
    public bool Required { get; set; }
    public string? Placeholder { get; set; }
    public List<string>? Options { get; set; }
}
