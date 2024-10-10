using Edi.TemplateEmail;
using Spectre.Console;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var smtpServer = AnsiConsole.Ask<string>("SMTP server (e.g. smtp.example.com): ");
var userName = AnsiConsole.Ask<string>("SMTP user name: ");
var password = AnsiConsole.Prompt(new TextPrompt<string>("SMTP password:").Secret());
var port = AnsiConsole.Ask<int>("Port number (e.g. 25): ");
var toAddress = AnsiConsole.Ask<string>("To email address: ");
var senderName = AnsiConsole.Ask<string>("Sender name: ");
var displayName = AnsiConsole.Ask<string>("Sender display name: ");

var configSource = $"{Directory.GetCurrentDirectory()}\\mailConfiguration.xml";
var emailHelper = new EmailHelper(configSource, new(smtpServer, userName, password, port)
{
    SenderName = senderName,
    EmailDisplayName = displayName
});

try
{
    var message = emailHelper.ForType("TestMail")
        .Map("MachineName", Environment.MachineName)
        .Map("SmtpServer", emailHelper.Settings.SmtpServer)
        .Map("SmtpServerPort", emailHelper.Settings.SmtpServerPort)
        .Map("SmtpUserName", emailHelper.Settings.SmtpUserName)
        .Map("EmailDisplayName", emailHelper.Settings.EmailDisplayName)
        .Map("EnableTls", emailHelper.Settings.EnableTls)
        .BuildMessage([toAddress]);

    var result = await message.SendAsync();
    Console.WriteLine($"Email is sent. Response: {result}");
}
catch (Exception e)
{
    AnsiConsole.WriteException(e);
}

Console.ReadLine();