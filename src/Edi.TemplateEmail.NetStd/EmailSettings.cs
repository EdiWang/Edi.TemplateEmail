namespace Edi.TemplateEmail
{
    public class EmailSettings
    {
        public string SmtpServer { get; }

        public string SmtpUserName { get; }

        public string SmtpPassword { get; }

        public int SmtpServerPort { get; }

        public bool EnableSsl { get; set; }

        public string EmailDisplayName { get; set; }

        public string SenderName { get; set; }

        public EmailSettings(string smtpServer, string smtpUserName, string smtpPassword, int smtpServerPort)
        {
            SmtpServer = smtpServer;
            SmtpUserName = smtpUserName;
            SmtpPassword = smtpPassword;
            SmtpServerPort = smtpServerPort;
        }
    }
}
