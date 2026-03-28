namespace Edi.TemplateEmail;

public interface IEmailHelper
{
    TemplateEngine Engine { get; }

    IEmailHelper ForType(string mailType);
    IEmailHelper Map(string name, object value);
    IEmailHelper MapRange(params (string name, object value)[] values);
    CommonMailMessage BuildMessage(string[] receipts, string[] ccReceipts = null);
}