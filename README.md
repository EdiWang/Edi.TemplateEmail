Edi.TemplateEmail
===============================

This library enable you to configure email in XML template and send the email in your .NET applications.

> SMTP sending is available through the `Edi.TemplateEmail.Smtp` package.

[![NuGet][main-nuget-badge]][main-nuget]

[main-nuget]: https://www.nuget.org/packages/Edi.TemplateEmail/
[main-nuget-badge]: https://img.shields.io/nuget/v/Edi.TemplateEmail.svg?style=flat-square&label=nuget

## Install

```
dotnet add package Edi.TemplateEmail
dotnet add package Edi.TemplateEmail.Smtp
```

## Usage


### Step 1: Put a mailConfiguration.xml somewhere you like

```xml
<?xml version="1.0"?>
<MailConfiguration>
  <MailMessage MessageType="TestMail" IsHtml="true">
    <MessageSubject>Test Mail on {MachineName.Value}</MessageSubject>
    <MessageBody>
      <![CDATA[
Mail configuration on {MachineName.Value} is good: <br />
Smtp Server: {SmtpServer.Value}<br />
Smtp Port: {SmtpServerPort.Value}<br />
Smtp Username: {SmtpUserName.Value}<br />
Email Display Name: {EmailDisplayName.Value}<br />
Enable TLS: {EnableTls.Value}<br />
      ]]>
    </MessageBody>
  </MailMessage>
</MailConfiguration>
```

### Step 2:

Initialize the `EmailHelper` by your mail server settings

```
// Change these values
var smtpServer = "smtp.example.com";
var userName = "test1@example.com";
var password = "********";
var port = 25;
var toAddress = "test2@test.com";
var senderName = "test1@example.com";
var displayName = "Test Sender";

var configSource = $"{Directory.GetCurrentDirectory()}\\mailConfiguration.xml";
var emailSettings = new EmailSettings
{
    SenderName = senderName,
    EmailDisplayName = displayName,
    SmtpSettings = new(smtpServer, userName, password, port)
};

var emailHelper = new EmailHelper(configSource);
```

### Step 3: Map the values and send Email

```
var message = emailHelper.ForType("TestMail")
    .Map("MachineName", Environment.MachineName)
    .Map("SmtpServer", emailSettings.SmtpSettings.SmtpServer)
    .Map("SmtpServerPort", emailSettings.SmtpSettings.SmtpServerPort)
    .Map("SmtpUserName", emailSettings.SmtpSettings.SmtpUserName)
    .Map("EmailDisplayName", emailSettings.EmailDisplayName)
    .Map("EnableTls", emailSettings.SmtpSettings.EnableTls)
    .BuildMessage([toAddress]);

var result = await message.SendAsync(emailSettings);
```
