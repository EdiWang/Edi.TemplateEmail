# Edi.TemplateEmail

Build email messages from XML templates in .NET 10. `Edi.TemplateEmail` handles templates and token mapping; `Edi.TemplateEmail.Smtp` adds SMTP delivery through MailKit.

[![NuGet][main-nuget-badge]][main-nuget]

[main-nuget]: https://www.nuget.org/packages/Edi.TemplateEmail/
[main-nuget-badge]: https://img.shields.io/nuget/v/Edi.TemplateEmail.svg?style=flat-square&label=nuget

## Install

```sh
dotnet add package Edi.TemplateEmail
# Optional SMTP delivery
dotnet add package Edi.TemplateEmail.Smtp
```

## XML templates

Place `mailConfiguration.xml` in your application's output/deployment directory, or supply another path to `EmailHelper`.

```xml
<?xml version="1.0"?>
<MailConfiguration>
  <MailMessage MessageType="Welcome" IsHtml="true">
    <MessageSubject>Welcome, {User.Name}</MessageSubject>
    <MessageBody><![CDATA[
<p>Hello {User.Name}!</p>
<p>{Message.Value}</p>
{Footer.Value}
    ]]></MessageBody>
  </MailMessage>
</MailConfiguration>
```

`MessageType` and `IsHtml` are attributes; `MessageSubject`, `MessageBody`, and optional `MessageCulture` are elements. Use `IsHtml="false"` for plain text.

Template selection matches `MessageType` exactly, prefers a case-insensitive match between `MessageCulture` and `CultureInfo.CurrentCulture.Name`, and otherwise uses the first entry of that message type.

## Build a message

```csharp
using Edi.TemplateEmail;

var configPath = Path.Combine(AppContext.BaseDirectory, "mailConfiguration.xml");
var emailHelper = new EmailHelper(configPath);

var message = emailHelper.ForType("Welcome")
    .Map("User", new { Name = "Alice & Bob" })
    .MapRange(
        ("Message", "Your account is ready."),
        ("Footer", new TrustedHtml("<small>Example team</small>")))
    .BuildMessage(["alice@example.com"]);
```

`ForType` creates an independent `IEmailBuilder`; `Map` and `MapRange` return that builder. Template selection and rendering occur at `BuildMessage`. You can also construct `EmailHelper` from a `MailConfiguration` instance.

`BuildMessage(to, cc)` requires at least one To recipient, accepts optional CC recipients, copies both arrays, and trims the rendered subject and body. The resulting `CommonMailMessage` remains mutable.

Separate builders from the same helper can be interleaved or built in parallel. Use one builder per message; do not use the same builder concurrently or mutate shared configuration or mapped objects during builds.

## Token mapping

- Tokens use `{Entity.Property}` with case-sensitive names; both parts match `\w+`.
- `{Entity.Value}` uses the entire mapped value's `ToString()`. `Value` is reserved, including when the mapped object has a property with that name. Map that property's value separately to render it.
- Other property names resolve public, readable, non-indexed instance properties. Strings return the same string for any property name; values without public properties fall back to `ToString()`.
- Missing entities/properties, unsupported properties, and null values render as empty strings. Exceptions from readable getters propagate.
- Dates and numbers use their normal `ToString()` behavior and the current culture where applicable.

## HTML values

HTML bodies encode ordinary mapped values, including object properties, while preserving the XML template's markup. Subjects and plain-text bodies use the mapped text directly. Supply ordinary text without pre-encoding to avoid double encoding.

`TrustedHtml` explicitly inserts a complete mapped fragment without encoding, as shown by `Footer` above. Use only trusted or sanitized markup: this wrapper does not sanitize its input. Map fragments separately as `{Entity.Value}`; a wrapper inside an ordinary object's property remains encoded. `MapRange` also accepts `TrustedHtml`.

Keep ordinary tokens in HTML text or quoted attributes. The renderer does not parse HTML contexts; do not insert untrusted values into tag names, unquoted attributes, event handlers, styles, or executable content. Validate URL schemes and destinations before mapping URL attributes, applying your application's host, port, and path rules. HTML encoding does not validate URLs; encode URL components separately when constructing them.

Direct `TemplateEngine.Format(text)` calls perform unencoded token replacement. Use `Format(text, encodeHtml: true)` to encode ordinary values when rendering HTML directly.

## SMTP delivery

Install `Edi.TemplateEmail.Smtp` and configure the sender and SMTP connection:

```csharp
using Edi.TemplateEmail.Smtp;
using MailKit.Security;

var emailSettings = new EmailSettings
{
    FromAddress = "notifications@example.com",
    EmailDisplayName = "Example team",
    SmtpSettings = new(
        "smtp.example.com",
        Environment.GetEnvironmentVariable("SMTP_USER"),
        Environment.GetEnvironmentVariable("SMTP_PASSWORD"),
        587)
    {
        SocketOptions = SecureSocketOptions.StartTls
    }
};

var response = await message.SendAsync(emailSettings);
```

`FromAddress` is required and independent of the SMTP login. A null or empty SMTP user name skips authentication. Optional `SenderAddress` and `SenderName` set the Sender header when sending on behalf of another address; `EmailDisplayName` names the From address.

Choose `SmtpSettings.SocketOptions` for your server:

| Mode | Behavior |
| --- | --- |
| `StartTls` (default) | Requires STARTTLS; commonly used on port 587. |
| `SslOnConnect` | Uses TLS immediately; commonly used on port 465. |
| `None` | Unencrypted connection. |
| `StartTlsWhenAvailable` | Uses STARTTLS when supported, otherwise permits unencrypted delivery. |
| `Auto` | Uses `SslOnConnect` on port 465 and `StartTlsWhenAvailable` on other ports. |

Choose `StartTls` or `SslOnConnect` when encryption is required. `SkipCertificateValidation` defaults to `false`; enable it only in development.

`SendAsync` accepts an optional cancellation token for connection, authentication, and sending. It returns the SMTP acceptance response; downstream delivery can still fail. Cleanup attempts graceful disconnection without the caller's token and always attempts disposal. Cleanup failures and cancellation after successful sending do not replace the response or an original operation exception.

The library does not retry automatically. A send failure or cancellation can leave acceptance uncertain, so callers must account for possible duplicates when retrying. See the [MailKit documentation](https://mimekit.net/docs/html/T_MailKit_Net_Smtp_SmtpClient.htm).

## Dependency injection

Register on your application's `IServiceCollection`:

```csharp
using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.DependencyInjection;

services.AddTemplateEmail(configPath, settings =>
{
    settings.FromAddress = "notifications@example.com";
    settings.SmtpSettings = new("smtp.example.com", null, null, 587);
});
```

Inject `IEmailHelper` and `EmailSettings`, and call `ForType` for each message. The helper is transient, settings are singleton, and builders are created per call rather than registered as shared services. The example uses anonymous SMTP; set credentials as needed.

The file-path overload reads XML each time a helper is resolved; each helper reuses that configuration for its builders. The overload accepting `MailConfiguration` shares the supplied instance. There is no configuration cache or automatic reload.
