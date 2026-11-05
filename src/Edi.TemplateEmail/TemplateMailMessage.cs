using System;
using System.Globalization;

namespace Edi.TemplateEmail;

public class TemplateMailMessage
{
    public string Text { get; set; }

    public bool IsHtml { get; set; }

    public string Subject { get; set; }

    public bool Loaded { get; }

    public TemplateMailMessage(MailConfiguration mailConfig, string messageType)
        : this(mailConfig, messageType, null)
    {
    }

    internal TemplateMailMessage(MailConfiguration mailConfig, string messageType, CultureInfo culture)
    {
        Loaded = false;

        if (null == mailConfig || null == mailConfig.MailMessages) return;

        // Load all the mail message templates that 
        // match the message type
        var possibleMails = mailConfig.MailMessages.FindAll(mail =>
            string.CompareOrdinal(mail.MessageType, messageType) == 0);
        if (possibleMails.Count == 0) return;

        MailMessageConfiguration mailMessageConfiguration;
        if (culture is null)
        {
            mailMessageConfiguration = possibleMails.Find(mail =>
                !string.IsNullOrEmpty(mail.MessageCulture)
                && mail.MessageCulture.Equals(CultureInfo.CurrentCulture.Name, StringComparison.InvariantCultureIgnoreCase))
                ?? possibleMails[0];
        }
        else
        {
            mailMessageConfiguration = null;
            for (var candidate = culture; candidate.Name.Length > 0; candidate = candidate.Parent)
            {
                mailMessageConfiguration = possibleMails.Find(mail =>
                    candidate.Name.Equals(mail.MessageCulture, StringComparison.InvariantCultureIgnoreCase));
                if (mailMessageConfiguration is not null) break;
            }

            mailMessageConfiguration ??= possibleMails.Find(mail => string.IsNullOrEmpty(mail.MessageCulture));
            if (mailMessageConfiguration is null) return;
        }

        // Map configured values to this instance
        Text = mailMessageConfiguration.MessageBody;
        IsHtml = mailMessageConfiguration.IsHtml;
        Subject = mailMessageConfiguration.MessageSubject;

        Loaded = true;
    }
}
