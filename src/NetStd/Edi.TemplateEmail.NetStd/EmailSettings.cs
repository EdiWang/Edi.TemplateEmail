namespace Edi.TemplateEmail.NetStd
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public string SmtpUserName { get; set; }
        public string SmtpPassword { get; set; }
        public int SmtpServerPort { get; set; }
        public bool EnableSsl { get; set; }
        public string EmailDisplayName { get; set; }
        public string SenderName { get; set; }
        public bool UseDefaultCredentials { get; set; }
    }
}
