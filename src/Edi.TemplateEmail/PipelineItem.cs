using System;

namespace Edi.TemplateEmail;

public class PipelineItem
{
    public string Name { get; set; }

    public object Value { get; set; }

    public string GetValue(string propertyName)
    {
        if (Value is null) return string.Empty;
        if (Value is string s) return s;
        if (propertyName == nameof(Value)) return Value.ToString() ?? string.Empty;

        var properties = Value.GetType().GetProperties();
        if (properties.Length == 0) return Value.ToString() ?? string.Empty;

        ArgumentNullException.ThrowIfNull(propertyName);

        var property = Array.Find(properties, property =>
            property.Name == propertyName
            && property.GetMethod is { IsPublic: true, IsStatic: false }
            && property.GetIndexParameters().Length == 0);

        return property?.GetValue(Value)?.ToString() ?? string.Empty;
    }
}
