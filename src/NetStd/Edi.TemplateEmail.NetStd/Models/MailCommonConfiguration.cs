using System.Xml.Serialization;

namespace Edi.TemplateEmail.NetStd.Models
{
    /// <summary>
    /// Represents the common configuration node
    /// </summary>
    public class MailCommonConfiguration
    {
        [XmlAttribute]
        public bool OverrideToAddress { get; set; }

        [XmlAttribute]
        public string ToAddress { get; set; }
    }
}