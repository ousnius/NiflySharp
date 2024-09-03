using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace NiflySharp.Generator
{
    /// <summary>
    /// Reads NIF XML files (nif.xml) and stores its data.
    /// </summary>
    [XmlRoot("niftoolsxml")]
    public class NifXml
    {
        /// <summary>
        /// Mapping from types used in the XML to matching or alternative C# types.
        /// </summary>
        private static readonly Dictionary<string, string> TypeMapping = new Dictionary<string, string>()
        {
            { "bool", "bool?" },
            { "uint64", "ulong" },
            { "int64", "long" },
            { "ulittle32", "uint" },
            { "normbyte", "sbyte" },
            { "StringOffset", "uint" },
            { "hfloat", "System.Half" },
            { "string", "NiStringRef" },
            { "PlatformID", "NiflySharp.Enums.PlatformID" }
        };

        /// <summary>
        /// Array of C# base types (plus System.Half).
        /// </summary>
        private static readonly string[] BaseTypes = new[]
        {
            "byte",
            "ushort",
            "uint",
            "ulong",
            "sbyte",
            "short",
            "int",
            "long",
            "char",
            "bool",
            "bool?",
            "float",
            "double",
            "decimal",
            "System.Half",
            "Vector2",
            "Vector3",
            "Vector4",
            "Quaternion"
        };

        /// <summary>
        /// Data of "token" elements
        /// </summary>
        [XmlElement("token")]
        public List<NifXmlToken> Tokens { get; set; }

        /// <summary>
        /// Data of "enum" elements
        /// </summary>
        [XmlElement("enum")]
        public List<NifXmlEnum> Enums { get; set; }

        /// <summary>
        /// Data of "bitflags" elements
        /// </summary>
        [XmlElement("bitflags")]
        public List<NifXmlBitflags> Bitflags { get; set; }

        /// <summary>
        /// Data of "bitfield" elements
        /// </summary>
        [XmlElement("bitfield")]
        public List<NifXmlBitfield> Bitfields { get; set; }

        /// <summary>
        /// Data of "niobject" elements
        /// </summary>
        [XmlElement("niobject")]
        public List<NifXmlObject> Objects { get; set; }

        /// <summary>
        /// Data of "struct" elements
        /// </summary>
        [XmlElement("struct")]
        public List<NifXmlStruct> Structs { get; set; }

        /// <summary>
        /// Creates empty container for XML data. 
        /// Use <see cref="DeserializeXmlResource(string)"/> to load data.
        /// </summary>
        public NifXml()
        {
        }

        /// <summary>
        /// Load XML document from resource and read data.
        /// </summary>
        /// <param name="resourceName">Resource name</param>
        /// <returns>Loading successful</returns>
        public static NifXml DeserializeXml(string xmlString)
        {
            try
            {
                using (var reader = new StringReader(xmlString))
                {
                    var serializer = new XmlSerializer(typeof(NifXml));
                    var nifXml = serializer.Deserialize(reader) as NifXml;

                    if (nifXml != null)
                    {
                        // Deserialize token entries manually as their element name varies
                        foreach (var token in nifXml.Tokens)
                        {
                            if (token.Entries == null)
                                token.Entries = new List<NifXmlTokenEntry>();

                            foreach (var tokenElement in token.Elements)
                            {
                                if (tokenElement.Name == token.Name)
                                {
                                    var entry = new NifXmlTokenEntry()
                                    {
                                        Token = tokenElement.Attributes["token"]?.Value,
                                        String = tokenElement.Attributes["string"]?.Value,
                                        Comment = GetCommentText(tokenElement)
                                    };
                                    token.Entries.Add(entry);
                                }
                            }

                            token.Elements = null;
                        }
                    }

                    return nifXml;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }

        /// <summary>
        /// Load XML document from resource and read data.
        /// </summary>
        /// <param name="resourceName">Resource name</param>
        /// <returns>Loading successful</returns>
        public static NifXml DeserializeXmlResource(string resourceName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    var serializer = new XmlSerializer(typeof(NifXml));
                    var nifXml = serializer.Deserialize(stream) as NifXml;

                    if (nifXml != null)
                    {
                        // Deserialize token entries manually as their element name varies
                        foreach (var token in nifXml.Tokens)
                        {
                            if (token.Entries == null)
                                token.Entries = new List<NifXmlTokenEntry>();

                            foreach (var tokenElement in token.Elements)
                            {
                                if (tokenElement.Name == token.Name)
                                {
                                    var entry = new NifXmlTokenEntry()
                                    {
                                        Token = tokenElement.Attributes["token"]?.Value,
                                        String = tokenElement.Attributes["string"]?.Value,
                                        Comment = GetCommentText(tokenElement)
                                    };
                                    token.Entries.Add(entry);
                                }
                            }

                            token.Elements = null;
                        }
                    }

                    return nifXml;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }

        /// <summary>
        /// Returns an enumeration of both objects and structs using the <see cref="INifXmlObject"/> interface.
        /// </summary>
        public IEnumerable<INifXmlObject> EnumerateObjectsAndStructs()
        {
            return Objects.Cast<INifXmlObject>().Concat(Structs);
        }

        /// <summary>
        /// Checks if the type of <paramref name="field"/> is part of the currently stored enum or bitflags types.
        /// </summary>
        /// <param name="field">Field to check type for</param>
        public bool IsEnumType(NifXmlField field)
        {
            return Enums.Any(e => e.Name == field.Type) ||
                Bitflags.Any(e => e.Name == field.Type);
        }

        /// <summary>
        /// Checks if the type of <paramref name="bitfieldMember"/> is part of the currently stored enum or bitflags types.
        /// </summary>
        /// <param name="bitfieldMember">Bitfield member to check type for</param>
        public bool IsEnumType(NifXmlBitfieldMember bitfieldMember)
        {
            return Enums.Any(e => e.Name == bitfieldMember.Type) ||
                Bitflags.Any(e => e.Name == bitfieldMember.Type);
        }

        /// <summary>
        /// Checks if the type of <paramref name="field"/> is part of the currently stored bitfield types.
        /// </summary>
        /// <param name="field">Field to check type for</param>
        public bool IsBitfieldType(NifXmlField field)
        {
            return Bitfields.Any(e => e.Name == field.Type);
        }

        /// <summary>
        /// Checks if the type of <paramref name="field"/> is part of the currently stored struct types.
        /// </summary>
        /// <param name="field">Field to check type for</param>
        public bool IsStructType(NifXmlField field)
        {
            return Structs.Any(s => s.Name == field.Type);
        }

        /// <summary>
        /// Checks if the type of <paramref name="bitfieldMember"/> is part of the currently stored struct types.
        /// </summary>
        /// <param name="bitfieldMember">Bitfield member to check type for</param>
        public bool IsStructType(NifXmlBitfieldMember bitfieldMember)
        {
            return Structs.Any(s => s.Name == bitfieldMember.Type);
        }

        /// <summary>
        /// Retrieve the enum storage type of enum field <paramref name="field"/>.
        /// </summary>
        /// <param name="field">Field of type enum, bitfield or bitflags</param>
        /// <returns>Storage type as string (e.g. ushort, uint, ...) or null if type not found</returns>
        public string GetEnumStorageType(NifXmlField field)
        {
            var en = Enums.FirstOrDefault(e => e.Name == field.Type);
            if (en != null)
                return en.StorageType;

            var bf = Bitfields.FirstOrDefault(e => e.Name == field.Type);
            if (bf != null)
                return bf.StorageType;

            var bflg = Bitflags.FirstOrDefault(e => e.Name == field.Type);
            if (bflg != null)
                return bflg.StorageType;

            return null;
        }

        /// <summary>
        /// Checks if <paramref name="fieldName"/> occurs in the inheritance chain of type <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">Object to recursively check inheritance chain of</param>
        /// <param name="fieldName">Field name to search for</param>
        /// <param name="fieldResult">Found field (output)</param>
        public bool IsFieldInTypeInheritance(INifXmlObject obj, string fieldName, out NifXmlField fieldResult)
        {
            if (!string.IsNullOrWhiteSpace(obj.Inherit))
            {
                var parentObject = Objects.FirstOrDefault(o => o.Name == obj.Inherit);
                if (parentObject != null)
                {
                    var field = parentObject.Fields.FirstOrDefault(f => f.Name == fieldName);
                    if (field != null)
                    {
                        fieldResult = field;
                        return true;
                    }

                    return IsFieldInTypeInheritance(parentObject, fieldName, out fieldResult);
                }
            }

            fieldResult = null;
            return false;
        }

        /// <summary>
        /// Get all fields of object <paramref name="obj"/> that have the name <paramref name="fieldName"/>.
        /// </summary>
        /// <param name="obj">Object to check fields on</param>
        /// <param name="fieldName">Field name to look for</param>
        /// <param name="ignoreType">Type name to ignore fields for (optional)</param>
        /// <returns>List of fields</returns>
        public List<NifXmlField> GetFieldsWithName(INifXmlObject obj, string fieldName, string ignoreType = null)
        {
            var fields = obj.Fields.Where(
                f =>
                f.Name == fieldName &&
                (ignoreType == null || f.Type != ignoreType)).ToList();

            if (!string.IsNullOrWhiteSpace(obj.Inherit))
            {
                var parentObject = Objects.FirstOrDefault(o => o.Name == obj.Inherit);
                if (parentObject != null)
                {
                    fields.AddRange(GetFieldsWithName(parentObject, fieldName));
                }
            }

            return fields;
        }

        /// <summary>
        /// Returns amount of fields of object <paramref name="obj"/> that have the name <paramref name="fieldName"/>.
        /// Same as <seealso cref="GetFieldsWithName(INifXmlObject, string, string)"/> but as a count.
        /// </summary>
        /// <param name="obj">Object to check fields on</param>
        /// <param name="fieldName">Field name to look for</param>
        /// <param name="ignoreType">Type name to ignore fields for (optional)</param>
        /// <returns>Field count</returns>
        public int GetFieldNameCount(INifXmlObject obj, string fieldName, string ignoreType = null)
        {
            return GetFieldsWithName(obj, fieldName, ignoreType).Count;
        }

        /// <summary>
        /// Does type mapping on the supplied <paramref name="typeName"/> based on <see cref="TypeMapping"/> as necessary.
        /// </summary>
        /// <param name="typeName">Source type name to map</param>
        /// <returns>Mapped type name or unchanged string</returns>
        public static string DoTypeMapping(string typeName)
        {
            if (typeName == null)
                return null;

            if (TypeMapping.ContainsKey(typeName))
                return TypeMapping[typeName];

            return typeName;
        }

        /// <summary>
        /// Checks if <paramref name="typeName"/> is a base type based on <see cref="BaseTypes"/>.
        /// Does type mapping beforehand.
        /// </summary>
        /// <param name="typeName">Type name to check</param>
        public static bool IsBaseType(string typeName)
        {
            typeName = DoTypeMapping(typeName);

            if (typeName == null)
                return false;

            if (BaseTypes.Contains(typeName))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if the type name is that of a numeric type.
        /// </summary>
        /// <param name="typeName">Type name to check</param>
        public static bool IsNumericIndexType(string typeName)
        {
            typeName = DoTypeMapping(typeName);

            return
                typeName == "sbyte" || typeName == "byte" ||
                typeName == "short" || typeName == "ushort" ||
                typeName == "int" || typeName == "uint" ||
                typeName == "long" || typeName == "ulong";
        }

        /// <summary>
        /// Get comment text of XML node <paramref name="node"/> (first text child node).
        /// </summary>
        /// <param name="node">XML node to get comment text of</param>
        /// <returns>Comment/node text or null</returns>
        public static string GetCommentText(XmlNode node)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.NodeType == XmlNodeType.Text || childNode.NodeType == XmlNodeType.CDATA)
                {
                    return childNode.Value;
                }
            }

            return null;
        }
    }
}
