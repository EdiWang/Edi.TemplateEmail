using System;
using System.Globalization;

namespace Edi.TemplateEmail;

public class PipelineItem
{
    public string Name { get; set; }

    public object Value { get; set; }

    public string GetValue(string propertyName)
    {
        return GetValue(propertyName, null);
    }

    internal string GetValue(string propertyName, CultureInfo culture)
    {
        if (Value is null) return string.Empty;
        if (Value is string s) return s;
        if (propertyName == nameof(Value)) return FormatValue(Value, culture);

        var properties = Value.GetType().GetProperties();
        if (properties.Length == 0) return FormatValue(Value, culture);

        ArgumentNullException.ThrowIfNull(propertyName);

        var property = Array.Find(properties, property =>
            property.Name == propertyName
            && property.GetMethod is { IsPublic: true, IsStatic: false }
            && property.GetIndexParameters().Length == 0);

        return FormatValue(property?.GetValue(Value), culture);
    }

    private static string FormatValue(object value, CultureInfo culture)
    {
        return culture is not null && value is IFormattable formattable
            ? formattable.ToString(null, culture) ?? string.Empty
            : value?.ToString() ?? string.Empty;
    }
}
