using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace NiflySharp.Generator
{
    /// <summary>
    /// Bitfield with storage type and members
    /// </summary>
    public class NifXmlBitfield
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("storage")]
        public string StorageType { get; set; }

        [XmlText]
        public string Comment { get; set; }

        [XmlElement("member")]
        public List<NifXmlBitfieldMember> Members { get; set; }
    }

    /// <summary>
    /// Bitfield member (bit or section of a bitfield)
    /// </summary>
    public class NifXmlBitfieldMember
    {
        [XmlAttribute("width")]
        public int Width { get; set; }

        [XmlAttribute("pos")]
        public int Position { get; set; }

        [XmlAttribute("mask")]
        public string Mask { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlAttribute("default")]
        public string Default { get; set; }

        [XmlText]
        public string Comment { get; set; }
    }
}
