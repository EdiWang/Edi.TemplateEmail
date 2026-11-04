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

### HTML body values and trusted markup

For XML templates with `IsHtml="true"`, ordinary `Map` and `MapRange` values are encoded with [.NET HTML encoding](https://learn.microsoft.com/en-us/dotnet/api/system.net.webutility.htmlencode?view=net-10.0) when inserted into the body. This includes object property values. Template markup remains unchanged; subjects and plain-text bodies keep their normal string values.

To insert a complete trusted HTML fragment, map it explicitly as `TrustedHtml`. For example, add this entry inside the XML `MailConfiguration` root:

```xml
<MailMessage MessageType="HtmlNotice" IsHtml="true">
  <MessageSubject>Hello {User.Name}</MessageSubject>
  <MessageBody><![CDATA[<p>Hello {User.Name}</p>{Footer.Value}]]></MessageBody>
</MailMessage>
```

```csharp
var message = emailHelper.ForType("HtmlNotice")
    .Map("User", new { Name = "Alice & Bob" })
    .Map("Footer", new TrustedHtml("<strong>Terms apply</strong>"))
    .BuildMessage([toAddress]);
```

The body becomes `<p>Hello Alice &amp; Bob</p><strong>Terms apply</strong>`. `MapRange` accepts `TrustedHtml` in the same way. Wrap only trusted or previously sanitized markup; `TrustedHtml` bypasses encoding and does not sanitize its input. The trust marker applies to the complete mapped entity. To insert a fragment stored in an object property, map that fragment separately and use its `{Entity.Value}` token. Wrappers inside an ordinary mapped object remain encoded.

Keep ordinary tokens in HTML text or quoted attributes. Templates are trusted markup; HTML encoding does not make tokens suitable for tag names, unquoted attributes, event handlers, styles, or executable content. The engine performs token replacement without parsing HTML contexts. See [Microsoft's guidance on output encoding contexts](https://learn.microsoft.com/en-us/aspnet/core/security/cross-site-scripting?view=aspnetcore-10.0).

For URL attributes, validate the supplied URL before mapping it. HTML encoding escapes separators and quotes but does not validate URL schemes or destinations. For a template containing `<a href="{Link.Value}">{Label.Value}</a>`, an application that permits only HTTPS links to `example.com` can use:

```csharp
var suppliedUrl = "https://example.com/account?view=profile&lang=en";
if (!Uri.TryCreate(suppliedUrl, UriKind.Absolute, out var link)
    || link.Scheme != Uri.UriSchemeHttps
    || !string.Equals(link.Host, "example.com", StringComparison.OrdinalIgnoreCase))
{
    throw new ArgumentException("Expected an HTTPS link to example.com.", nameof(suppliedUrl));
}

var builder = emailHelper.ForType("AccountLink")
    .MapRange(("Link", link.AbsoluteUri), ("Label", "Open account"));
```

Use an XML `MailMessage` named `AccountLink` with `IsHtml="true"` for that body and finish with `builder.BuildMessage(...)`. Apply your application's URL rules, including any port or path restrictions. Encode individual URL components when constructing a URL; let the builder HTML-encode the complete validated URL when inserting it into a quoted attribute.

#### Migrating HTML templates

This changes the output of existing HTML templates that map markup as ordinary strings. Move fixed markup into the XML body, or explicitly wrap each trusted fragment with `new TrustedHtml(...)`. Map ordinary text without pre-encoding; pre-encoded text is encoded again. No XML schema change is required. Subject and plain-text rendering are unchanged. Direct `TemplateEngine.Format(text)` calls retain unencoded formatting; use `Format(text, encodeHtml: true)` to encode mapped values when rendering HTML directly.

### SMTP results and cancellation

`SendAsync` returns the SMTP server's final response after MailKit completes sending. See the [MailKit SendAsync documentation](https://mimekit.net/docs/html/M_MailKit_Net_Smtp_SmtpClient_SendAsync_1.htm). A successful result records server acceptance; downstream delivery can still fail.

The cancellation token applies to connection, authentication, and sending. After a successful send, later cancellation, disconnect errors, and disposal errors do not change the returned response. Cleanup attempts a graceful disconnect for a connected client with `CancellationToken.None` and always attempts disposal. Cleanup exceptions are suppressed so they cannot hide the original connection, authentication, conversion, or send exception, including cancellation.

When upgrading, treat a successful return as SMTP acceptance even if the caller's token has since been canceled. Callers no longer receive disconnect or disposal exceptions from this method. Review retry policies for send failures or cancellation: the server may have accepted the message before the connection failed, leaving the outcome uncertain. The library does not retry automatically.

### Independent message builders

`EmailHelper` holds reusable template configuration. Each `ForType(...)` call returns a fresh `IEmailBuilder` with its own mappings; template selection and engine creation happen when `BuildMessage(...)` is called. Builders from the same helper can be interleaved or built in parallel:

```csharp
var aliceBuilder = emailHelper.ForType("TestMail").Map("MachineName", "Alice");
var bobBuilder = emailHelper.ForType("TestMail").Map("MachineName", "Bob");

var bobMessage = bobBuilder.BuildMessage(["bob@example.com"]);
var aliceMessage = aliceBuilder.BuildMessage(["alice@example.com"]);
```

Each builder is mutable. Use a separate builder for each message and do not modify or build the same builder concurrently. Configuration and mapped objects are shared by reference; do not mutate them during concurrent builds. `BuildMessage` copies the To and CC arrays, so later changes to the caller's arrays do not change a built message. `CommonMailMessage` itself remains mutable.

#### Migrating existing callers

This is a breaking API change and consumers must recompile. `IEmailHelper` now exposes only `ForType`, which returns `IEmailBuilder`; `Map`, `MapRange`, and `BuildMessage` belong to that builder. Existing chained `ForType(...).Map(...).BuildMessage(...)` calls keep the same source syntax. For separate statements, capture the builder instead of discarding the result of `ForType`:

```csharp
IEmailBuilder builder = emailHelper.ForType("TestMail");
builder.Map("MachineName", Environment.MachineName);
var message = builder.BuildMessage([toAddress]);
```

`ForType` no longer resets an existing builder. Start another message with `emailHelper.ForType(...)`. The helper's public `Engine` and `Pipeline` properties have been removed; use builder mappings rather than modifying rendering state directly. Update custom `IEmailHelper` implementations to return independent `IEmailBuilder` instances. To change a built message's recipients intentionally, modify that message rather than the original input arrays.

### Dependency injection

Register SMTP settings and the helper using `AddTemplateEmail` from the SMTP package:

```csharp
using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.DependencyInjection;

services.AddTemplateEmail(configSource, settings =>
{
    settings.FromAddress = fromAddress;
    settings.EmailDisplayName = displayName;
    settings.SmtpSettings = new(smtpServer, userName, password, port);
});
```

Inject `IEmailHelper` and call `ForType` for each message; do not register or reuse an `IEmailBuilder` as a shared service. Both registration overloads keep `IEmailHelper` transient and `EmailSettings` singleton. The file-path overload reads XML when each helper is resolved and that helper reuses its loaded configuration for subsequent builders. The `MailConfiguration` overload uses the supplied configuration instance. No configuration cache or automatic reload is introduced.

### Token values

Tokens use `{Entity.Property}` with case-sensitive names. Map an entity before calling `BuildMessage`:

```csharp
var message = emailHelper.ForType("TestMail")
    .Map("SentAt", DateTime.Now)
    .Map("User", new { Name = "Alice" })
    .BuildMessage([toAddress]);
```

- `{SentAt.Value}` uses the mapped value's `ToString()`, including dates, numbers, enums, and objects. Formatting uses the value's normal `ToString()` behavior and current culture where applicable.
- `Value` is reserved for the whole mapped value, even when the object has a property named `Value`. To render that property, map its value separately, for example `.Map("Amount", model.Value)` and use `{Amount.Value}`.
- Other property names, such as `{User.Name}`, read public instance getters without index parameters. Indexers, static properties, and properties without a public getter return an empty string. Exceptions thrown by a readable getter still propagate.
- Strings return the mapped string for any property name. Values without public properties retain the `ToString()` fallback for any property name.
- Missing entities or properties and null mapped or property values produce an empty string.
