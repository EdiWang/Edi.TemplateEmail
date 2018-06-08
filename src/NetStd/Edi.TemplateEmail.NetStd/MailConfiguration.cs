using System.Collections.Generic;
using System.Xml.Serialization;

namespace Edi.TemplateEmail.NetStd
{
    /// <summary>
    /// Represents the root mail configuration node
    /// </summary>
    public class MailConfiguration
    {
        /// <summary>
        /// Gets or sets the mail messages.
        /// </summary>
        /// <value>The mail messages.</value>
        [XmlElement("MailMessage")]
        public List<MailMessageConfiguration> MailMessages { get; set; }

        /// <summary>
        /// Gets or sets the common configuration.
        /// </summary>
        /// <value>The common configuration.</value>
        [XmlElement("CommonConfiguration")]
        public MailCommonConfiguration CommonConfiguration { get; set; }
    }

    /// <summary>
    /// Represents the common configuration node
    /// </summary>
    public class MailCommonConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether [override to address].
        /// </summary>
        /// <value><c>true</c> if [override to address]; otherwise, <c>false</c>.</value>
        [XmlAttribute("OverrideToAddress")]
        public bool OverrideToAddress { get; set; }


        /// <summary>
        /// Gets or sets to address.
        /// </summary>
        /// <value>To address.</value>
        [XmlAttribute("ToAddress")]
        public string ToAddress { get; set; }
    }

    /// <summary>
    /// Represents the mail message configuration node
    /// </summary>
    public class MailMessageConfiguration
    {
        /// <summary>
        /// Gets or sets the type of the message.
        /// </summary>
        /// <value>The type of the message.</value>
        [XmlAttribute("MessageType")]
        public string MessageType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is HTML.
        /// </summary>
        /// <value><c>true</c> if this instance is HTML; otherwise, <c>false</c>.</value>
        [XmlAttribute("IsHtml")]
        public bool IsHtml { get; set; }

        /// <summary>
        /// Gets or sets the message body.
        /// </summary>
        /// <value>The message body.</value>
        [XmlElement("MessageBody")]
        public string MessageBody { get; set; }


        /// <summary>
        /// Gets or sets the message subject.
        /// </summary>
        /// <value>The message subject.</value>
        [XmlElement("MessageSubject")]
        public string MessageSubject { get; set; }


        /// <summary>
        /// Gets or sets the message culture.
        /// </summary>
        /// <value>The message culture.</value>
        [XmlElement]
        public string MessageCulture { get; set; }
    }
}
