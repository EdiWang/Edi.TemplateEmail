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
            messageToSend.Settings.SmtpSettings.SmtpServer,
            messageToSend.Settings.SmtpSettings.SmtpServerPort,
            messageToSend.Settings.SmtpSettings.EnableTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
        if (!string.IsNullOrEmpty(messageToSend.Settings.SmtpSettings.SmtpUserName))
        {
            await smtp.AuthenticateAsync(messageToSend.Settings.SmtpSettings.SmtpUserName, messageToSend.Settings.SmtpSettings.SmtpPassword);
        }

        var result = await smtp.SendAsync(messageToSend.MimeMessage);
        await smtp.DisconnectAsync(true);

        return result;
    }
}