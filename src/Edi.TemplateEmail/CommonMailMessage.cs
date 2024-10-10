namespace Edi.TemplateEmail;

public class CommonMailMessage
{
    public string Subject { get; set; }

    public string Body { get; set; }

    public string[] Receipts { get; set; }

    public string[] CcReceipts { get; set; }
}