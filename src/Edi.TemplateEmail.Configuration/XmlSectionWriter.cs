using System.IO;
using System.Xml;

namespace Edi.TemplateEmail.Configuration
{
    public class XmlSectionWriter : XmlTextWriter
    {
        private bool _skipAttribute;
        private readonly StringWriter _stringWriter;

        public XmlSectionWriter(StringWriter w)
            : base(w)
        {
            _stringWriter = w;
        }

        public override string ToString()
        {
            return _stringWriter.ToString();
        }

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
        }

        public override void WriteStartDocument()
        {
        }

        public override void WriteStartDocument(bool standalone)
        {
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            if (prefix == "xmlns" && (localName == "xsi" || localName == "xsd"))
            {
                _skipAttribute = true;
            }
            else
            {
                base.WriteStartAttribute(prefix, localName, ns);
            }
        }

        public override void WriteString(string text)
        {
            if (!_skipAttribute)
            {
                base.WriteString(text);
            }
        }

        public override void WriteEndAttribute()
        {
            if (_skipAttribute)
            {
                _skipAttribute = false;
            }
            else
            {
                base.WriteEndAttribute();
            }
        }
    }
}
