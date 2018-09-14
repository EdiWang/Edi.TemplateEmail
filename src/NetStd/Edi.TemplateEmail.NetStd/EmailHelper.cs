using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Edi.TemplateEmail.NetStd.Models;

namespace Edi.TemplateEmail.NetStd
{
    public class EmailHelper
    {
        #region Events

        public event EmailFailedEventHandler EmailFailed;

        public delegate void EmailFailedEventHandler(object sender, EmailStateEventArgs e);

        public event EmailSentEventHandler EmailSent;

        public delegate void EmailSentEventHandler(object sender, EmailStateEventArgs e);

        public event EmailCompletedEventHandler EmailCompleted;

        public delegate void EmailCompletedEventHandler(object sender, EmailStateEventArgs e);

        private void OnEmailFailed(MailMessage message)
        {
            EmailFailed?.Invoke(message, new EmailStateEventArgs(false, null));
        }

        private void OnEmailSent(MailMessage message)
        {
            EmailSent?.Invoke(message, new EmailStateEventArgs(true, null));
        }

        private void OnEmailComplete(MailMessage message)
        {
            EmailCompleted?.Invoke(message, new EmailStateEventArgs(true, null));
        }

        #endregion Events

        #region Properties

        public EmailSettings Settings { get; }

        public TemplateEngine CurrentEngine { get; private set; }

        #endregion

        private readonly MailConfiguration _mailConfiguration;

        public EmailHelper(string configSource, EmailSettings settings)
        {
            if (string.IsNullOrWhiteSpace(configSource))
            {
                throw new ArgumentNullException(nameof(configSource));
            }

            Settings = settings ?? throw new ArgumentNullException(nameof(settings));

            var serializer = new XmlSerializer(typeof(MailConfiguration));
            using (var fileStream = new FileStream(configSource, FileMode.Open))
            {
                _mailConfiguration = ((MailConfiguration)serializer.Deserialize(fileStream));
            }
        }

        public EmailHelper ApplyTemplate(string mailType, TemplatePipeline pipeline)
        {
            var messageToPersonalize = new TemplateMailMessage(_mailConfiguration, mailType);
            if (messageToPersonalize.Loaded)
            {
                var engine = new TemplateEngine(messageToPersonalize, pipeline);
                CurrentEngine = engine;
            }
            return this;
        }

        public async Task SendMailAsync(string toAddress,
            TemplateEngine templateEngine = null, string ccAddress = null, List<Attachment> attachments = null)
        {
            if (string.IsNullOrWhiteSpace(toAddress))
            {
                throw new ArgumentNullException(nameof(toAddress));
            }
            await SendMailAsync(new[] { toAddress }, templateEngine, ccAddress, attachments);
        }

        public async Task SendMailAsync(IEnumerable<string> toAddress,
            TemplateEngine templateEngine = null, string ccAddress = null, List<Attachment> attachments = null)
        {
            var enumerable = toAddress as string[] ?? toAddress.ToArray();
            if (!enumerable.Any())
            {
                throw new ArgumentNullException(nameof(toAddress));
            }

            switch (CurrentEngine)
            {
                case null when templateEngine == null:
                    throw new Exception("TemplateEngine must be specified.");
                case null when true:
                    CurrentEngine = templateEngine;
                    break;
            }

            var smtp = new SmtpClient(Settings.SmtpServer);
            if (!string.IsNullOrEmpty(Settings.SmtpUserName))
            {
                smtp.UseDefaultCredentials = Settings.UseDefaultCredentials;
                smtp.Credentials = new NetworkCredential(Settings.SmtpUserName, Settings.SmtpPassword);
            }
            smtp.Port = Settings.SmtpServerPort;
            smtp.EnableSsl = Settings.EnableSsl;

            // create mail message
            var messageToSend = new MailMessage
            {
                Sender = new MailAddress(Settings.SmtpUserName, Settings.SenderName),
                From = new MailAddress(Settings.SmtpUserName, Settings.EmailDisplayName),
                Body = CurrentEngine.Format(() => new StringBuilder(CurrentEngine.TextProvider.Text)).Trim(),
                Subject = CurrentEngine.Format(() => new StringBuilder(CurrentEngine.TextProvider.Subject)).Trim(),
                IsBodyHtml = CurrentEngine.TextProvider is TemplateMailMessage templateMailMessage && templateMailMessage.IsHtml
            };

            foreach (var add in enumerable)
            {
                messageToSend.To.Add(_mailConfiguration.CommonConfiguration.OverrideToAddress ?
                    _mailConfiguration.CommonConfiguration.ToAddress : add);
            }

            if (!string.IsNullOrEmpty(ccAddress))
            {
                messageToSend.CC.Add(ccAddress);
            }

            if (null != attachments && attachments.Any())
            {
                attachments.ForEach(attachment => messageToSend.Attachments.Add(attachment));
            }

            try
            {
                await smtp.SendMailAsync(messageToSend);
                OnEmailSent(messageToSend);
            }
            catch (SmtpException)
            {
                OnEmailFailed(messageToSend);
                throw;
            }
            finally
            {
                OnEmailComplete(messageToSend);
                messageToSend.Dispose();
            }
        }
    }
}