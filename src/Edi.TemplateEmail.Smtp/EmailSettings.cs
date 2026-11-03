using MailKit.Security;

namespace Edi.TemplateEmail.Smtp;

public class EmailSettings
{
    /// <summary>
    /// Required From address, independent of the SMTP authentication user name.
    /// </summary>
    public string FromAddress { get; set; }

    public string EmailDisplayName { get; set; }

    /// <summary>
    /// Optional Sender address when sending on behalf of the From address.
    /// </summary>
    public string SenderAddress { get; set; }

    public string SenderName { get; set; }

    public SmtpSettings SmtpSettings { get; set; }
}

public class SmtpSettings(string smtpServer, string smtpUserName, string smtpPassword, int smtpServerPort)
{
    public string SmtpServer { get; } = smtpServer;

    public string SmtpUserName { get; } = smtpUserName;

    public string SmtpPassword { get; } = smtpPassword;

    public int SmtpServerPort { get; } = smtpServerPort;

    /// <summary>
    /// Connection security mode. Defaults to requiring STARTTLS; use SslOnConnect for implicit TLS.
    /// </summary>
    public SecureSocketOptions SocketOptions { get; set; } = SecureSocketOptions.StartTls;

    /// <summary>
    /// Skips TLS certificate validation. Should only be used in development environments.
    /// </summary>
    public bool SkipCertificateValidation { get; set; } = false;
}
