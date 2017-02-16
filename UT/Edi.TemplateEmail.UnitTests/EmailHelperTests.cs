using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Edi.TemplateEmail.UnitTests
{
    [TestClass]
    public class EmailHelperTests
    {
        public static EmailHelper EmailHelper { get; }

        static EmailHelperTests()
        {
            if (EmailHelper == null)
            {
                EmailHelper = new EmailHelper(new EmailSettings
                {
                    SmtpServer = "smtp.live.com",
                    SmtpUserName = "Edi.Test@outlook.com",
                    SmtpPassword = "",
                    SmtpServerPort = 25,
                    EnableSsl = true,
                    EmailDisplayName = "Edi.TemplateEmail"
                })
                .SendAs("Edi.TemplateEmail.UnitTests")
                .LogExceptionWith((s, exception) => Debug.WriteLine(s + exception.Message));
                EmailHelper.UseSignature("signature test");
                EmailHelper.Settings.EmailWithSystemInfo = true;
                EmailHelper.WithFooter("<p>Footer Test</p>");
                //EmailHelper.EmailCompleted += (sender, message, args) => WriteEmailLog(sender as MailMessage, message);
            }
        }

        [TestMethod]
        public async Task TestSendTestMail()
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

            await EmailHelper.ApplyTemplate("TestMail", pipeline)
                             .SendMailAsync("Edi.Wang@outlook.com");

            Assert.IsTrue(isOk);
        }
    }
}
