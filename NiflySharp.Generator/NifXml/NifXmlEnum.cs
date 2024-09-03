using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace NiflySharp.Generator
{
    /// <summary>
    /// Enumeration with storage type
    /// </summary>
    public class NifXmlEnum
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("storage")]
        public string StorageType { get; set; }

        [XmlText]
        public string Comment { get; set; }

        [XmlElement("option")]
        public List<NifXmlEnumOption> Options { get; set; }
    }

    /// <summary>
    /// Enumeration option (name and value)
    /// </summary>
    public class NifXmlEnumOption
    {
        [XmlAttribute("value")]
        public string Value { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlText]
        public string Comment { get; set; }
    }
}
