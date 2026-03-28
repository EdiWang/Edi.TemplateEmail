using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Edi.TemplateEmail.Smtp;

public static class SmtpExtensions
{
    public static MimeMessage ToMimeMessage(this CommonMailMessage message, EmailSettings settings)
    {
        var messageToSend = new MimeMessage
        {
            Sender = new(settings.SenderName, settings.SmtpSettings.SmtpUserName),
        };

        messageToSend.From.Add(new MailboxAddress(settings.EmailDisplayName, settings.SmtpSettings.SmtpUserName));
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

    public static async Task<string> SendAsync(this CommonMailMessage message, EmailSettings settings, CancellationToken cancellationToken = default)
    {
        using var smtp = new MailKit.Net.Smtp.SmtpClient();
        if (settings.SmtpSettings.SkipCertificateValidation)
        {
            smtp.ServerCertificateValidationCallback = (_, _, _, _) => true;
        }

        await smtp.ConnectAsync(
            settings.SmtpSettings.SmtpServer,
            settings.SmtpSettings.SmtpServerPort,
            settings.SmtpSettings.EnableTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
            cancellationToken);
        if (!string.IsNullOrEmpty(settings.SmtpSettings.SmtpUserName))
        {
            await smtp.AuthenticateAsync(settings.SmtpSettings.SmtpUserName, settings.SmtpSettings.SmtpPassword, cancellationToken);
        }

        var messageToSend = message.ToMimeMessage(settings);
        var result = await smtp.SendAsync(messageToSend, cancellationToken: cancellationToken);
        await smtp.DisconnectAsync(true, cancellationToken);

        return result;
    }
}
