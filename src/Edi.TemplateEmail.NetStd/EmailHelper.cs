using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Edi.TemplateEmail.NetStd.Models;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace Edi.TemplateEmail.NetStd
{
    public class EmailHelper : IEmailHelper
    {
        #region Events

        public event EmailFailedEventHandler EmailFailed;

        public delegate void EmailFailedEventHandler(object sender, EmailStateEventArgs e);

        public event EmailSentEventHandler EmailSent;

        public delegate void EmailSentEventHandler(object sender, EmailStateEventArgs e);

        public event EmailCompletedEventHandler EmailCompleted;

        public delegate void EmailCompletedEventHandler(object sender, EmailStateEventArgs e);

        private void OnEmailFailed(MimeMessage message)
        {
            EmailFailed?.Invoke(message, new EmailStateEventArgs(false, null));
        }

        private void OnEmailSent(MimeMessage message, string response)
        {
            EmailSent?.Invoke(message, new EmailStateEventArgs(true, null)
            {
                ServerResponse = response
            });
        }

        private void OnEmailComplete(MimeMessage message)
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
            TemplateEngine templateEngine = null, string ccAddress = null)
        {
            if (string.IsNullOrWhiteSpace(toAddress))
            {
                throw new ArgumentNullException(nameof(toAddress));
            }
            await SendMailAsync(new[] { toAddress }, templateEngine, ccAddress);
        }

        public async Task SendMailAsync(IEnumerable<string> toAddress,
            TemplateEngine templateEngine = null, string ccAddress = null)
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

            // create mail message
            var messageToSend = new MimeMessage
            {
                Sender = new MailboxAddress(Settings.SenderName, Settings.SmtpUserName),
                Subject = CurrentEngine.Format(() => new StringBuilder(CurrentEngine.TextProvider.Subject)).Trim(),
            };
            messageToSend.From.Add(new MailboxAddress(Settings.EmailDisplayName, Settings.SmtpUserName));
            var bodyText = CurrentEngine.Format(() => new StringBuilder(CurrentEngine.TextProvider.Text)).Trim();
            if (CurrentEngine.TextProvider is TemplateMailMessage templateMailMessage && templateMailMessage.IsHtml)
            {
                messageToSend.Body = new TextPart(TextFormat.Html) { Text = bodyText };
            }
            else
            {
                messageToSend.Body = new TextPart(TextFormat.Plain) { Text = bodyText };
            }

            if (_mailConfiguration.CommonConfiguration.OverrideToAddress)
            {
                messageToSend.To.Add(new MailboxAddress(_mailConfiguration.CommonConfiguration.ToAddress));
            }
            else
            {
                foreach (var add in enumerable)
                {
                    messageToSend.To.Add(new MailboxAddress(add));
                }
            }

            if (!string.IsNullOrEmpty(ccAddress))
            {
                messageToSend.Cc.Add(new MailboxAddress(ccAddress));
            }

            try
            {
                using (var smtp = new MailKit.Net.Smtp.SmtpClient())
                {
                    smtp.MessageSent += (sender, args) =>
                    {
                        OnEmailSent(messageToSend, args.Response);
                    };

                    smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    await smtp.ConnectAsync(Settings.SmtpServer, Settings.SmtpServerPort,
                        Settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
                    if (!string.IsNullOrEmpty(Settings.SmtpUserName))
                    {
                        await smtp.AuthenticateAsync(Settings.SmtpUserName, Settings.SmtpPassword);
                    }

                    await smtp.SendAsync(messageToSend);
                    await smtp.DisconnectAsync(true);
                }
            }
            catch (SmtpException)
            {
                OnEmailFailed(messageToSend);
                throw;
            }
            finally
            {
                OnEmailComplete(messageToSend);
            }
        }
    }
}