Edi.TemplateEmail
===============================

config template in config and send email in async

Step 1: Sample mailConfiguration.config under your website root
---------------------------------------------------------------

```xml
<?xml version="1.0"?>
<mailConfiguration>
  <CommonConfiguration OverrideToAddress="false" ToAddress="" />
  <MailMessage MessageType="TestMail" IsHtml="true">
    <MessageSubject>EdiBlog系统测试邮件</MessageSubject>
    <MessageBody>
      <![CDATA[
如果你收到了这封邮件，则证明EdiBlog在 {MachineName.Value} 上的邮件配置已经生效。参数摘要如下：<br />
Smtp服务器：{SmtpServer.Value}<br />
Smtp端口：{SmtpServerPort.Value}<br />
Smtp用户名：{SmtpUserName.Value}<br />
发件人显示名：{EmailDisplayName.Value}<br />
启用SSL加密：{EnableSsl.Value}<br /> <br />
这是一封由系统生成的邮件，请不要傻乎乎的回复。
      ]]>
    </MessageBody>
  </MailMessage>
  <MailMessage MessageType="NewCommentNotification" IsHtml="true">
    <MessageSubject>您有新的评论</MessageSubject>
    <MessageBody>
      <![CDATA[
{Username.Value}评论了文章"{Title.Value}": <br />
{CommentContent.Value} <br /> <br />
评论时间: {PubDate.Value} <br />
对方Email地址: {Email.Value} <br />
对方IP地址: {IPAddress.Value}
      ]]>
    </MessageBody>
</mailConfiguration>
```

Step 2: In your web.config
--------------------------

```xml
<configSections>
    ...
    <section name="mailConfiguration" type="Edi.XmlConfigMapper.XmlSection`1[[Edi.TemplateEmail.MailConfiguration, Edi.TemplateEmail, Culture=neutral]], Edi.XmlConfigMapper" />
</configSections>
<mailConfiguration configSource="mailConfiguration.config" />
```

Step 3: In your code
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