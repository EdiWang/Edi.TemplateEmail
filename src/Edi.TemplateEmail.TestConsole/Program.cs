using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using MailKit.Security;
using Spectre.Console;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var smtpServer = AnsiConsole.Ask<string>("SMTP server (e.g. smtp.example.com): ");
var userName = AnsiConsole.Prompt(new TextPrompt<string>("SMTP user name (leave blank for anonymous): ").AllowEmpty());
var password = string.IsNullOrEmpty(userName)
    ? null
    : AnsiConsole.Prompt(new TextPrompt<string>("SMTP password:").Secret());
var port = AnsiConsole.Ask<int>("Port number (e.g. 587 or 465): ");
var socketOptions = AnsiConsole.Prompt(
    new SelectionPrompt<SecureSocketOptions>()
        .Title("SMTP security (587: StartTls, 465: SslOnConnect):")
        .AddChoices(SecureSocketOptions.StartTls, SecureSocketOptions.SslOnConnect,
            SecureSocketOptions.StartTlsWhenAvailable, SecureSocketOptions.Auto, SecureSocketOptions.None));
var toAddress = AnsiConsole.Ask<string>("To email address: ");
var fromAddress = AnsiConsole.Ask<string>("From email address: ");
var displayName = AnsiConsole.Ask<string>("Sender display name: ");

var configSource = $"{Directory.GetCurrentDirectory()}\\mailConfiguration.xml";

var emailSettings = new EmailSettings
{
    FromAddress = fromAddress,
    EmailDisplayName = displayName,
    SmtpSettings = new(smtpServer, userName, password, port) { SocketOptions = socketOptions }
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
            ("SocketOptions", emailSettings.SmtpSettings.SocketOptions)
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
