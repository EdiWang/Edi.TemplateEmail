Edi.TemplateEmail
===============================

This library enable you to configure email in XML template and send the email in your .NET applications.

```
PM > Install-Package Edi.TemplatedEmail
```

Tested fine with outlook.com email server.

Step 1: Sample mailConfiguration.config under your website root
---------------------------------------------------------------

```xml
<?xml version="1.0"?>
<mailConfiguration>
  <CommonConfiguration OverrideToAddress="false" ToAddress="" />
  <MailMessage MessageType="TestMail" IsHtml="true">
    <MessageSubject>Test Mail on {MachineName.Value}</MessageSubject>
    <MessageBody>
      <![CDATA[
Email Sending has been configured on {MachineName.Value} <br />
Smtp Server: {SmtpServer.Value}<br />
Smtp Port: {SmtpServerPort.Value}<br />
Smtp Username: {SmtpUserName.Value}<br />
Display Name: {EmailDisplayName.Value}<br />
Require SSL: {EnableSsl.Value}<br />
      ]]>
    </MessageBody>
  </MailMessage>
  ...
</mailConfiguration>
```

Step 2: Add Following Section in Web.config
--------------------------

```xml
<configSections>
    ...
    <section name="mailConfiguration" type="Edi.XmlConfigMapper.XmlSection`1[[Edi.TemplateEmail.MailConfiguration, Edi.TemplateEmail, Culture=neutral]], Edi.XmlConfigMapper" />
</configSections>
<mailConfiguration configSource="mailConfiguration.config" />
```

Step 3: C# Sample Code
--------------------

```
public static EmailHelper EmailHelper { get; private set; }

static EmailNotification()
{
    if (EmailHelper == null)
    {
        EmailHelper = new EmailHelper(new EmailSettings
        {
            SmtpServer = Settings.Settings.Instance.SmtpServer,
            SmtpUserName = Settings.Settings.Instance.SmtpUserName,
            SmtpPassword = Utils.DecryptEmailPassword(Settings.Settings.Instance.SmtpPassword),
            SmtpServerPort = Settings.Settings.Instance.SmtpServerPort,
            EnableSsl = Settings.Settings.Instance.EnableSslForEmail,
            EmailDisplayName = Settings.Settings.Instance.EmailDisplayName
        })
        .SendAs(PubConstant.SENDERNAME)
        .LogExceptionWith(Logger.Error);

        if (Settings.Settings.Instance.IncludeSignature)
        {
            EmailHelper.UseSignature(Settings.Settings.Instance.SignatureContent);
        }

        if (Settings.Settings.Instance.EmailWithSystemInfo)
        {
            EmailHelper.Settings.EmailWithSystemInfo = true;
            EmailHelper.WithFooter(string.Format("<p>Powered by EdiBlog {0}</p>", Settings.Settings.Instance.Version()));
        }

        EmailHelper.EmailCompleted += (sender, message, args) => WriteEmailLog(sender as MailMessage, message);
    }
}

...

public static void SendTestMail()
{
    var pipeline = new TemplatePipeline().Map("MachineName", HttpContext.Current.Server.MachineName)
                                         .Map("SmtpServer", Settings.Settings.Instance.SmtpServer)
                                         .Map("SmtpServerPort", Settings.Settings.Instance.SmtpServerPort)
                                         .Map("SmtpUserName", Settings.Settings.Instance.SmtpUserName)
                                         .Map("EmailDisplayName", Settings.Settings.Instance.SmtpUserName)
                                         .Map("EnableSsl", Settings.Settings.Instance.EnableSslForEmail);

    Task.Run(() => EmailHelper.ApplyTemplate(MailMesageType.TestMail, pipeline)
        .SendMailAsync(Settings.Settings.Instance.AdminEmail));
}

public static void SendNewCommentNotificationAsync(Comment comment, Post post)
{
    var pipeline = new TemplatePipeline().Map("Username", comment.Username)
                                         .Map("Email", comment.Email)
                                         .Map("IPAddress", comment.IPAddress)
                                         .Map("PubDate", comment.PubDate)
                                         .Map("Title", post.Title)
                                         .Map("CommentContent", comment.CommentContent);
    Task.Run(() => EmailHelper.ApplyTemplate(MailMesageType.NewCommentNotification, pipeline)
        .SendMailAsync(Settings.Settings.Instance.AdminEmail));
}
```

**and you can also**

```
// AfterComplete() will update complete status of the mail for Messages table in db
await EmailHelper.ApplyTemplate(MailMesageType.ContactMessageForAdmin, pipelineForAdmin)
                 .AfterComplete(async () => await new MessageOperator().UpdateCompleteStatusForMessage(mailId))
                 .SendMailAsync(Settings.Settings.Instance.AdminEmail);

// clear after complete in case of other mail methods using this action
EmailHelper.AfterCompleteAction = null;
```
