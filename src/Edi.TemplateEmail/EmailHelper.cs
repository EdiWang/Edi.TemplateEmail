using Edi.TemplateEmail.Models;
using MimeKit;
using MimeKit.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Edi.TemplateEmail;

public class EmailHelper : IEmailHelper
{
    public EmailSettings Settings { get; }

    public TemplateEngine CurrentEngine { get; private set; }

    public TemplatePipeline Pipeline { get; private set; }

    private readonly MailConfiguration _mailConfiguration;
    private string _mailType;

    public EmailHelper(MailConfiguration configuration, EmailSettings settings)
    {
        Settings = settings;
        _mailConfiguration = configuration;
    }

    public EmailHelper(string configPath, EmailSettings settings)
    {
        Settings = settings;

        if (string.IsNullOrWhiteSpace(configPath))
        {
            throw new ArgumentNullException(nameof(configPath));
        }

        var serializer = new XmlSerializer(typeof(MailConfiguration));
        using var fileStream = new FileStream(configPath, FileMode.Open);
        _mailConfiguration = (MailConfiguration)serializer.Deserialize(fileStream);
    }

    public EmailHelper ForType(string mailType)
    {
        _mailType = mailType;
        Pipeline = new();
        return this;
    }

    public EmailHelper Map(string name, object value)
    {
        Pipeline.Map(name, value);
        return this;
    }

    public MimeMessageWithSettings BuildMessage(IEnumerable<string> toAddress, string ccAddress = null)
    {
        var messageToPersonalize = new TemplateMailMessage(_mailConfiguration, _mailType);
        if (messageToPersonalize.Loaded)
        {
            var engine = new TemplateEngine(messageToPersonalize, Pipeline);
            CurrentEngine = engine;
        }

        var enumerable = toAddress as string[] ?? toAddress.ToArray();
        if (!enumerable.Any())
        {
            throw new ArgumentNullException(nameof(toAddress));
        }

        // create mail message
        var messageToSend = new MimeMessage
        {
            Sender = new(Settings.SenderName, Settings.SmtpUserName),
            Subject = CurrentEngine.Format(() => new(CurrentEngine.TextProvider.Subject)).Trim(),
        };
        messageToSend.From.Add(new MailboxAddress(Settings.EmailDisplayName, Settings.SmtpUserName));
        var bodyText = CurrentEngine.Format(() => new(CurrentEngine.TextProvider.Text)).Trim();
        messageToSend.Body = CurrentEngine.TextProvider is { IsHtml: true }
            ? new(TextFormat.Html) { Text = bodyText }
            : new TextPart(TextFormat.Plain) { Text = bodyText };

        if (_mailConfiguration.CommonConfiguration.OverrideToAddress)
        {
            messageToSend.To.Add(MailboxAddress.Parse(_mailConfiguration.CommonConfiguration.ToAddress));
        }
        else
        {
            foreach (string add in enumerable)
            {
                messageToSend.To.Add(MailboxAddress.Parse(add));
            }
        }

        if (!string.IsNullOrEmpty(ccAddress))
        {
            messageToSend.Cc.Add(MailboxAddress.Parse(ccAddress));
        }

        return new MimeMessageWithSettings
        {
            MimeMessage = messageToSend,
            Settings = Settings
        };
    }
}