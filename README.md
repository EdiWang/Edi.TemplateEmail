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
SMTP security: {SocketOptions.Value}<br />
      ]]>
    </MessageBody>
  </MailMessage>
</MailConfiguration>
```

### Step 2:

Initialize the `EmailHelper` by your mail server settings

```
using MailKit.Security;

// Change these values
var smtpServer = "smtp.example.com";
var userName = "test1@example.com";
var password = "********";
var port = 587;
var toAddress = "test2@test.com";
var fromAddress = "notifications@example.com";
var displayName = "Test Sender";

var configSource = $"{Directory.GetCurrentDirectory()}\\mailConfiguration.xml";
var emailSettings = new EmailSettings
{
    FromAddress = fromAddress,
    EmailDisplayName = displayName,
    SmtpSettings = new(smtpServer, userName, password, port)
    {
        SocketOptions = SecureSocketOptions.StartTls
    }
};

var emailHelper = new EmailHelper(configSource);
```

`FromAddress` is required and is independent of the SMTP login. `EmailDisplayName` is the display name for this address. Set the SMTP user name to `null` or an empty string to send without authentication; authenticated user names do not have to be email addresses.

When sending on behalf of another address, optionally set `SenderAddress` and its display name, `SenderName`. The `Sender` header is omitted when `SenderAddress` is not configured:

```csharp
emailSettings.SenderAddress = "mailer@example.com";
emailSettings.SenderName = "Mail Service";
```

When upgrading, explicitly set `FromAddress` (usually to the email address previously used as the SMTP user name). Set `SenderAddress` as well if you need the previous `Sender` header; `SenderName` alone no longer adds it.

Set `SmtpSettings.SocketOptions` to the connection mode required by your SMTP server:

| Mode | Behavior |
| --- | --- |
| `StartTls` (default) | Requires STARTTLS; fails if unsupported. Commonly used on port 587. |
| `SslOnConnect` | Uses TLS immediately upon connection. Commonly used on port 465. |
| `None` | Uses an unencrypted connection. |
| `StartTlsWhenAvailable` | Uses STARTTLS if supported; otherwise continues unencrypted. |
| `Auto` | Selects `SslOnConnect` on port 465 and `StartTlsWhenAvailable` on other ports. |

For port 465, set the port to `465` and `SocketOptions` to `SecureSocketOptions.SslOnConnect`. `Auto` and `StartTlsWhenAvailable` permit unencrypted connections; choose `StartTls` or `SslOnConnect` when encryption is required. See the [MailKit connection documentation](https://mimekit.net/docs/html/M_MailKit_Net_Smtp_SmtpClient_ConnectAsync_2.htm).

`EnableTls` has been removed. Replace `EnableTls = true` with `SocketOptions = SecureSocketOptions.StartTls`. The previous `EnableTls = false` behavior corresponds to `SocketOptions = SecureSocketOptions.Auto`; use `None` only when an unencrypted connection is intended. `SkipCertificateValidation` remains disabled by default and is for development only.

### Step 3: Map the values and send Email

```
var message = emailHelper.ForType("TestMail")
    .Map("MachineName", Environment.MachineName)
    .Map("SmtpServer", emailSettings.SmtpSettings.SmtpServer)
    .Map("SmtpServerPort", emailSettings.SmtpSettings.SmtpServerPort)
    .Map("SmtpUserName", emailSettings.SmtpSettings.SmtpUserName)
    .Map("EmailDisplayName", emailSettings.EmailDisplayName)
    .Map("SocketOptions", emailSettings.SmtpSettings.SocketOptions)
    .BuildMessage([toAddress]);

var result = await message.SendAsync(emailSettings);
```
