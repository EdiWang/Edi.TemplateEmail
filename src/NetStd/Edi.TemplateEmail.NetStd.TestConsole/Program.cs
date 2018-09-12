using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Edi.TemplateEmail.NetStd.Models;

namespace Edi.TemplateEmail.NetStd.TestConsole
{
    class Program
    {
        public static EmailHelper EmailHelper { get; set; }

        static async Task Main(string[] args)
        {
            if (EmailHelper == null)
            {
                EmailHelper = new EmailHelper(new EmailSettings
                {
                    SmtpServer = "smtp-mail.outlook.com",
                    SmtpUserName = "Edi.Test@outlook.com",
                    SmtpPassword = "",
                    SmtpServerPort = 587,
                    EnableSsl = true,
                    EmailDisplayName = "Edi.TemplateEmail.NetStd"
                }, "Edi.TemplateEmail.NetStd.TestConsole");
                //EmailHelper.EmailCompleted += (sender, message, args) => WriteEmailLog(sender as MailMessage, message);
            }

            try
            {
                Console.WriteLine("Sending Email...");
                await TestSendTestMail();
                Console.WriteLine("Success");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.ReadLine();
        }

        public static async Task TestSendTestMail()
        {
            // TODO: fuck this into xml config source
            var mailConfig = new MailConfiguration()
            {
                CommonConfiguration = new MailCommonConfiguration()
                {
                    OverrideToAddress = false,
                    ToAddress = "test@hello.com"
                },
                MailMessages = new List<MailMessageConfiguration>()
                {
                    new MailMessageConfiguration()
                    {
                        IsHtml = true,
                        MessageBody = "Hello {MachineName.Value}",
                        MessageSubject = "Test Mail on {MachineName.Value}",
                        MessageType = "TestMail"
                    }
                }
            };

            var pipeline = new TemplatePipeline().Map("MachineName", Environment.MachineName)
                .Map("SmtpServer", EmailHelper.Settings.SmtpServer)
                .Map("SmtpServerPort", EmailHelper.Settings.SmtpServerPort)
                .Map("SmtpUserName", EmailHelper.Settings.SmtpUserName)
                .Map("EmailDisplayName", EmailHelper.Settings.EmailDisplayName)
                .Map("EnableSsl", EmailHelper.Settings.EnableSsl);
            bool isOk = true;
            EmailHelper.EmailFailed += (s, e) =>
            {
                isOk = false;
            };

            await EmailHelper.ApplyTemplate(mailConfig, "TestMail", pipeline).SendMailAsync("Edi.Wang@outlook.com");
        }
    }
}
