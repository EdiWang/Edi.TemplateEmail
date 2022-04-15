using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Edi.TemplateEmail.Models;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace Edi.TemplateEmail
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

        public EmailHelper(string configSource, string smtpServer, string smtpUserName, string smtpPassword, int smtpServerPort)
        {
            if (string.IsNullOrWhiteSpace(configSource))
            {
                throw new ArgumentNullException(nameof(configSource));
            }

            Settings = new EmailSettings(smtpServer, smtpUserName, smtpPassword, smtpServerPort);

            var serializer = new XmlSerializer(typeof(MailConfiguration));
            using var fileStream = new FileStream(configSource, FileMode.Open);
            _mailConfiguration = ((MailConfiguration)serializer.Deserialize(fileStream));
        }

        public EmailHelper WithTls()
        {
            Settings.EnableTls = true;
            return this;
        }

        public EmailHelper WithSenderName(string name)
        {
            Settings.SenderName = name;
            return this;
        }

        public EmailHelper WithDisplayName(string displayName)
        {
            Settings.EmailDisplayName = displayName;
            return this;
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

            CurrentEngine = CurrentEngine switch
            {
                null when templateEngine == null => throw new Exception("TemplateEngine must be specified."),
                null when true => templateEngine,
                _ => CurrentEngine
            };

            // create mail message
            var messageToSend = new MimeMessage
            {
                Sender = new MailboxAddress(Settings.SenderName, Settings.SmtpUserName),
                Subject = CurrentEngine.Format(() => new StringBuilder(CurrentEngine.TextProvider.Subject)).Trim(),
            };
            messageToSend.From.Add(new MailboxAddress(Settings.EmailDisplayName, Settings.SmtpUserName));
            var bodyText = CurrentEngine.Format(() => new StringBuilder(CurrentEngine.TextProvider.Text)).Trim();
            if (CurrentEngine.TextProvider is { } templateMailMessage && templateMailMessage.IsHtml)
            {
                messageToSend.Body = new TextPart(TextFormat.Html) { Text = bodyText };
            }
            else
            {
                messageToSend.Body = new TextPart(TextFormat.Plain) { Text = bodyText };
            }

            if (_mailConfiguration.CommonConfiguration.OverrideToAddress)
            {
                messageToSend.To.Add(MailboxAddress.Parse(_mailConfiguration.CommonConfiguration.ToAddress));
            }
            else
            {
                foreach (string add in enumerable)
                {
                    messageToSend.To.Add(MailboxAddress.Parse(add));
                }
            }

            if (!string.IsNullOrEmpty(ccAddress))
            {
                messageToSend.Cc.Add(MailboxAddress.Parse(ccAddress));
            }

            try
            {
                using var smtp = new MailKit.Net.Smtp.SmtpClient();
                smtp.MessageSent += (sender, args) =>
                {
                    OnEmailSent(messageToSend, args.Response);
                };

                smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;
                await smtp.ConnectAsync(
                    Settings.SmtpServer, 
                    Settings.SmtpServerPort,
                    Settings.EnableTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
                if (!string.IsNullOrEmpty(Settings.SmtpUserName))
                {
                    await smtp.AuthenticateAsync(Settings.SmtpUserName, Settings.SmtpPassword);
                }

                await smtp.SendAsync(messageToSend);
                await smtp.DisconnectAsync(true);
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