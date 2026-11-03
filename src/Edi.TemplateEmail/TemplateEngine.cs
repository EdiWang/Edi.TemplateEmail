using System.Text.RegularExpressions;

namespace Edi.TemplateEmail;

public partial class TemplateEngine(TemplateMailMessage provider, TemplatePipeline pipeline)
{
    [GeneratedRegex(@"\{(?<Entity>\w+)\.(?<Property>\w+)\}")]
    private static partial Regex FormatRegex();

    public TemplatePipeline Pipeline { get; } = pipeline;

    public TemplateMailMessage TextProvider { get; } = provider;

    public string Format(string text)
    {
        return FormatRegex().Replace(text, match =>
        {
            string entity = match.Groups["Entity"].Value;
            string property = match.Groups["Property"].Value;
            return Pipeline.HasEntity(entity) ? Pipeline[entity].GetValue(property) : string.Empty;
        });
    }
}
