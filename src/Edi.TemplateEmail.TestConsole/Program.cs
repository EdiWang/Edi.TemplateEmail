using Edi.TemplateEmail;

// Change these values
var smtpServer = "smtp-mail.outlook.com";
var userName = "Edi.Test@outlook.com";
var password = "";
var port = 587;
var toAddress = "Edi.Wang@outlook.com";

var configSource = $"{Directory.GetCurrentDirectory()}\\mailConfiguration.xml";
var emailHelper = new EmailHelper(configSource, smtpServer, userName, password, port)
     .WithTls()
     .WithSenderName("Test Sender")
     .WithDisplayName("Edi.TemplateEmail.TestConsole");

emailHelper.EmailSent += (sender, eventArgs) =>
{
    Console.WriteLine($"Email is sent, Success: {eventArgs.IsSuccess}, Response: {eventArgs.ServerResponse}");
};
emailHelper.EmailFailed += (sender, eventArgs) => Console.WriteLine("Failed");
emailHelper.EmailCompleted += (sender, e) => Console.WriteLine("Completed.");

try
{
    Console.WriteLine("Sending Email...");

    await emailHelper.ForType("TestMail")
                     .Map("MachineName", Environment.MachineName)
                     .Map("SmtpServer", emailHelper.Settings.SmtpServer)
                     .Map("SmtpServerPort", emailHelper.Settings.SmtpServerPort)
                     .Map("SmtpUserName", emailHelper.Settings.SmtpUserName)
                     .Map("EmailDisplayName", emailHelper.Settings.EmailDisplayName)
                     .Map("EnableTls", emailHelper.Settings.EnableTls)
                     .SendAsync(toAddress);
}
catch (Exception e)
{
    Console.WriteLine(e);
}

Console.ReadLine();