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

var settings = new EmailSettings
{
    SenderName = senderName,
    EmailDisplayName = displayName,
    SmtpSettings = new(smtpServer, userName, password, port)
};

var emailHelper = new EmailHelper(configSource, settings);

try
{
    var message = emailHelper.ForType("TestMail")
        //.Map("MachineName", Environment.MachineName)
        //.Map("SmtpServer", emailHelper.Settings.SmtpSettings.SmtpServer)
        //.Map("SmtpServerPort", emailHelper.Settings.SmtpSettings.SmtpServerPort)
        //.Map("SmtpUserName", emailHelper.Settings.SmtpSettings.SmtpUserName)
        //.Map("EmailDisplayName", emailHelper.Settings.EmailDisplayName)
        //.Map("EnableTls", emailHelper.Settings.SmtpSettings.EnableTls)
        .MapRange(
            ("MachineName", Environment.MachineName),
            ("SmtpServer", emailHelper.Settings.SmtpSettings.SmtpServer),
            ("SmtpServerPort", emailHelper.Settings.SmtpSettings.SmtpServerPort),
            ("SmtpUserName", emailHelper.Settings.SmtpSettings.SmtpUserName),
            ("EmailDisplayName", emailHelper.Settings.EmailDisplayName),
            ("EnableTls", emailHelper.Settings.SmtpSettings.EnableTls)
        )
        .BuildMessage([toAddress]);

    var result = await message.SendAsync();
    Console.WriteLine($"Email is sent. Response: {result}");
}
catch (Exception e)
{
    AnsiConsole.WriteException(e);
}

Console.ReadLine();