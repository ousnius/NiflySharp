using System.Xml.Serialization;

namespace NiflySharp.Generator
{
    /// <summary>
    /// Field of an object or struct
    /// </summary>
    public class NifXmlField
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Type of the field.
        /// Can be any object, struct, enum, bitfield, bitflags etc.
        /// </summary>
        [XmlAttribute("type")]
        public string Type { get; set; }

        /// <summary>
        /// Template type used for generic fields.
        /// </summary>
        [XmlAttribute("template")]
        public string Template { get; set; }

        /// <summary>
        /// Length for arrays
        /// </summary>
        [XmlAttribute("length")]
        public string Length { get; set; }

        /// <summary>
        /// Additional width for arrays
        /// </summary>
        [XmlAttribute("width")]
        public string Width { get; set; }

        /// <summary>
        /// Field can only appear until this NIF version.
        /// </summary>
        [XmlAttribute("until")]
        public string VersionUntil { get; set; }

        /// <summary>
        /// Field can only appear since this NIF version.
        /// </summary>
        [XmlAttribute("since")]
        public string VersionSince { get; set; }

        /// <summary>
        /// Version condition for which the field can appear.
        /// </summary>
        [XmlAttribute("vercond")]
        public string VersionCondition { get; set; }

        /// <summary>
        /// Condition for when to read/write the field.
        /// </summary>
        [XmlAttribute("cond")]
        public string Condition { get; set; }

        /// <summary>
        /// Field only exists when the object is the specified type.
        /// </summary>
        [XmlAttribute("onlyT")]
        public string OnlyType { get; set; }

        /// <summary>
        /// Field only exists when the object is NOT the specified type.
        /// </summary>
        [XmlAttribute("excludeT")]
        public string ExcludeType { get; set; }

        /// <summary>
        /// Default value of the field
        /// </summary>
        [XmlAttribute("default")]
        public string Default { get; set; }

        /// <summary>
        /// Argument value that gets passed down in the IO.
        /// </summary>
        [XmlAttribute("arg")]
        public string Arg { get; set; }

        /// <summary>
        /// Calculation formula for setting value before writing it
        /// </summary>
        [XmlAttribute("calc")]
        public string Calculate { get; set; }

        [XmlText]
        public string Comment { get; set; }
    }
}
