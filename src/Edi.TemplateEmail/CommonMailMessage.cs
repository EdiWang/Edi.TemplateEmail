using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using System.Threading.Tasks;

namespace Edi.TemplateEmail;

public class CommonMailMessage
{
    public string Subject { get; set; }

    public string Body { get; set; }

    public string[] Receipts { get; set; }

    public string[] CcReceipts { get; set; }

    public EmailSettings Settings { get; set; }

    public bool BodyIsHtml { get; set; }
}

public static class CommonMailMessageExtensions
{
    public static MimeMessage ToMimeMessage(this CommonMailMessage message)
    {
        var messageToSend = new MimeMessage
        {
            Sender = new(message.Settings.SenderName, message.Settings.SmtpSettings.SmtpUserName),
        };

        messageToSend.From.Add(new MailboxAddress(message.Settings.EmailDisplayName, message.Settings.SmtpSettings.SmtpUserName));
        messageToSend.Subject = message.Subject;
        messageToSend.Body = message.BodyIsHtml
            ? new(TextFormat.Html) { Text = message.Body }
            : new TextPart(TextFormat.Plain) { Text = message.Body };

        foreach (var address in message.Receipts)
        {
            messageToSend.To.Add(MailboxAddress.Parse(address));
        }

        if (message.CcReceipts is { Length: > 0 })
        {
            foreach (var ccReceipt in message.CcReceipts)
            {
                messageToSend.Cc.Add(MailboxAddress.Parse(ccReceipt));
            }
        }

        return messageToSend;
    }

    public static async Task<string> SendAsync(this CommonMailMessage message)
    {
        using var smtp = new MailKit.Net.Smtp.SmtpClient();
        smtp.ServerCertificateValidationCallback = (_, _, _, _) => true;
        await smtp.ConnectAsync(
            message.Settings.SmtpSettings.SmtpServer,
            message.Settings.SmtpSettings.SmtpServerPort,
            message.Settings.SmtpSettings.EnableTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
        if (!string.IsNullOrEmpty(message.Settings.SmtpSettings.SmtpUserName))
        {
            await smtp.AuthenticateAsync(message.Settings.SmtpSettings.SmtpUserName, message.Settings.SmtpSettings.SmtpPassword);
        }

        var messageToSend = message.ToMimeMessage();
        var result = await smtp.SendAsync(messageToSend);
        await smtp.DisconnectAsync(true);

        return result;
    }
}