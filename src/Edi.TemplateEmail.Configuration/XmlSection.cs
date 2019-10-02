using System.Configuration;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Edi.TemplateEmail.Configuration
{
    public class XmlSection<T> : ConfigurationSection where T : class
    {
        private XmlSerializer _serializer;
        private T _configurationItem;

        public static T GetSection(string sectionName)
        {
            var xmlSection = (XmlSection<T>)ConfigurationManager.GetSection(sectionName);
            return xmlSection?._configurationItem;
        }

        public static T GetSection(string sectionName, System.Configuration.Configuration configuration)
        {
            var xmlSection = (XmlSection<T>)configuration.GetSection(sectionName);
            return xmlSection?._configurationItem;
        }

        protected override void Init()
        {
            base.Init();
            _serializer = SerializerCache.Load(typeof(T), SectionInformation.Name);
        }

        protected override void DeserializeSection(XmlReader reader)
        {
            _configurationItem = (T)_serializer.Deserialize(reader);
        }

        protected override string SerializeSection(ConfigurationElement parentElement, string name, ConfigurationSaveMode saveMode)
        {
            var xmlSectionWriter = new XmlSectionWriter(new StringWriter());
            _serializer.Serialize(xmlSectionWriter, _configurationItem);
            return xmlSectionWriter.ToString();
        }

        protected override bool IsModified()
        {
            return true;
        }
    }
}
