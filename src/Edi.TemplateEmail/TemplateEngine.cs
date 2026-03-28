using System.Text;
using System.Text.RegularExpressions;

namespace Edi.TemplateEmail;

public partial class TemplateEngine(TemplateMailMessage provider, TemplatePipeline pipeline)
{
    [GeneratedRegex(@"\{(?<Entity>\w+)\.(?<Property>\w+)")]
    private static partial Regex FormatRegex();

    public TemplatePipeline Pipeline { get; } = pipeline;

    public TemplateMailMessage TextProvider { get; } = provider;

    public string Format(string text)
    {
        // Find all the personalization tokens in the text
        // They should be in the format {<item name>.<item property>}
        // Eg. {User.FirstName} will be replaced by the FirstName property
        // of the personalization item named "User"
        MatchCollection matches = FormatRegex().Matches(text);
        var formattedText = new StringBuilder(text);

        foreach (Match match in matches)
        {
            string entity = match.Groups["Entity"].Value;
            string property = match.Groups["Property"].Value;
            string value = Pipeline.HasEntity(entity) ? Pipeline[entity].GetValue(property) : string.Empty;

            formattedText.Replace($"{{{entity}.{property}}}", value);
        }

        return formattedText.ToString();
    }
}