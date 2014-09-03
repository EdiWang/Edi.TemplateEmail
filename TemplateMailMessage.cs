using System;
using System.Globalization;
using Edi.XmlConfigMapper;

namespace Edi.TemplateEmail
{
    public class TemplateMailMessage : IFormatableTextProvider
    {
        /// <summary>
        /// Gets the text.
        /// </summary>
        /// <value>The text.</value>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is HTML.
        /// </summary>
        /// <value><c>true</c> if this instance is HTML; otherwise, <c>false</c>.</value>
        public bool IsHtml { get; set; }

        /// <summary>
        /// Gets or sets the subject.
        /// </summary>
        /// <value>The subject.</value>
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="TemplateMailMessage"/> is loaded.
        /// </summary>
        /// <value><c>true</c> if loaded; otherwise, <c>false</c>.</value>
        public bool Loaded { get; private set; }

        public TemplateMailMessage(string messageType)
        {
            Loaded = false;

            var mailConfiguration = XmlSection<MailConfiguration>.GetSection("mailConfiguration");
            if (null == mailConfiguration) return;

            // Load all the mail message templates that 
            // match the message type
            var possibleMails = mailConfiguration.MailMessages.FindAll(mail => 
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
