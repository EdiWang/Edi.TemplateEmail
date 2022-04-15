Edi.TemplateEmail
===============================

This library enable you to configure email in XML template and send the email in your .NET Core applications.

![.NET Core](https://github.com/EdiWang/Edi.TemplateEmail/workflows/.NET%20Core/badge.svg)

[![NuGet][main-nuget-badge]][main-nuget]

[main-nuget]: https://www.nuget.org/packages/Edi.TemplateEmail/
[main-nuget-badge]: https://img.shields.io/nuget/v/Edi.TemplateEmail.svg?style=flat-square&label=nuget

## Install

```
dotnet add package Edi.TemplateEmail
```

## Usage


### Step 1: Put a mailConfiguration.xml somewhere you like

```xml
<?xml version="1.0"?>
<MailConfiguration>
  <CommonConfiguration OverrideToAddress="false" ToAddress="overridetest@test.com" />
  <MailMessage MessageType="TestMail" IsHtml="true">
    <MessageSubject>Test Mail on {MachineName.Value}</MessageSubject>
    <MessageBody>
      <![CDATA[
Mail configuration on {MachineName.Value} is good: <br />
Smtp Server: {SmtpServer.Value}<br />
Smtp Port: {SmtpServerPort.Value}<br />
Smtp Username: {SmtpUserName.Value}<br />
Email Display Name: {EmailDisplayName.Value}<br />
Enable SSL: {EnableSsl.Value}<br />
      ]]>
    </MessageBody>
  </MailMessage>
</MailConfiguration>
```

### Step 2:

Initialize the `EmailHelper` by your mail server settings

```
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
```

You can also add event handlers

```
emailHelper.EmailSent += (sender, eventArgs) =>
{
    Console.WriteLine($"Email is sent, Success: {eventArgs.IsSuccess}, Response: {eventArgs.ServerResponse}");
};
emailHelper.EmailFailed += (sender, eventArgs) => Console.WriteLine("Failed");
emailHelper.EmailCompleted += (sender, e) => Console.WriteLine("Completed.");
```

### Step 3: Map the values and send Email

```
await emailHelper.ForType("TestMail")
                 .Map("MachineName", Environment.MachineName)
                 .Map("SmtpServer", emailHelper.Settings.SmtpServer)
                 .Map("SmtpServerPort", emailHelper.Settings.SmtpServerPort)
                 .Map("SmtpUserName", emailHelper.Settings.SmtpUserName)
                 .Map("EmailDisplayName", emailHelper.Settings.EmailDisplayName)
                 .Map("EnableTls", emailHelper.Settings.EnableTls)
                 .SendAsync(toAddress);
```
