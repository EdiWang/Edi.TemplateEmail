using Edi.TemplateEmail;
using Spectre.Console;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var smtpServer = AnsiConsole.Ask<string>("SMTP server (e.g. smtp-mail.outlook.com): ");
var userName = AnsiConsole.Ask<string>("SMTP user name: ");
var password = AnsiConsole.Prompt(new TextPrompt<string>("SMTP password:").Secret());
var port = AnsiConsole.Ask<int>("Port number (e.g. 587): ");
var toAddress = AnsiConsole.Ask<string>("To email address: ");
var senderName = AnsiConsole.Ask<string>("Sender name: ");
var displayName = AnsiConsole.Ask<string>("Sender display name: ");

var configSource = $"{Directory.GetCurrentDirectory()}\\mailConfiguration.xml";
var emailHelper = new EmailHelper(configSource, smtpServer, userName, password, port)
     .WithTls()
     .WithSenderName(senderName)
     .WithDisplayName(displayName);

emailHelper.EmailSent += (sender, eventArgs) =>
{
    AnsiConsole.MarkupLine($"Email is sent. Success: [blue]{eventArgs.IsSuccess}[/], Response: [blue]{eventArgs.ServerResponse}[/]");
};
emailHelper.EmailFailed += (sender, eventArgs) => AnsiConsole.MarkupLine("[red]Failed[/]");
emailHelper.EmailCompleted += (sender, e) => AnsiConsole.WriteLine("Completed.");

try
{
    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .StartAsync($"Sending email...", async _ =>
        {
            await emailHelper.ForType("TestMail")
                .Map("MachineName", Environment.MachineName)
                .Map("SmtpServer", emailHelper.Settings.SmtpServer)
                .Map("SmtpServerPort", emailHelper.Settings.SmtpServerPort)
                .Map("SmtpUserName", emailHelper.Settings.SmtpUserName)
                .Map("EmailDisplayName", emailHelper.Settings.EmailDisplayName)
                .Map("EnableTls", emailHelper.Settings.EnableTls)
                .SendAsync(toAddress);
        });
}
catch (Exception e)
{
    AnsiConsole.WriteException(e);
}

Console.ReadLine();