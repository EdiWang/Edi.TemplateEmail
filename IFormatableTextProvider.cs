namespace Edi.TemplateEmail
{
    public interface IFormatableTextProvider
    {
        string Text { get; }
        string Subject { get; }
    }
}