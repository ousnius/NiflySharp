using System.Collections.Generic;
using System.Xml.Serialization;

namespace NiflySharp.Generator
{
    /// <summary>
    /// Object ("niobject")
    /// </summary>
    public class NifXmlObject : INifXmlObject
    {
        /// <summary>
        /// Name of the object
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Module of the object (e.g. NiAnimation)
        /// </summary>
        [XmlAttribute("module")]
        public string Module { get; set; }

        /// <summary>
        /// Inherited object name
        /// </summary>
        [XmlAttribute("inherit")]
        public string Inherit { get; set; }

        /// <summary>
        /// Stop condition for IO
        /// </summary>
        [XmlAttribute("stopcond")]
        public string StopCondition { get; set; }

        /// <summary>
        /// Abstract object (can not be instantiated with this type).
        /// </summary>
        [XmlAttribute("abstract")]
        public bool Abstract { get; set; }

        /// <summary>
        /// Uses generic typing
        /// </summary>
        [XmlAttribute("generic")]
        public bool Generic { get; set; }

        [XmlText]
        public string Comment { get; set; }

        /// <summary>
        /// List of the fields of the object
        /// </summary>
        [XmlElement("field")]
        public List<NifXmlField> Fields { get; set; }

        /// <summary>
        /// Is this an object or struct (interface property). Set internally.
        /// </summary>
        public bool IsStruct { get => false; set { } }
    }
}
