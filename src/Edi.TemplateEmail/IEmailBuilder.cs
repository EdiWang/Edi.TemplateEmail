namespace Edi.TemplateEmail;

/// <summary>
/// Holds mappings for a message. Use separate builders for concurrent work.
/// </summary>
public interface IEmailBuilder
{
    IEmailBuilder Map(string name, object value);
    IEmailBuilder MapRange(params (string name, object value)[] values);

    /// <summary>
    /// Builds a message with its own copies of the recipient arrays.
    /// </summary>
    CommonMailMessage BuildMessage(string[] receipts, string[] ccReceipts = null);
}
