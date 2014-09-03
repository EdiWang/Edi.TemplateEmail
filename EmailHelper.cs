using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Edi.XmlConfigMapper;

namespace Edi.TemplateEmail
{
    public class EmailStateEventArgs : EventArgs
    {
        public bool IsSuccess { get; set; }

        public Guid? EmailId { get; set; }

        public EmailStateEventArgs(bool isSuccess, Guid? emailId)
        {
            IsSuccess = isSuccess;
            EmailId = emailId;
        }
    }

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

        public delegate void EmailCompletedEventHandler(object sender, string exceptionMessage, EmailStateEventArgs e);

        /// <summary>
        /// The on email failed.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        private void OnEmailFailed(MailMessage message)
        {
            if (EmailFailed != null)
            {
                EmailFailed(message, new EmailStateEventArgs(false, null));
            }
        }

        /// <summary>
        /// The on email sent.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        private void OnEmailSent(MailMessage message)
        {
            if (EmailSent != null)
            {
                EmailSent(message, new EmailStateEventArgs(true, null));
            }
        }

        private void OnEmailComplete(MailMessage message, string exceptionMessage)
        {
            if (EmailCompleted != null)
            {
                EmailCompleted(message, exceptionMessage, new EmailStateEventArgs(true, null));
            }
        }

        #endregion Events

        #region Properties

        public EmailSettings Settings { get; set; }
        public Action AfterCompleteAction { get; set; }
        public Action<string, Exception> LogExceptionAction { get; set; }
        public string ToAddressOverride { get; set; }
        public ITemplateEngine CurrentEngine { get; set; }

        #endregion

        #region Builder Methods

        public EmailHelper AfterComplete(Action afterCompleteAction)
        {
            AfterCompleteAction = afterCompleteAction;
            return this;
        }

        public EmailHelper LogExceptionWith(Action<string, Exception> logExceptionAction)
        {
            LogExceptionAction = logExceptionAction;
            return this;
        }

        public EmailHelper UseSignature(string signatureContent)
        {
            Settings.IncludeSignature = true;
            Settings.SignatureContent = signatureContent;
            return this;
        }

        public EmailHelper WithFooter(string footerContent)
        {
            Settings.IncludeFooter = true;
            Settings.FooterContent = footerContent;
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
            Settings.EmailWithSystemInfo = emailWithSystemInfo;
        }

        public EmailHelper ApplyTemplate(string mailType, TemplatePipeline pipeline, Action emailSentAction = null, Action emailFailedAction = null)
        {
            var mailConfig = XmlSection<MailConfiguration>.GetSection("mailConfiguration");
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
            ITemplateEngine templateEngine = null, string ccAddress = null, List<Attachment> attachments = null)
        {
            await SendMailAsync(new[] { toAddress }, templateEngine, ccAddress, attachments);
        }

        public async Task SendMailAsync(IEnumerable<string> toAddress,
            ITemplateEngine templateEngine = null, string ccAddress = null, List<Attachment> attachments = null)
        {
            var errorMsg = new StringBuilder();

            // create smtp client
            var smtp = new SmtpClient(Settings.SmtpServer);
            if (!string.IsNullOrEmpty(Settings.SmtpUserName))
            {
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
            var templateMailMessage = CurrentEngine.TextProvider as TemplateMailMessage;
            var messageToSend = new MailMessage
            {
                Sender = new MailAddress(Settings.SmtpUserName, Settings.SenderName),
                From = new MailAddress(Settings.SmtpUserName, Settings.EmailDisplayName),
                Body = CurrentEngine.Format(() => new StringBuilder(CurrentEngine.TextProvider.Text)).Trim(),
                Subject = CurrentEngine.Format(() => new StringBuilder(CurrentEngine.TextProvider.Subject)).Trim(),
                IsBodyHtml = templateMailMessage != null && templateMailMessage.IsHtml
            };

            foreach (var add in toAddress)
            {
                messageToSend.To.Add(!string.IsNullOrEmpty(ToAddressOverride) ? ToAddressOverride : add);
            }

            if (Settings.IncludeSignature)
            {
                messageToSend.Body += string.Format("<p>{0}</p>", Settings.SignatureContent);
            }

            if (!string.IsNullOrEmpty(ccAddress))
            {
                messageToSend.CC.Add(ccAddress);
            }

            if (null != attachments && attachments.Any())
            {
                attachments.ForEach(attachment => messageToSend.Attachments.Add(attachment));
            }

            if (Settings.EmailWithSystemInfo)
            {
                try
                {
                    const string footer = "\r\n\r\nServer Name:{0}\r\nServer IP Address:{1}";
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    string ipAddresses = string.Empty;
                    foreach (var ip in host.AddressList)
                    {
                        if (ipAddresses.Length > 0)
                        {
                            ipAddresses += "; ";
                        }

                        if (!ip.IsIPv6LinkLocal && !ip.IsIPv6Multicast && !ip.IsIPv6SiteLocal)
                        {
                            ipAddresses += ip.ToString();
                        }
                    }
                    messageToSend.Body += string.Format(footer, host.HostName, ipAddresses);
                }
                catch
                {
                    // Eat up the exception, pretend to be unaware of it :) 
                }
            }

            if (Settings.IncludeFooter)
            {
                messageToSend.Body += string.Format(Settings.FooterContent);
            }

            try
            {
                await smtp.SendMailAsync(messageToSend);
                OnEmailSent(messageToSend);
            }
            catch (SmtpException ex)
            {
                OnEmailFailed(messageToSend);

                errorMsg.Append("Error sending email in SendMailAsync: ");
                Exception current = ex;

                while (current != null)
                {
                    LogExceptionAction("Error sending email in SendMailAsync.", current);

                    if (errorMsg.Length > 0) { errorMsg.Append(" "); }
                    errorMsg.Append(current.Message);
                    current = current.InnerException;
                }
            }
            finally
            {
                if (null != AfterCompleteAction)
                {
                    AfterCompleteAction();
                }

                OnEmailComplete(messageToSend, errorMsg.ToString());

                // Remove the pointer to the message object so the GC can close the thread.
                messageToSend.Dispose();
            }
        }
    }
}