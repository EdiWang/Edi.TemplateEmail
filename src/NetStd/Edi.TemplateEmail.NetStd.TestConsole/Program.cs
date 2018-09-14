using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Edi.TemplateEmail.NetStd.Models;

namespace Edi.TemplateEmail.NetStd.TestConsole
{
    class Program
    {
        public static EmailHelper EmailHelper { get; set; }

        static async Task Main(string[] args)
        {
            var configSource = $"{Directory.GetCurrentDirectory()}\\mailConfiguration.config";

            if (EmailHelper == null)
            {
                var settings = new EmailSettings("smtp-mail.outlook.com", "Edi.Test@outlook.com", "", 587)
                {
                    EnableSsl = true,
                    EmailDisplayName = "Edi.TemplateEmail.NetStd",
                    SenderName = "Test Sender"
                };

                EmailHelper = new EmailHelper(configSource, settings);
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
            bool isOk = true;

            var pipeline = new TemplatePipeline().Map("MachineName", Environment.MachineName)
                .Map("SmtpServer", EmailHelper.Settings.SmtpServer)
                .Map("SmtpServerPort", EmailHelper.Settings.SmtpServerPort)
                .Map("SmtpUserName", EmailHelper.Settings.SmtpUserName)
                .Map("EmailDisplayName", EmailHelper.Settings.EmailDisplayName)
                .Map("EnableSsl", EmailHelper.Settings.EnableSsl);

            EmailHelper.EmailFailed += (s, e) =>
            {
                isOk = false;
            };

            await EmailHelper.ApplyTemplate("TestMail", pipeline).SendMailAsync("Edi.Wang@outlook.com");
        }
    }
}
