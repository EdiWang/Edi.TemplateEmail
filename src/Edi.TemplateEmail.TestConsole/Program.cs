using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
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

var emailSettings = new EmailSettings
{
    SenderName = senderName,
    EmailDisplayName = displayName,
    SmtpSettings = new(smtpServer, userName, password, port)
};

var emailHelper = new EmailHelper(configSource);

try
{
    var message = emailHelper.ForType("TestMail")
        .MapRange(
            ("MachineName", Environment.MachineName),
            ("SmtpServer", emailSettings.SmtpSettings.SmtpServer),
            ("SmtpServerPort", emailSettings.SmtpSettings.SmtpServerPort),
            ("SmtpUserName", emailSettings.SmtpSettings.SmtpUserName),
            ("EmailDisplayName", emailSettings.EmailDisplayName),
            ("EnableTls", emailSettings.SmtpSettings.EnableTls)
        )
        .BuildMessage([toAddress]);

    var result = await message.SendAsync(emailSettings);
    Console.WriteLine($"Email is sent. Response: {result}");
}
catch (Exception e)
{
    AnsiConsole.WriteException(e);
}

Console.ReadLine();