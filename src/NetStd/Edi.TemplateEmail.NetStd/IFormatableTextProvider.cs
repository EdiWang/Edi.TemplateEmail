namespace Edi.TemplateEmail.NetStd
{
    public interface IFormatableTextProvider
    {
        string Text { get; }
        string Subject { get; }
    }
}