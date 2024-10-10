namespace Edi.TemplateEmail;

public class EmailSettings
{
    public string EmailDisplayName { get; set; }

    public string SenderName { get; set; }

    public SmtpSettings SmtpSettings { get; set; }
}

public class SmtpSettings(string smtpServer, string smtpUserName, string smtpPassword, int smtpServerPort)
{
    public string SmtpServer { get; } = smtpServer;

    public string SmtpUserName { get; } = smtpUserName;

    public string SmtpPassword { get; } = smtpPassword;

    public int SmtpServerPort { get; } = smtpServerPort;

    public bool EnableTls { get; set; } = true;
}