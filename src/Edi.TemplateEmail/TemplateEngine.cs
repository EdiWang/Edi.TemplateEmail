using System.Net;
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
        return Format(text, false);
    }

    /// <summary>
    /// Formats tokens, optionally HTML-encoding ordinary mapped values while
    /// preserving template markup and explicitly trusted HTML fragments.
    /// </summary>
    public string Format(string text, bool encodeHtml)
    {
        return FormatRegex().Replace(text, match =>
        {
            string entity = match.Groups["Entity"].Value;
            string property = match.Groups["Property"].Value;
            if (!Pipeline.HasEntity(entity)) return string.Empty;

            var item = Pipeline[entity];
            var value = item.GetValue(property);
            return encodeHtml && item.Value is not TrustedHtml ? WebUtility.HtmlEncode(value) : value;
        });
    }
}
