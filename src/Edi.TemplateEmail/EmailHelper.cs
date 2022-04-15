using Edi.TemplateEmail.Models;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Edi.TemplateEmail
{
    public delegate void EmailCompletedEventHandler(object sender, EmailStateEventArgs e);
    public delegate void EmailSentEventHandler(object sender, EmailStateEventArgs e);
    public delegate void EmailFailedEventHandler(object sender, EmailStateEventArgs e);

    public class EmailHelper : IEmailHelper
    {
        #region Events

        public event EmailFailedEventHandler EmailFailed;
        public event EmailSentEventHandler EmailSent;
        public event EmailCompletedEventHandler EmailCompleted;

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

        public EmailSettings Settings { get; private set; }

        public TemplateEngine CurrentEngine { get; private set; }

        public TemplatePipeline Pipeline { get; private set; }

        #endregion

        private MailConfiguration _mailConfiguration;
        private string _mailType;

        public EmailHelper() { }

        public EmailHelper(string smtpServer, string smtpUserName, string smtpPassword, int smtpServerPort)
        {
            WithSettings(smtpServer, smtpUserName, smtpPassword, smtpServerPort);
        }

        public EmailHelper(MailConfiguration configuration, string smtpServer, string smtpUserName, string smtpPassword, int smtpServerPort)
        {
            WithSettings(smtpServer, smtpUserName, smtpPassword, smtpServerPort);
            WithConfig(configuration);
        }

        public EmailHelper(string configPath, string smtpServer, string smtpUserName, string smtpPassword, int smtpServerPort)
        {
            WithSettings(smtpServer, smtpUserName, smtpPassword, smtpServerPort);
            WithConfig(configPath);
        }

        public EmailHelper WithSettings(string smtpServer, string smtpUserName, string smtpPassword, int smtpServerPort)
        {
            Settings = new EmailSettings(smtpServer, smtpUserName, smtpPassword, smtpServerPort);
            return this;
        }

        public EmailHelper WithConfig(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
            {
                throw new ArgumentNullException(nameof(configPath));
            }

            var serializer = new XmlSerializer(typeof(MailConfiguration));
            using var fileStream = new FileStream(configPath, FileMode.Open);
            _mailConfiguration = (MailConfiguration)serializer.Deserialize(fileStream);
            return this;
        }

        public EmailHelper WithConfig(MailConfiguration configuration)
        {
            _mailConfiguration = configuration;
            return this;
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

        public EmailHelper ForType(string mailType)
        {
            _mailType = mailType;
            Pipeline = new TemplatePipeline();
            return this;
        }

        public EmailHelper Map(string name, object value)
        {
            Pipeline.Map(name, value);
            return this;
        }

        public async Task SendAsync(string toAddress, string ccAddress = null)
        {
            if (string.IsNullOrWhiteSpace(toAddress))
            {
                throw new ArgumentNullException(nameof(toAddress));
            }
            await SendAsync(new[] { toAddress }, ccAddress);
        }

        public async Task SendAsync(IEnumerable<string> toAddress, string ccAddress = null)
        {
            var messageToPersonalize = new TemplateMailMessage(_mailConfiguration, _mailType);
            if (messageToPersonalize.Loaded)
            {
                var engine = new TemplateEngine(messageToPersonalize, Pipeline);
                CurrentEngine = engine;
            }

            var enumerable = toAddress as string[] ?? toAddress.ToArray();
            if (!enumerable.Any())
            {
                throw new ArgumentNullException(nameof(toAddress));
            }

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