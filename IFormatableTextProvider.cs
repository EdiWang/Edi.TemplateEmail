namespace Edi.Web.TemplateEmail
{
    public interface IFormatableTextProvider
    {
        string Text { get; }
        string Subject { get; }
    }
}