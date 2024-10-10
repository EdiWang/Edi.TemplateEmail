using MimeKit;

namespace Edi.TemplateEmail;

public class MimeMessageWithSettings
{
    public MimeMessage MimeMessage { get; set; }

    public EmailSettings Settings { get; set; }
}