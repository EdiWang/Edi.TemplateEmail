using System;
using System.Diagnostics;
using System.Threading.Tasks;

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
                }).SendAs("Edi.TemplateEmail.NetStd.TestConsole");
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
            var pipeline = new TemplatePipeline().Map("MachineName", System.Environment.MachineName)
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

            await EmailHelper.ApplyTemplate("TestMail", pipeline).SendMailAsync("Edi.Wang@outlook.com");
        }
    }
}
