using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Edi.TemplateEmail.NetStd
{
    public class EmailHelper
    {
        #region Events

        /// <summary>
        ///     Occurs after an e-mail blow up. The sender is the MailMessage object.
        /// </summary>
        public event EmailFailedEventHandler EmailFailed;

        public delegate void EmailFailedEventHandler(object sender, EmailStateEventArgs e);

        /// <summary>
        ///     Occurs after an e-mail has been sent. The sender is the MailMessage object.
        /// </summary>
        public event EmailSentEventHandler EmailSent;

        public delegate void EmailSentEventHandler(object sender, EmailStateEventArgs e);

        /// <summary>
        ///     Occurs after an e-mail has been sent no matter blow up or not. The sender is the MailMessage object.
        /// </summary>
        public event EmailCompletedEventHandler EmailCompleted;

        public delegate void EmailCompletedEventHandler(object sender, EmailStateEventArgs e);

        /// <summary>
        /// The on email failed.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        private void OnEmailFailed(MailMessage message)
        {
            EmailFailed?.Invoke(message, new EmailStateEventArgs(false, null));
        }

        /// <summary>
        /// The on email sent.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
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

        public EmailSettings Settings { get; set; }
        public Action AfterCompleteAction { get; set; }
        public string ToAddressOverride { get; set; }
        public TemplateEngine CurrentEngine { get; set; }

        #endregion

        #region Builder Methods

        public EmailHelper AfterComplete(Action afterCompleteAction)
        {
            AfterCompleteAction = afterCompleteAction;
            return this;
        }

        public EmailHelper SendAs(string senderName)
        {
            Settings.SenderName = senderName;
            return this;
        }

        #endregion

        public EmailHelper(EmailSettings settings)
        {
            Settings = settings;
        }

        public EmailHelper(string smtpServer, string smtpUserName, string smtpPassword, int smtpServerPort, bool enableSSl, string emailDisplayName, bool emailWithSystemInfo = false)
        {
            Settings.SmtpServer = smtpServer;
            Settings.SmtpUserName = smtpUserName;
            Settings.SmtpPassword = smtpPassword;
            Settings.SmtpServerPort = smtpServerPort;
            Settings.EnableSsl = enableSSl;
            Settings.EmailDisplayName = emailDisplayName;
        }

        public EmailHelper ApplyTemplate(string mailType, TemplatePipeline pipeline, Action emailSentAction = null, Action emailFailedAction = null)
        {
            var mailConfig = XmlConfigMapper.XmlSection<MailConfiguration>.GetSection("mailConfiguration");
            if (mailConfig.CommonConfiguration.OverrideToAddress)
            {
                ToAddressOverride = mailConfig.CommonConfiguration.ToAddress;
            }

            var messageToPersonalize = new TemplateMailMessage(mailType);
            if (messageToPersonalize.Loaded)
            {
                var engine = new TemplateEngine(messageToPersonalize, pipeline);
                CurrentEngine = engine;
                return this;
            }
            return null;
        }

        public async Task SendMailAsync(string toAddress,
            TemplateEngine templateEngine = null, string ccAddress = null, List<Attachment> attachments = null)
        {
            await SendMailAsync(new[] { toAddress }, templateEngine, ccAddress, attachments);
        }

        public async Task SendMailAsync(IEnumerable<string> toAddress,
            TemplateEngine templateEngine = null, string ccAddress = null, List<Attachment> attachments = null)
        {
            // create smtp client
            var smtp = new SmtpClient(Settings.SmtpServer);
            if (!string.IsNullOrEmpty(Settings.SmtpUserName))
            {
                smtp.UseDefaultCredentials = Settings.UseDefaultCredentials;
                smtp.Credentials = new NetworkCredential(
                    Settings.SmtpUserName, Settings.SmtpPassword);
            }
            smtp.Port = Settings.SmtpServerPort;
            smtp.EnableSsl = Settings.EnableSsl;

            // check engine instance
            if (CurrentEngine == null && templateEngine == null)
            {
                throw new Exception("TemplateEngine must be specified.");
            }

            if (CurrentEngine == null && templateEngine != null)
            {
                CurrentEngine = templateEngine;
            }

            // create mail message
            var messageToSend = new MailMessage
            {
                Sender = new MailAddress(Settings.SmtpUserName, Settings.SenderName),
                From = new MailAddress(Settings.SmtpUserName, Settings.EmailDisplayName),
                Body = CurrentEngine.Format(() => new StringBuilder(CurrentEngine.TextProvider.Text)).Trim(),
                Subject = CurrentEngine.Format(() => new StringBuilder(CurrentEngine.TextProvider.Subject)).Trim(),
                IsBodyHtml = CurrentEngine.TextProvider is TemplateMailMessage templateMailMessage && templateMailMessage.IsHtml
            };

            foreach (var add in toAddress)
            {
                messageToSend.To.Add(!string.IsNullOrEmpty(ToAddressOverride) ? ToAddressOverride : add);
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
            catch (SmtpException ex)
            {
                OnEmailFailed(messageToSend);
                throw;
            }
            finally
            {
                AfterCompleteAction?.Invoke();
                OnEmailComplete(messageToSend);
                messageToSend.Dispose();
            }
        }
    }
}