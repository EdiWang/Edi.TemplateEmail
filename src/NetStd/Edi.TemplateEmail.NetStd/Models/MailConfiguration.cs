using System.Collections.Generic;

namespace Edi.TemplateEmail.NetStd.Models
{
    /// <summary>
    /// Represents the root mail configuration node
    /// </summary>
    public class MailConfiguration
    {
        public List<MailMessageConfiguration> MailMessages { get; set; }

        public MailCommonConfiguration CommonConfiguration { get; set; }
    }
}
