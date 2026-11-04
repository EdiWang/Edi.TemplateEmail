namespace Edi.TemplateEmail;

public interface IEmailHelper
{
    /// <summary>
    /// Creates an independent builder. Template selection is deferred until BuildMessage.
    /// </summary>
    IEmailBuilder ForType(string mailType);
}
