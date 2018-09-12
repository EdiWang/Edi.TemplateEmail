namespace Edi.TemplateEmail.NetStd.Models
{
    /// <summary>
    /// Represents the mail message configuration node
    /// </summary>
    public class MailMessageConfiguration
    {
        public string MessageType { get; set; }

        public bool IsHtml { get; set; }

        public string MessageBody { get; set; }

        public string MessageSubject { get; set; }

        public string MessageCulture { get; set; }
    }
}