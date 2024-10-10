using MailKit.Security;
using System.Threading.Tasks;

namespace Edi.TemplateEmail;

public static class MimeMessageWithSettingsExtension
{
    public static async Task<string> SendAsync(this MimeMessageWithSettings messageToSend)
    {
        using var smtp = new MailKit.Net.Smtp.SmtpClient();
        smtp.ServerCertificateValidationCallback = (_, _, _, _) => true;
        await smtp.ConnectAsync(
            messageToSend.Settings.SmtpServer,
            messageToSend.Settings.SmtpServerPort,
            messageToSend.Settings.EnableTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
        if (!string.IsNullOrEmpty(messageToSend.Settings.SmtpUserName))
        {
            await smtp.AuthenticateAsync(messageToSend.Settings.SmtpUserName, messageToSend.Settings.SmtpPassword);
        }

        var result = await smtp.SendAsync(messageToSend.MimeMessage);
        await smtp.DisconnectAsync(true);

        return result;
    }
}