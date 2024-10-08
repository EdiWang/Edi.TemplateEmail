﻿using Edi.TemplateEmail.Models;
using MimeKit;
using MimeKit.Text;
using System;
using System.IO;
using System.Xml.Serialization;

namespace Edi.TemplateEmail;

public class EmailHelper : IEmailHelper
{
    public EmailSettings Settings { get; }

    public TemplateEngine Engine { get; private set; }

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

    public MimeMessageWithSettings BuildMessage(string[] receipts, string[] ccReceipts = null)
    {
        if (null == receipts || receipts.Length == 0)
        {
            throw new ArgumentNullException(nameof(receipts));
        }

        LoadEngine();

        var subjectText = Engine.Format(() => new(Engine.TextProvider.Subject)).Trim();
        var bodyText = Engine.Format(() => new(Engine.TextProvider.Text)).Trim();

        var cm = new CommonMailMessage
        {
            Subject = subjectText,
            Body = bodyText,
            Receipts = receipts,
            CcReceipts = ccReceipts
        };

        var messageToSend = new MimeMessage
        {
            Sender = new(Settings.SenderName, Settings.SmtpSettings.SmtpUserName),
        };

        messageToSend.From.Add(new MailboxAddress(Settings.EmailDisplayName, Settings.SmtpSettings.SmtpUserName));
        messageToSend.Subject = cm.Subject;
        messageToSend.Body = Engine.TextProvider is { IsHtml: true }
            ? new(TextFormat.Html) { Text = cm.Body }
            : new TextPart(TextFormat.Plain) { Text = cm.Body };

        foreach (var address in cm.Receipts)
        {
            messageToSend.To.Add(MailboxAddress.Parse(address));
        }

        if (cm.CcReceipts is { Length: > 0 })
        {
            foreach (var ccReceipt in cm.CcReceipts)
            {
                messageToSend.Cc.Add(MailboxAddress.Parse(ccReceipt));
            }
        }

        return new MimeMessageWithSettings
        {
            MimeMessage = messageToSend,
            Settings = Settings
        };
    }

    private void LoadEngine()
    {
        var messageToPersonalize = new TemplateMailMessage(_mailConfiguration, _mailType);
        if (messageToPersonalize.Loaded)
        {
            var engine = new TemplateEngine(messageToPersonalize, Pipeline);
            Engine = engine;
        }
    }
}