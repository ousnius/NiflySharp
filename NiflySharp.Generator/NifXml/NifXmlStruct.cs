using System.Collections.Generic;
using System.Xml.Serialization;

namespace NiflySharp.Generator
{
    /// <summary>
    /// Struct ("struct")
    /// </summary>
    public class NifXmlStruct : INifXmlObject
    {
        /// <summary>
        /// Name of the struct
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Module of the struct (e.g. NiAnimation)
        /// </summary>
        [XmlAttribute("module")]
        public string Module { get; set; }

        /// <summary>
        /// Uses generic typing
        /// </summary>
        [XmlAttribute("generic")]
        public bool Generic { get; set; }

        /// <summary>
        /// Stop condition for IO
        /// </summary>
        [XmlAttribute("stopcond")]
        public string StopCondition { get; set; }

        [XmlText]
        public string Comment { get; set; }

        /// <summary>
        /// List of the fields of the struct
        /// </summary>
        [XmlElement("field")]
        public List<NifXmlField> Fields { get; set; }

        /// <summary>
        /// Structs don't have inheritance.
        /// </summary>
        public string Inherit { get => null; set { } }

        /// <summary>
        /// Structs are never abstract.
        /// </summary>
        public bool Abstract { get => false; set { } }

        /// <summary>
        /// Is this an object or struct (interface property). Set internally.
        /// </summary>
        public bool IsStruct { get => true; set { } }
    }
}
