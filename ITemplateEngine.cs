using System;
using System.Text;

namespace Edi.TemplateEmail
{
    public interface ITemplateEngine
    {
        string Format(Func<StringBuilder> textSelector);
        TemplatePipeline Pipeline { get; }
        IFormatableTextProvider TextProvider { get; }
    }
}
