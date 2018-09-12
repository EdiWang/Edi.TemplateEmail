using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Edi.TemplateEmail.Configuration
{
    internal static class SerializerCache
    {
        private static readonly Dictionary<string, XmlSerializer> Cache = new Dictionary<string, XmlSerializer>();

        static SerializerCache()
        {
        }

        public static string GenerateKey(Type type, string rootElementName)
        {
            return type.Namespace + "." + type.Name + ":" + rootElementName;
        }

        public static XmlSerializer GenerateSerializer(Type type, string rootElementName)
        {
            return GenerateSerializer(type, rootElementName, GenerateKey(type, rootElementName));
        }

        public static XmlSerializer GenerateSerializer(Type type, string rootElementName, string key)
        {
            var root = new XmlRootAttribute(rootElementName);
            var serializer = new XmlSerializer(type, root);
            Add(key, serializer);
            return serializer;
        }

        public static void Add(string key, XmlSerializer serializer)
        {
            Cache.Add(key, serializer);
        }

        public static XmlSerializer Get(string key)
        {
            XmlSerializer xmlSerializer;
            return Cache.TryGetValue(key, out xmlSerializer) ? xmlSerializer : null;
        }

        public static void Clear()
        {
            Cache.Clear();
        }

        public static XmlSerializer Load(Type type, string rootElementName)
        {
            var key = GenerateKey(type, rootElementName);
            var xmlSerializer = Get(key);
            return xmlSerializer ?? GenerateSerializer(type, rootElementName, key);
        }
    }
}
