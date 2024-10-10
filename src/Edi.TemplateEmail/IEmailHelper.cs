using System.Collections.Generic;

namespace Edi.TemplateEmail;

public interface IEmailHelper
{
    EmailSettings Settings { get; }
    TemplateEngine CurrentEngine { get; }

    public EmailHelper ForType(string mailType);
    public EmailHelper Map(string name, object value);
    public MimeMessageWithSettings BuildMessage(IEnumerable<string> toAddress, string ccAddress = null);
}