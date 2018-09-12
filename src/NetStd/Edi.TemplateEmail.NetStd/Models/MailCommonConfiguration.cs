namespace Edi.TemplateEmail.NetStd.Models
{
    /// <summary>
    /// Represents the common configuration node
    /// </summary>
    public class MailCommonConfiguration
    {
        public bool OverrideToAddress { get; set; }

        public string ToAddress { get; set; }
    }
}