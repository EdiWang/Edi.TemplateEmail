using System;
using System.Globalization;
using Edi.TemplateEmail.NetStd.Models;

namespace Edi.TemplateEmail.NetStd
{
    public class TemplateMailMessage
    {
        public string Text { get; set; }

        public bool IsHtml { get; set; }

        public string Subject { get; set; }

        public bool Loaded { get; private set; }

        public TemplateMailMessage(MailConfiguration mailConfig, string messageType)
        {
            Loaded = false;

            if (null == mailConfig) return;

            // Load all the mail message templates that 
            // match the message type
            var possibleMails = mailConfig.MailMessages.FindAll(mail => 
                string.CompareOrdinal(mail.MessageType, messageType) == 0);
            if (possibleMails.Count == 0) return;

            // Now check if we have a mail with the current culture
            var mailMessageConfiguration =
                possibleMails.Find(
                    mail =>
                    !string.IsNullOrEmpty(mail.MessageCulture)
                    && mail.MessageCulture.Equals(CultureInfo.CurrentCulture.Name, StringComparison.InvariantCultureIgnoreCase)
                    ) ?? possibleMails[0];
            // If we can't find one, then use the first one we get

            // Map configured values to this instance
            Text = mailMessageConfiguration.MessageBody;
            IsHtml = mailMessageConfiguration.IsHtml;
            Subject = mailMessageConfiguration.MessageSubject;

            Loaded = true;
        }
    }
}
