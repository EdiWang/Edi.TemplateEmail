using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;

namespace Edi.TemplateEmail;

public class EmailHelper : IEmailHelper
{
    private readonly MailConfiguration _mailConfiguration;

    public EmailHelper(MailConfiguration configuration)
    {
        ValidateConfiguration(configuration);
        _mailConfiguration = configuration;
    }

    public EmailHelper(string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
        {
            throw new ArgumentNullException(nameof(configPath));
        }

        var serializer = new XmlSerializer(typeof(MailConfiguration));
        using var fileStream = File.OpenRead(configPath);
        _mailConfiguration = (MailConfiguration)serializer.Deserialize(fileStream);
        ValidateConfiguration(_mailConfiguration);
    }

    public IEmailBuilder ForType(string mailType)
    {
        return new EmailBuilder(_mailConfiguration, mailType);
    }

    /// <summary>
    /// Creates an independent builder using the specified culture for template selection and formatting.
    /// </summary>
    public IEmailBuilder ForType(string mailType, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return new EmailBuilder(_mailConfiguration, mailType, culture);
    }

    private static void ValidateConfiguration(MailConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (configuration.MailMessages is null)
        {
            throw new InvalidOperationException("MailConfiguration.MailMessages must not be null.");
        }

        var culturesByType = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        for (var index = 0; index < configuration.MailMessages.Count; index++)
        {
            var template = configuration.MailMessages[index];
            if (template is null)
            {
                throw new InvalidOperationException($"MailMessages[{index}] must not be null.");
            }

            var location = $"MailMessages[{index}] (MessageType='{template.MessageType}', MessageCulture='{template.MessageCulture ?? ""}')";
            if (string.IsNullOrWhiteSpace(template.MessageType))
            {
                throw new InvalidOperationException($"{location}: MessageType must not be null, empty, or whitespace.");
            }
            if (template.MessageSubject is null)
            {
                throw new InvalidOperationException($"{location}: MessageSubject is required; use an empty string for an empty subject.");
            }
            if (template.MessageBody is null)
            {
                throw new InvalidOperationException($"{location}: MessageBody is required; use an empty string for an empty body.");
            }

            if (!culturesByType.TryGetValue(template.MessageType, out var cultures))
            {
                cultures = new(StringComparer.InvariantCultureIgnoreCase);
                culturesByType.Add(template.MessageType, cultures);
            }
            if (!cultures.Add(template.MessageCulture ?? ""))
            {
                throw new InvalidOperationException($"{location}: duplicate MessageType/MessageCulture key.");
            }
        }
    }
}
