namespace Edi.TemplateEmail;

public class PipelineItem
{
    public string Name { get; set; }

    public object Value { get; set; }

    public string GetValue(string propertyName)
    {
        if (Value is null) return string.Empty;
        if (Value is string s) return s;

        var properties = Value.GetType().GetProperties();
        if (properties.Length == 0) return Value.ToString() ?? string.Empty;

        return Value.GetType().GetProperty(propertyName)?.GetValue(Value, null)?.ToString() ?? string.Empty;
    }
}