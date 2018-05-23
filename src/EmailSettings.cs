namespace Edi.TemplateEmail
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public string SmtpUserName { get; set; }
        public string SmtpPassword { get; set; }
        public int SmtpServerPort { get; set; }
        public bool EnableSsl { get; set; }
        public bool IncludeSignature { get; set; }
        public string SignatureContent { get; set; }
        public string EmailDisplayName { get; set; }
        public bool EmailWithSystemInfo { get; set; }
        public string SenderName { get; set; }
        public bool IncludeFooter { get; set; }
        public string FooterContent { get; set; }
    }
}
