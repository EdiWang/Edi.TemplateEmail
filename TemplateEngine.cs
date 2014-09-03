using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Edi.TemplateEmail
{
    public class TemplateEngine : ITemplateEngine
    {
        private const string FormatPattern = @"{(?<Entity>\w+).(?<Property>\w+)";

        public TemplatePipeline Pipeline { get; private set; }

        public IFormatableTextProvider TextProvider { get; private set; }

        public TemplateEngine(IFormatableTextProvider provider, TemplatePipeline pipeline)
        {
            TextProvider = provider;
            Pipeline = pipeline;
        }

        public string Format(Func<StringBuilder> textSelector)
        {
            // Find all the personalization tokens in the text
            // They should be in the format {<item name>.<item property>}
            // Eg. {User.FirstName} will be replaced by the FirstName property
            // of the personalization item named "User"
            MatchCollection matches = Regex.Matches(TextProvider.Text, FormatPattern);
            StringBuilder formattedText = textSelector(); //new StringBuilder(TextProvider.Text);

            foreach (Match match in matches)
            {
                string entity = match.Groups["Entity"].Value;
                string property = match.Groups["Property"].Value;
                string value = Pipeline.HasEntity(entity) ? Pipeline[entity].GetValue(property) : string.Empty;

                formattedText.Replace(string.Format("{{{0}.{1}}}", entity, property), value);
            }

            return formattedText.ToString();
        }
    }
}