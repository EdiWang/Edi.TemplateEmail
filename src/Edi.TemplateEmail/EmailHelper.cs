using System;
using System.IO;
using System.Xml.Serialization;

namespace Edi.TemplateEmail;

public class EmailHelper : IEmailHelper
{
    private readonly MailConfiguration _mailConfiguration;

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
        using var fileStream = File.OpenRead(configPath);
        _mailConfiguration = (MailConfiguration)serializer.Deserialize(fileStream);
    }

    public IEmailBuilder ForType(string mailType)
    {
        return new EmailBuilder(_mailConfiguration, mailType);
    }
}
