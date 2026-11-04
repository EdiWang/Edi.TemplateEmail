namespace Edi.TemplateEmail;

/// <summary>
/// Marks a complete mapped value as trusted HTML for insertion without encoding
/// into HTML bodies. Only wrap markup from a trusted or sanitized source.
/// </summary>
public sealed class TrustedHtml(string html)
{
    public override string ToString() => html ?? string.Empty;
}
