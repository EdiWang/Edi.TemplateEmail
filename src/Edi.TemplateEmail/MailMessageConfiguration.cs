using System.Xml.Serialization;

namespace Edi.TemplateEmail;

public class MailMessageConfiguration
{
    [XmlAttribute]
    public string MessageType { get; set; }

    [XmlAttribute]
    public bool IsHtml { get; set; }

    [XmlElement]
    public string MessageBody { get; set; }

    [XmlElement]
    public string MessageSubject { get; set; }

    [XmlElement]
    public string MessageCulture { get; set; }
}