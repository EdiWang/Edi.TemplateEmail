namespace Edi.TemplateEmail;

public interface IEmailHelper
{
    EmailSettings Settings { get; }
    TemplateEngine Engine { get; }

    public EmailHelper ForType(string mailType);
    public EmailHelper Map(string name, object value);
    public CommonMailMessage BuildMessage(string[] receipts, string[] ccReceipts = null);
}