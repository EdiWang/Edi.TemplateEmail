Edi.TemplateEmail
===============================

This library enable you to configure email in XML template and send the email in your .NET applications.

> Currently, only SMTP Basic Authentication is supported.

![.NET](https://github.com/EdiWang/Edi.TemplateEmail/workflows/.NET%20Core/badge.svg)

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
var smtpServer = "smtp.example.com";
var userName = "test1@example.com";
var password = "********";
var port = 25;
var toAddress = "test2@test.com";

var configSource = $"{Directory.GetCurrentDirectory()}\\mailConfiguration.xml";
var emailHelper = new EmailHelper(configSource, new(smtpServer, userName, password, port)
{
    SenderName = senderName,
    EmailDisplayName = displayName
});
```

### Step 3: Map the values and send Email

```
var message = emailHelper.ForType("TestMail")
    .Map("MachineName", Environment.MachineName)
    .Map("SmtpServer", emailHelper.Settings.SmtpServer)
    .Map("SmtpServerPort", emailHelper.Settings.SmtpServerPort)
    .Map("SmtpUserName", emailHelper.Settings.SmtpUserName)
    .Map("EmailDisplayName", emailHelper.Settings.EmailDisplayName)
    .Map("EnableTls", emailHelper.Settings.EnableTls)
    .BuildMessage([toAddress]);

var result = await message.SendAsync();
```

## 免责申明

此项目(Edi.TemplateEmail)及其配套组件均为免费开源的产品，仅用于学习交流，并且不直接向中国提供服务，中国访客请于下载后立即删除。

任何中国境内的组织及个人不得使用此项目(Edi.TemplateEmail)及其配套组件构建任何形式的面向中国境内访客的网站或服务。

不可用于任何违反中华人民共和国(含台湾省)或使用者所在地区法律法规的用途。

因为作者即本人仅完成代码的开发和开源活动(开源即任何人都可以下载使用)，从未参与访客的任何运营和盈利活动。

且不知晓访客后续将程序源代码用于何种用途，故访客使用过程中所带来的任何法律责任即由访客自己承担。

[《开源软件有漏洞，作者需要负责吗？是的！》](https://go.edi.wang/aka/os251)