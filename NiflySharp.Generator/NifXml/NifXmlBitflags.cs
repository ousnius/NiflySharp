using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace NiflySharp.Generator
{
    /// <summary>
    /// Bitflags as enum with storage type
    /// </summary>
    public class NifXmlBitflags
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("storage")]
        public string StorageType { get; set; }

        [XmlText]
        public string Comment { get; set; }

        [XmlElement("option")]
        public List<NifXmlBitflagsOption> Options { get; set; }
    }

    /// <summary>
    /// Bitflags enum options (name and bit)
    /// </summary>
    public class NifXmlBitflagsOption
    {
        [XmlAttribute("bit")]
        public int Bit { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlText]
        public string Comment { get; set; }
    }
}
