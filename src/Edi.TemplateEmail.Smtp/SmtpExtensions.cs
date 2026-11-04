using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Edi.TemplateEmail.Smtp;

public static class SmtpExtensions
{
    public static MimeMessage ToMimeMessage(this CommonMailMessage message, EmailSettings settings)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.FromAddress, nameof(settings.FromAddress));
        ArgumentNullException.ThrowIfNull(message.Receipts, nameof(message.Receipts));
        if (message.Receipts.Length == 0)
        {
            throw new ArgumentException("At least one To recipient is required.", nameof(message.Receipts));
        }

        var messageToSend = new MimeMessage();
        if (!string.IsNullOrEmpty(settings.SenderAddress))
        {
            messageToSend.Sender = new(settings.SenderName, settings.SenderAddress);
        }

        messageToSend.From.Add(new MailboxAddress(settings.EmailDisplayName, settings.FromAddress));
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

    /// <summary>
    /// Sends a message and returns the SMTP response. Cleanup failures preserve the
    /// successful response or the original operation exception.
    /// </summary>
    /// <remarks>
    /// Settings validation and MIME conversion complete before connecting or authenticating.
    /// Cancellation applies to connecting, authenticating, and sending, but not cleanup.
    /// No automatic retries are performed.
    /// </remarks>
    public static async Task<string> SendAsync(this CommonMailMessage message, EmailSettings settings, CancellationToken cancellationToken = default)
    {
        return await message.SendAsync(settings, new SmtpClient(), cancellationToken);
    }

    internal static async Task<string> SendAsync(this CommonMailMessage message, EmailSettings settings, ISmtpClient smtp, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(settings.SmtpSettings, nameof(settings.SmtpSettings));
            ArgumentException.ThrowIfNullOrWhiteSpace(settings.SmtpSettings.SmtpServer, nameof(settings.SmtpSettings.SmtpServer));
            if (settings.SmtpSettings.SmtpServerPort is < 0 or > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.SmtpSettings.SmtpServerPort));
            }
            if (!Enum.IsDefined(settings.SmtpSettings.SocketOptions))
            {
                throw new ArgumentOutOfRangeException(nameof(settings.SmtpSettings.SocketOptions));
            }
            if (!string.IsNullOrEmpty(settings.SmtpSettings.SmtpUserName))
            {
                ArgumentNullException.ThrowIfNull(settings.SmtpSettings.SmtpPassword, nameof(settings.SmtpSettings.SmtpPassword));
            }

            var messageToSend = message.ToMimeMessage(settings);

            if (settings.SmtpSettings.SkipCertificateValidation)
            {
                smtp.ServerCertificateValidationCallback = (_, _, _, _) => true;
            }

            await smtp.ConnectAsync(
                settings.SmtpSettings.SmtpServer,
                settings.SmtpSettings.SmtpServerPort,
                settings.SmtpSettings.SocketOptions,
                cancellationToken);
            if (!string.IsNullOrEmpty(settings.SmtpSettings.SmtpUserName))
            {
                await smtp.AuthenticateAsync(settings.SmtpSettings.SmtpUserName, settings.SmtpSettings.SmtpPassword, cancellationToken);
            }

            return await smtp.SendAsync(messageToSend, cancellationToken: cancellationToken);
        }
        finally
        {
            try
            {
                using (smtp)
                {
                    if (smtp.IsConnected)
                    {
                        await smtp.DisconnectAsync(true, CancellationToken.None);
                    }
                }
            }
            catch (Exception)
            {
                // Cleanup must not replace an accepted-send result or the original exception.
            }
        }
    }
}
