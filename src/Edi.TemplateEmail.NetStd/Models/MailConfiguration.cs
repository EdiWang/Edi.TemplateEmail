using System.Collections.Generic;
using System.Xml.Serialization;

namespace Edi.TemplateEmail.Models
{
    /// <summary>
    /// Represents the root mail configuration node
    /// </summary>
    public class MailConfiguration
    {
        [XmlElement("MailMessage")]
        public List<MailMessageConfiguration> MailMessages { get; set; }

        [XmlElement]
        public MailCommonConfiguration CommonConfiguration { get; set; }
    }
}
