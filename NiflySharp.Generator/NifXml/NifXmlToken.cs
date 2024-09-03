using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace NiflySharp.Generator
{
    /// <summary>
    /// Token for replacing operators, version numbers/checks and more.
    /// </summary>
    public class NifXmlToken
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Attributes that specify where the token is used in.
        /// </summary>
        [XmlAttribute("attrs")]
        public string Attributes { get; set; }

        [XmlAnyElement]
        public XmlElement[] Elements { get; set; }

        [XmlIgnore]
        public List<NifXmlTokenEntry> Entries { get; set; }

        [XmlText]
        public string Comment { get; set; }

        public string[] GetAttributesAsArray()
        {
            return Attributes.Split(' ');
        }
    }

    public class NifXmlTokenEntry
    {
        /// <summary>
        /// Token (source string).
        /// </summary>
        [XmlAttribute("token")]
        public string Token { get; set; }

        /// <summary>
        /// Replacement string for the token
        /// </summary>
        [XmlAttribute("string")]
        public string String { get; set; }

        [XmlText]
        public string Comment { get; set; }
    }
}
