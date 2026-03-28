using System;
using System.IO;
using System.Xml.Serialization;

namespace Edi.TemplateEmail;

public class EmailHelper : IEmailHelper
{
    public TemplateEngine Engine { get; private set; }

    public TemplatePipeline Pipeline { get; private set; }

    private readonly MailConfiguration _mailConfiguration;
    private string _mailType;

    public EmailHelper(MailConfiguration configuration)
    {
        _mailConfiguration = configuration;
    }

    public EmailHelper(string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
        {
            throw new ArgumentNullException(nameof(configPath));
        }

        var serializer = new XmlSerializer(typeof(MailConfiguration));
        using var fileStream = new FileStream(configPath, FileMode.Open);
        _mailConfiguration = (MailConfiguration)serializer.Deserialize(fileStream);
    }

    public IEmailHelper ForType(string mailType)
    {
        _mailType = mailType;
        Pipeline = new();
        Engine = null;
        return this;
    }

    public IEmailHelper Map(string name, object value)
    {
        Pipeline.Map(name, value);
        return this;
    }

    public IEmailHelper MapRange(params (string name, object value)[] values)
    {
        foreach (var (name, value) in values)
        {
            Pipeline.Map(name, value);
        }

        return this;
    }

    public CommonMailMessage BuildMessage(string[] receipts, string[] ccReceipts = null)
    {
        if (null == receipts || receipts.Length == 0)
        {
            throw new ArgumentNullException(nameof(receipts));
        }

        LoadEngine();

        var subjectText = Engine.Format(Engine.TextProvider.Subject).Trim();
        var bodyText = Engine.Format(Engine.TextProvider.Text).Trim();

        var cm = new CommonMailMessage
        {
            Subject = subjectText,
            Body = bodyText,
            Receipts = receipts,
            CcReceipts = ccReceipts,
            BodyIsHtml = Engine.TextProvider.IsHtml
        };

        return cm;
    }

    private void LoadEngine()
    {
        var messageToPersonalize = new TemplateMailMessage(_mailConfiguration, _mailType);
        if (!messageToPersonalize.Loaded)
        {
            throw new InvalidOperationException($"Mail type '{_mailType}' was not found in the mail configuration.");
        }

        Engine = new TemplateEngine(messageToPersonalize, Pipeline);
    }
}