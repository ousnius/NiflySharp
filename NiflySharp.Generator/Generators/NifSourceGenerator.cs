using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace NiflySharp.Generator
{
    [Generator]
    public class NifSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext initContext)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                // Allows attaching a debugger to the source generator process
                //Debugger.Launch();
            }
#endif 

            // Find all additional files that end with .xml
            IncrementalValuesProvider<AdditionalText> textFiles = initContext.AdditionalTextsProvider.Where(file => file.Path.EndsWith(".xml"));

            // Read their contents and save their name
            IncrementalValuesProvider<(string Path, string Content)> namesAndContents = textFiles.Select((text, cancellationToken) => (text.Path, Content: text.GetText(cancellationToken)?.ToString()));

            initContext.RegisterSourceOutput(namesAndContents, (spc, nameAndContent) =>
            {
                // Deserialize the nif.xml
                Debug.WriteLine($"Deserializing '{nameAndContent.Path}'...");
                var nifXml = NifXml.DeserializeXml(nameAndContent.Content);

                // Generate enum sources
                foreach (var nifEnum in nifXml.Enums)
                    GenerateSource(spc, nifEnum);

                // Generate bitflags sources
                foreach (var nifBitflags in nifXml.Bitflags)
                    GenerateSource(spc, nifBitflags);

                // Generate bitfield sources
                foreach (var nifBitfield in nifXml.Bitfields)
                    GenerateSource(spc, nifXml, nifBitfield);

                // Generate object/struct sources
                foreach (var nifObject in nifXml.EnumerateObjectsAndStructs())
                    GenerateSource(spc, nifXml, nifObject);
            });
        }

        /// <summary>
        /// Generate enum sources
        /// </summary>
        public void GenerateSource(SourceProductionContext context, NifXmlEnum nifEnum)
        {
            string typeName = SourceGenUtil.NormalizeTypeName(nifEnum.Name);
            string storageType = NifXml.DoTypeMapping(nifEnum.StorageType);

            if (SourceGenUtil.SkippedTypes.Contains(typeName))
                return;

            string enumComment = string.Empty;
            if (nifEnum.Comment != null)
            {
                // Split comment into clean lines
                var splitCommentLines = nifEnum.Comment.Trim().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in splitCommentLines)
                {
                    enumComment += $"    /// {line.Trim()}\r\n";
                }

                if (enumComment.Length > 0)
                {
                    // Add comment as summary
                    enumComment =
                        $"\r\n" +
                        $"    /// <summary>\r\n" +
                        $"{enumComment}" +
                        $"    /// </summary>";
                }
            }

            string enumOptionsSection = string.Empty;
            foreach (var enumOption in nifEnum.Options)
            {
                string enumOptionValue = enumOption.Value;
                string enumOptionName = enumOption.Name.Replace(" ", "");
                string enumOptionComment = string.Empty;

                if (enumOption.Comment != null)
                {
                    // Split comment into clean lines
                    var splitCommentLines = enumOption.Comment.Trim().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in splitCommentLines)
                    {
                        enumOptionComment += $"        /// {line.Trim()}\r\n";
                    }

                    if (enumOptionComment.Length > 0)
                    {
                        // Add comment as summary
                        enumOptionComment =
                            $"\r\n" +
                            $"        /// <summary>\r\n" +
                            $"{enumOptionComment}" +
                            $"        /// </summary>";
                    }
                }

                // Add enum option (name and value)
                enumOptionsSection +=
                    $"{enumOptionComment}\r\n" +
                    $"        {enumOptionName} = {enumOptionValue},\r\n";
            }

            enumOptionsSection = enumOptionsSection.TrimEnd().TrimEnd(',');

            // Add storage type of enum
            string typeSuffix = storageType;
            if (!string.IsNullOrWhiteSpace(typeSuffix))
                typeSuffix = $" : {typeSuffix}";

            // Build up the enum source code
            string source =
$@"// <auto-generated/>
namespace NiflySharp.Enums
{{{enumComment}
    public enum {typeName}{typeSuffix}
    {{{enumOptionsSection}
    }}
}}
";

            // Add the source code to the compilation
            string hintName = $"Enum.{typeName}.g.cs";

            context.AddSource(hintName, source);
            Debug.WriteLine($"Source: {hintName}");
        }

        /// <summary>
        /// Generate bitflags sources
        /// </summary>
        public void GenerateSource(SourceProductionContext context, NifXmlBitflags nifBitflags)
        {
            string typeName = SourceGenUtil.NormalizeTypeName(nifBitflags.Name);
            string storageType = NifXml.DoTypeMapping(nifBitflags.StorageType);

            if (SourceGenUtil.SkippedTypes.Contains(typeName))
                return;

            string comment = string.Empty;
            if (nifBitflags.Comment != null)
            {
                // Split comment into clean lines
                var splitCommentLines = nifBitflags.Comment.Trim().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in splitCommentLines)
                {
                    comment += $"    /// {line.Trim()}\r\n";
                }

                if (comment.Length > 0)
                {
                    // Add comment as summary
                    comment =
                        $"\r\n" +
                        $"    /// <summary>\r\n" +
                        $"{comment}" +
                        $"    /// </summary>";
                }
            }

            string optionsSection = string.Empty;
            foreach (var bitflagsOption in nifBitflags.Options)
            {
                string optionName = bitflagsOption.Name.Replace(" ", "");
                string optionComment = string.Empty;

                if (bitflagsOption.Comment != null)
                {
                    // Split comment into clean lines
                    var splitCommentLines = bitflagsOption.Comment.Trim().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in splitCommentLines)
                    {
                        optionComment += $"        /// {line.Trim()}\r\n";
                    }

                    if (optionComment.Length > 0)
                    {
                        // Add comment as summary
                        optionComment =
                            $"\r\n" +
                            $"        /// <summary>\r\n" +
                            $"{optionComment}" +
                            $"        /// </summary>";
                    }
                }

                string value = $"1 << {bitflagsOption.Bit}";
                if (storageType == "uint" && bitflagsOption.Bit >= 31 ||
                    storageType == "ushort" && bitflagsOption.Bit >= 15 ||
                    storageType == "byte" && bitflagsOption.Bit >= 7)
                {
                    // Add cast for unsigned storage types
                    value = $"({storageType})((long){value})";
                }

                // Add enum options/bitflag (name and value)
                optionsSection +=
                    $"{optionComment}\r\n" +
                    $"        {optionName} = {value},\r\n";
            }

            optionsSection = optionsSection.TrimEnd().TrimEnd(',');

            // Add storage type of enum/bitflags
            string typeSuffix = storageType;
            if (!string.IsNullOrWhiteSpace(typeSuffix))
                typeSuffix = $" : {typeSuffix}";

            // Build up the bitflags source code
            string source =
$@"// <auto-generated/>
namespace NiflySharp.Enums
{{{comment}
    public enum {typeName}{typeSuffix}
    {{{optionsSection}
    }}
}}
";

            // Add the source code to the compilation
            string hintName = $"Bitflags.{typeName}.g.cs";

            context.AddSource(hintName, source);
            Debug.WriteLine($"Source: {hintName}");
        }

        /// <summary>
        /// Generate bitfield sources
        /// </summary>
        public void GenerateSource(SourceProductionContext context, NifXml nifXml, NifXmlBitfield nifBitfield)
        {
            string typeName = SourceGenUtil.NormalizeTypeName(nifBitfield.Name);
            string storageType = NifXml.DoTypeMapping(nifBitfield.StorageType);

            if (SourceGenUtil.SkippedTypes.Contains(typeName))
                return;

            string comment = string.Empty;
            if (nifBitfield.Comment != null)
            {
                // Split comment into clean lines
                var splitCommentLines = nifBitfield.Comment.Trim().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in splitCommentLines)
                {
                    comment += $"    /// {line.Trim()}\r\n";
                }

                if (comment.Length > 0)
                {
                    // Add comment as summary
                    comment =
                        $"\r\n" +
                        $"    /// <summary>\r\n" +
                        $"{comment}" +
                        $"    /// </summary>";
                }
            }

            string bitVectorType = "BitVector32";
            if (storageType == "long" || storageType == "ulong")
                bitVectorType = "BitVector64";

            // Build sections and constructor
            string sectionsCtor = string.Empty;
            foreach (var bitflagsMember in nifBitfield.Members)
            {
                string memberName = bitflagsMember.Name.Replace(" ", "");

                sectionsCtor +=
                    $"        private readonly {bitVectorType}.Section Section{memberName};\r\n";
            }

            sectionsCtor +=
                $"\r\n" +
                $"        public {typeName}()\r\n" +
                $"        {{\r\n";

            string lastSectionName = null;
            foreach (var bitflagsMember in nifBitfield.Members)
            {
                string memberName = bitflagsMember.Name.Replace(" ", "");

                if (lastSectionName == null)
                {
                    sectionsCtor +=
                        $"            Section{memberName} = {bitVectorType}.CreateSection((1 << {bitflagsMember.Width}) - 1);\r\n";
                }
                else
                {
                    sectionsCtor +=
                        $"            Section{memberName} = {bitVectorType}.CreateSection((1 << {bitflagsMember.Width}) - 1, {lastSectionName});\r\n";
                }

                lastSectionName = $"Section{memberName}";
            }

            sectionsCtor +=
                $"\r\n" +
                $"            SetDefaults();\r\n" +
                $"        }}\r\n" +
                $"\r\n" +
                $"        public {typeName}({storageType} val) : this()\r\n" +
                $"        {{\r\n" +
                $"            Value = val;\r\n" +
                $"        }}\r\n";

            // Build defaults setter function
            string defaultsFunc =
                $"        public void SetDefaults()\r\n" +
                $"        {{\r\n" +
                $"            bits = new {bitVectorType}(0);\r\n\r\n";

            foreach (var bitflagsMember in nifBitfield.Members.Where(m => !string.IsNullOrWhiteSpace(m.Default)))
            {
                string memberName = bitflagsMember.Name.Replace(" ", "");
                string memberTypeName = NifXml.DoTypeMapping(SourceGenUtil.NormalizeTypeName(bitflagsMember.Type));

                string defaultString = bitflagsMember.Default;

                // Replace tokens that apply to 'default'
                foreach (var token in nifXml.Tokens.Where(t => t.GetAttributesAsArray().Contains("default")).SelectMany(t => t.Entries))
                    defaultString = defaultString.Replace(token.Token, token.String);

                if (SourceGenUtil.FloatValueTypes.Contains(memberTypeName))
                {
                    // Append 'f' for float literals
                    defaultString = Regex.Replace(defaultString, @"[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?", (match) =>
                    {
                        return match.Value + "f";
                    });
                }

                if (memberTypeName == "bool?")
                {
                    switch (defaultString)
                    {
                        case "0":
                            defaultString = " = false";
                            break;
                        case "1":
                            defaultString = " = true";
                            break;
                        case "2":
                            // Assign value "2" of booleans as null in nullable boolean type (bool?)
                            defaultString = " = null";
                            break;
                        default:
                            defaultString = $" = {defaultString}";
                            break;
                    }
                }
                else if (nifXml.IsStructType(bitflagsMember))
                {
                    // Call matching struct constructor (needs to be defined)
                    defaultString = $" = new ({defaultString})";
                }
                else if (nifXml.IsEnumType(bitflagsMember))
                {
                    // Assign matching enum option as default value
                    bool isLiteral = char.IsNumber(bitflagsMember.Default[0]);
                    if (isLiteral)
                        defaultString = $" = ({memberTypeName}){defaultString}";
                    else
                        defaultString = $" = {memberTypeName}.{defaultString}";
                }
                else
                    defaultString = $" = {defaultString}";

                defaultsFunc +=
                    $"            {memberName}{defaultString};\r\n";
            }

            defaultsFunc +=
                $"        }}\r\n";

            // Build value property
            string valueProperty =
                $"        public {storageType} Value\r\n" +
                $"        {{\r\n" +
                $"            get => ({storageType})bits.Data;\r\n" +
                $"            set => bits = new {bitVectorType}(value);\r\n" +
                $"        }}\r\n";

            // Add properties for bitfield members
            string bitfieldProperties = string.Empty;
            foreach (var bitflagsMember in nifBitfield.Members)
            {
                string memberName = bitflagsMember.Name.Replace(" ", "");
                string memberComment = string.Empty;

                if (bitflagsMember.Comment != null)
                {
                    // Split comment into clean lines
                    var splitCommentLines = bitflagsMember.Comment.Trim().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in splitCommentLines)
                    {
                        memberComment += $"        /// {line.Trim()}\r\n";
                    }

                    if (memberComment.Length > 0)
                    {
                        // Add comment as summary
                        memberComment =
                            $"\r\n" +
                            $"        /// <summary>\r\n" +
                            $"{memberComment}" +
                            $"        /// </summary>";
                    }
                }

                string memberTypeName = NifXml.DoTypeMapping(SourceGenUtil.NormalizeTypeName(bitflagsMember.Type));
                if (memberTypeName == "bool?")
                    memberTypeName = "bool";

                if (memberTypeName == "bool" && bitflagsMember.Width > 1)
                    memberTypeName = "int";

                // Build get/set for property
                string getSetProperty;
                if (memberTypeName == "bool")
                {
                    getSetProperty =
                        $"            get => Convert.ToBoolean(bits[Section{memberName}]);\r\n" +
                        $"            set => bits[Section{memberName}] = Convert.ToInt32(value);\r\n";
                }
                else
                {
                    string valueCastType;
                    if (storageType == "long" || storageType == "ulong")
                        valueCastType = "long";
                    else
                        valueCastType = "int";

                    getSetProperty =
                        $"            get => ({memberTypeName})bits[Section{memberName}];\r\n" +
                        $"            set => bits[Section{memberName}] = ({valueCastType})value;\r\n";
                }

                // Add property for bitfield
                bitfieldProperties +=
                    $"{memberComment}\r\n" +
                    $"        public {memberTypeName} {memberName}\r\n" +
                    $"        {{\r\n" +
                    $"{getSetProperty}" +
                    $"        }}\r\n\r\n";
            }

            // Build sync function
            string syncFunc =
                    $"        public void Sync(NiStreamReversible stream)\r\n" +
                    $"        {{\r\n" +
                    $"            {storageType} val = default;\r\n" +
                    $"\r\n" +
                    $"            if (stream.CurrentMode == NiStreamReversible.Mode.Write)\r\n" +
                    $"                val = Value;\r\n" +
                    $"\r\n" +
                    $"            stream.Sync(ref val);\r\n" +
                    $"\r\n" +
                    $"            if (stream.CurrentMode == NiStreamReversible.Mode.Read)\r\n" +
                    $"                Value = val;\r\n" +
                    $"        }}";

            string typeInherit = " : INiStreamable";

            // Build up the bitfields source code
            string source = $@"// <auto-generated/>
using NiflySharp.Bitfields;
using NiflySharp.Enums;
using NiflySharp.Stream;
using NiflySharp.Structs;
using System;
using System.Collections.Specialized;

namespace NiflySharp.Bitfields
{{{comment}
    public partial class {typeName}{typeInherit}
    {{
        private {bitVectorType} bits;

{sectionsCtor}
{defaultsFunc}
{valueProperty}
{bitfieldProperties}
{syncFunc}
    }}
}}
";

            // Add the source code to the compilation
            string hintName = $"Bitfields.{typeName}.g.cs";

            context.AddSource(hintName, source);
            Debug.WriteLine($"Source: {hintName}");
        }

        /// <summary>
        /// Generate object/struct sources
        /// </summary>
        public void GenerateSource(SourceProductionContext context, NifXml nifXml, INifXmlObject nifObject)
        {
            string typeName = SourceGenUtil.NormalizeTypeName(nifObject.Name);

            if (SourceGenUtil.SkippedTypes.Contains(typeName))
                return;

            string inherit;
            if (!nifObject.IsStruct && nifObject.Inherit != null)
                inherit = $" : {SourceGenUtil.NormalizeTypeName(nifObject.Inherit)}, INiStreamable";
            else
                inherit = " : INiStreamable";

            string classComment = string.Empty;
            if (nifObject.Comment != null)
            {
                // Split comment into clean lines
                var splitCommentLines = nifObject.Comment.Trim().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in splitCommentLines)
                {
                    classComment += $"    /// {line.Trim()}\r\n";
                }

                if (classComment.Length > 0)
                {
                    // Add comment as summary
                    classComment =
                        $"\r\n" +
                        $"    /// <summary>\r\n" +
                        $"{classComment}" +
                        $"    /// </summary>";
                }
            }

            var fieldNamesAndTypes = new HashSet<string>();

            string fieldsSection = string.Empty;
            string publicPropertiesSection = string.Empty;
            string syncFuncBody = string.Empty;
            string refsBody = string.Empty;
            string ptrsBody = string.Empty;
            string refArraysBody = string.Empty;
            string stringRefsBody = string.Empty;
            string lastSyncFuncCondition = null;
            var dictArrayCountMembers = new Dictionary<string, NifXmlField>();

            // Stop condition for IO
            if (!string.IsNullOrWhiteSpace(nifObject.StopCondition))
            {
                var replacedFieldNames = new List<string>();

                string cond = nifObject.StopCondition;
                cond = cond.Replace("#ARG#", "Convert.ToInt32(stream.Argument)");

                // Replace tokens
                foreach (var token in nifXml.Tokens.Where(t => t.GetAttributesAsArray().Contains("cond")).SelectMany(t => t.Entries))
                {
                    switch (token.Token)
                    {
                        case "#VER#":
                            cond = cond.Replace(token.Token, "stream.Version.FileVersion");
                            break;
                        case "#USER#":
                            cond = cond.Replace(token.Token, "stream.Version.UserVersion");
                            break;
                        case "#BSVER#":
                            cond = cond.Replace(token.Token, "stream.Version.StreamVersion");
                            break;
                        case "#FLT_INF#":
                            cond = cond.Replace(token.Token, "float.PositiveInfinity");
                            break;
                        default:
                            cond = cond.Replace(token.Token, token.String);
                            break;
                    }
                }

                cond = SourceGenUtil.ReplaceFieldNames(cond, nifXml, nifObject, typeName);

                if (cond.Contains("stream.Version.FileVersion"))
                {
                    // Find version "a.b.c.d" pattern and replace with function
                    string pattern = @"(\d+)\.(\d+)\.(\d+)\.(\d+)";
                    string replacement = "NiVersion.ToFile($1, $2, $3, $4)";
                    cond = Regex.Replace(cond, pattern, replacement);
                }

                // FIXME: Is there a better way to do the check for the name string ref?
                cond = cond.Replace(" _name", " !string.IsNullOrEmpty(stream.File.Header.GetString(_name.Index))");

                syncFuncBody +=
                    $"            if ({cond})\r\n" +
                    $"                return;\r\n";
            }

            foreach (var field in nifObject.Fields)
            {
                string fieldTypeName = SourceGenUtil.GetFieldTypeName(nifObject, field, out bool isArray, out bool isRef, out bool isPtr, out bool isStringRef);

                string fieldComment = string.Empty;
                if (field.Comment != null)
                {
                    // Split comment into clean lines
                    var splitCommentLines = field.Comment.Trim().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in splitCommentLines)
                    {
                        fieldComment += $"        /// {line.Trim()}\r\n";
                    }

                    if (fieldComment.Length > 0)
                    {
                        // Add comment as summary
                        fieldComment =
                            $"\r\n" +
                            $"        /// <summary>\r\n" +
                            $"{fieldComment}" +
                            $"        /// </summary>";
                    }
                }

                string fieldName = SourceGenUtil.NormalizeFieldName(field.Name, typeName); // Field name for private/internal fields (_camelCase)
                string fieldNameUp = SourceGenUtil.NormalizeFieldName(field.Name, typeName, false); // Field name for public properties (CamelCase)
                string origFieldName = fieldName;

                if (nifXml.GetFieldNameCount(nifObject, field.Name, field.Type) > 0)
                {
                    // Append an abbreviation of the field type if it appears for than once in the object
                    string typeAbbrev = SourceGenUtil.AbbreviateType(fieldTypeName);
                    fieldName += $"_{typeAbbrev}";
                    fieldNameUp += $"_{typeAbbrev}";
                }

                // Only create each field/type combination once
                if (!fieldNamesAndTypes.Contains($"{origFieldName}_{fieldTypeName}"))
                {
                    string defaultString = string.Empty;
                    if (!string.IsNullOrWhiteSpace(field.Default) &&
                        field.Length == null /* Ignore default for arrays */)
                    {
                        defaultString = field.Default;

                        // Replace tokens that apply to 'default'
                        foreach (var token in nifXml.Tokens.Where(t => t.GetAttributesAsArray().Contains("default")).SelectMany(t => t.Entries))
                            defaultString = defaultString.Replace(token.Token, token.String);

                        defaultString = SourceGenUtil.ReplaceFieldNames(defaultString, nifXml, nifObject, typeName);

                        if (SourceGenUtil.FloatValueTypes.Contains(field.Type))
                        {
                            // Append 'f' for float literals
                            defaultString = Regex.Replace(defaultString, @"[-+]?([0-9]*[.])?[0-9]+([eE][-+]?\d+)?", (match) =>
                            {
                                return match.Value + "f";
                            });
                        }

                        if (fieldTypeName.StartsWith("NiString"))
                        {
                            // Default string content
                            defaultString = $" = new (\"{defaultString}\")";
                        }
                        else if (fieldTypeName == "bool?")
                        {
                            switch (defaultString)
                            {
                                case "0":
                                    defaultString = " = false";
                                    break;
                                case "1":
                                    defaultString = " = true";
                                    break;
                                case "2":
                                    // Assign value "2" of booleans as null in nullable boolean type (bool?)
                                    defaultString = " = null";
                                    break;
                                default:
                                    defaultString = $" = {defaultString}";
                                    break;
                            }
                        }
                        else if (fieldTypeName == "char" && defaultString.StartsWith("0x"))
                        {
                            // Cast hex bytes to char
                            defaultString = $" = (char){defaultString}";
                        }
                        else if (nifXml.IsStructType(field) || nifXml.IsBitfieldType(field))
                        {
                            // Call matching struct/bitfield constructor (needs to be defined)
                            defaultString = $" = new ({defaultString})";
                        }
                        else if (nifXml.IsEnumType(field))
                        {
                            // Assign matching enum option as default value
                            bool isLiteral = char.IsNumber(field.Default[0]);
                            if (isLiteral)
                                defaultString = $" = ({fieldTypeName}){defaultString}";
                            else
                                defaultString = $" = {fieldTypeName}.{defaultString}";
                        }
                        else
                            defaultString = $" = {defaultString}";
                    }

                    // Create struct fields as internal and class fields as protected
                    fieldsSection +=
                        $"{fieldComment}\r\n" +
                        $"        {(nifObject.IsStruct ? "internal" : "protected")} {fieldTypeName} {fieldName}{defaultString};\r\n";

#if !PUBLIC_PROPERTIES_ALL
                    if (!SourceGenUtil.ClassesWithoutProperties.Contains(typeName))
                    {
#endif
                        // Create additional public property for the field
                        fieldsSection +=
                            $"        public {fieldTypeName} {fieldNameUp} {{ get => {fieldName}; set => {fieldName} = value; }}\r\n";

                        publicPropertiesSection +=
                            $"        public {fieldTypeName} {fieldNameUp} {{ get; set; }}\r\n";
#if !PUBLIC_PROPERTIES_ALL
                    }
#endif

                    if (!isStringRef && nifXml.IsStructType(field))
                    {
                        if (!SourceGenUtil.SkippedTypes.Contains(field.Type))
                        {
                            if (isArray)
                            {
                                refsBody += $"                if ({fieldName} != null) list.AddRange({fieldName}.SelectMany(s => s.References));\r\n";
                                ptrsBody += $"                if ({fieldName} != null) list.AddRange({fieldName}.SelectMany(s => s.Pointers));\r\n";
                                refArraysBody += $"                if ({fieldName} != null) list.AddRange({fieldName}.SelectMany(s => s.ReferenceArrays));\r\n";
                                stringRefsBody += $"                if ({fieldName} != null) list.AddRange({fieldName}.SelectMany(s => s.StringRefs));\r\n";
                            }
                            else
                            {
                                refsBody += $"                list.AddRange({fieldName}.References);\r\n";
                                ptrsBody += $"                list.AddRange({fieldName}.Pointers);\r\n";
                                refArraysBody += $"                list.AddRange({fieldName}.ReferenceArrays);\r\n";
                                stringRefsBody += $"                list.AddRange({fieldName}.StringRefs);\r\n";
                            }
                        }
                    }
                    else
                    {
                        if (isRef)
                        {
                            // Make list of ref fields
                            if (isArray)
                            {
                                refsBody += $"                if ({fieldName} != null) list.AddRange({fieldName}.References);\r\n";
                                refArraysBody += $"                if ({fieldName} != null) list.Add({fieldName});\r\n";
                            }
                            else
                                refsBody += $"                if ({fieldName} != null) list.Add({fieldName});\r\n";
                        }
                        else if (isPtr)
                        {
                            // Make list of ptr fields
                            if (isArray)
                            {
                                ptrsBody += $"                if ({fieldName} != null) list.AddRange({fieldName}.Pointers);\r\n";
                                refArraysBody += $"                if ({fieldName} != null) list.Add({fieldName});\r\n";
                            }
                            else
                                ptrsBody += $"                if ({fieldName} != null) list.Add({fieldName});\r\n";
                        }
                        else if (isStringRef)
                        {
                            // Make list of string ref fields
                            if (isArray)
                                stringRefsBody += $"                if ({fieldName} != null) list.AddRange({fieldName});\r\n";
                            else
                                stringRefsBody += $"                if ({fieldName} != null) list.Add({fieldName});\r\n";
                        }
                        else if (nifObject.Generic && fieldTypeName == "T")
                        {
                            if (!isArray)
                                stringRefsBody += $"                if ({fieldName} != null && {fieldName} is NiStringRef) list.Add({fieldName} as NiStringRef);\r\n";
                        }
                    }

                    // Remember field as added
                    fieldNamesAndTypes.Add($"{origFieldName}_{fieldTypeName}");
                }

                string syncFuncField = string.Empty;
                if (!string.IsNullOrWhiteSpace(field.Length))
                {
                    // Handle arrays with a length
                    string lengthString = field.Length;
                    bool hasFixedLength = lengthString.All(char.IsDigit);

                    // Replace tokens that apply to 'length'
                    foreach (var token in nifXml.Tokens.Where(t => t.GetAttributesAsArray().Contains("length")).SelectMany(t => t.Entries))
                        lengthString = lengthString.Replace(token.Token, token.String);

                    // Handle #ARG1# as #ARG# for now
                    if (lengthString == "#ARG1#")
                        lengthString = "#ARG#";

                    // Replace #ARG# with accessible property in stream object
                    if (lengthString == "#ARG#")
                        lengthString = lengthString.Replace("#ARG#", "stream.Argument");

                    // Replace all field names in length string
                    lengthString = SourceGenUtil.ReplaceFieldNames(lengthString, nifXml, nifObject, typeName);

                    var lengthField = nifObject.Fields.FirstOrDefault(f => f.Name == field.Length);
                    if (lengthField == null)
                    {
                        nifXml.IsFieldInTypeInheritance(nifObject, field.Length, out lengthField);
                    }

                    if (lengthField != null)
                    {
                        if (!dictArrayCountMembers.ContainsValue(lengthField))
                        {
                            dictArrayCountMembers.Add(field.Name, lengthField);
                        }
                    }

                    bool doWidthSum = false;
                    string widthString = string.Empty;
                    if (!string.IsNullOrWhiteSpace(field.Width))
                    {
                        // Handle arrays with an additional width
                        widthString = field.Width;

                        // Replace tokens that apply to 'width'
                        foreach (var token in nifXml.Tokens.Where(t => t.GetAttributesAsArray().Contains("width")).SelectMany(t => t.Entries))
                            widthString = widthString.Replace(token.Token, token.String);

                        // Handle #ARG1# as #ARG# for now
                        if (widthString == "#ARG1#")
                            widthString = "#ARG#";

                        // Replace #ARG# with accessible property in stream object
                        if (widthString == "#ARG#")
                            widthString = widthString.Replace("#ARG#", "stream.Argument");

                        // Replace all field names in width string
                        widthString = SourceGenUtil.ReplaceFieldNames(widthString, nifXml, nifObject, typeName);
                        doWidthSum = nifObject.Fields.Any(f => f.Name == field.Width && !string.IsNullOrWhiteSpace(f.Length) && NifXml.IsNumericIndexType(f.Type));
                    }

                    if (field.Type == "Ref" || field.Type == "Ptr")
                    {
                        // Arrays of block ref/ptr
                        if (string.IsNullOrWhiteSpace(widthString))
                            syncFuncField = $"stream.SetListSize(ref {fieldName}, Convert.ToInt32({lengthString}));\r\n";
                        else
                            syncFuncField = $"stream.SetListSize(ref {fieldName}, Convert.ToInt32({(doWidthSum ? $"{widthString}.Sum(n => n)" : $"{lengthString} * {widthString}")}));\r\n";

                        if (lengthField != null && string.IsNullOrWhiteSpace(widthString))
                        {
                            // Create additional conditions for all colliding field definitions (e.g. mismatching types)
                            string collisionConditions = SourceGenUtil.BuildFieldCollisionConditions(nifXml, lengthField.Name, nifObject, typeName, syncFuncField, lengthString);
                            if (!string.IsNullOrWhiteSpace(collisionConditions))
                            {
                                syncFuncField = collisionConditions;
                            }
                        }

                        syncFuncField += $"{fieldName}.SyncContent(stream);";
                    }
                    else
                    {
                        if (hasFixedLength)
                        {
                            // Array with fixed length
                            if (string.IsNullOrWhiteSpace(widthString))
                                syncFuncField = $"stream.InitArraySize(ref {fieldName}, {lengthString});\r\n";
                            else
                                syncFuncField = $"stream.InitArraySize(ref {fieldName}, Convert.ToInt32({(doWidthSum ? $"{widthString}.Sum(n => n)" : $"{lengthString} * {widthString}")}));\r\n";

                            if (lengthField != null && string.IsNullOrWhiteSpace(widthString))
                            {
                                // Create additional conditions for all colliding field definitions (e.g. mismatching types)
                                string collisionConditions = SourceGenUtil.BuildFieldCollisionConditions(nifXml, lengthField.Name, nifObject, typeName, syncFuncField, lengthString);
                                if (!string.IsNullOrWhiteSpace(collisionConditions))
                                {
                                    syncFuncField = collisionConditions;
                                }
                            }

                            if (NifXml.IsBaseType(field.Type))
                            {
                                // Array of base type values
                                syncFuncField += $"stream.SyncArrayContent({fieldName});";
                            }
                            else if (nifXml.IsEnumType(field))
                            {
                                // Array of enum values
                                syncFuncField += $"stream.SyncArrayContentEnum({fieldName});";
                            }
                            else
                            {
                                // Array of streamable structs (Sync function)
                                syncFuncField += $"stream.SyncArrayContentStreamable({fieldName});";
                            }
                        }
                        else
                        {
                            // List with variable length
                            if (string.IsNullOrWhiteSpace(widthString))
                                syncFuncField = $"stream.SetListSize(ref {fieldName}, Convert.ToInt32({lengthString}));\r\n";
                            else
                                syncFuncField = $"stream.SetListSize(ref {fieldName}, Convert.ToInt32({(doWidthSum ? $"{widthString}.Sum(n => n)" : $"{lengthString} * {widthString}")}));\r\n";

                            if (lengthField != null && string.IsNullOrWhiteSpace(widthString))
                            {
                                // Create additional conditions for all colliding field definitions (e.g. mismatching types)
                                string collisionConditions = SourceGenUtil.BuildFieldCollisionConditions(nifXml, lengthField.Name, nifObject, typeName, syncFuncField, lengthString);
                                if (!string.IsNullOrWhiteSpace(collisionConditions))
                                {
                                    syncFuncField = collisionConditions;
                                }
                            }

                            if (NifXml.IsBaseType(field.Type))
                            {
                                // List of base type values
                                syncFuncField += $"stream.SyncListContent({fieldName});";
                            }
                            else if (nifXml.IsEnumType(field))
                            {
                                // List of enum values
                                syncFuncField += $"stream.SyncListContentEnum({fieldName});";
                            }
                            else
                            {
                                // List of streamable structs (Sync function)
                                syncFuncField += $"stream.SyncListContentStreamable({fieldName});";
                            }
                        }
                    }
                }
                else if (nifXml.IsEnumType(field))
                {
                    // Enum
                    syncFuncField = $"stream.SyncEnum(ref {fieldName});";
                }
                else if (fieldTypeName == "T")
                {
                    // Generic type
                    syncFuncField = $"stream.SyncGeneric(ref {fieldName});";
                }
                else
                {
                    // Base type, struct, block ref/ptr and others
                    syncFuncField = $"stream.Sync(ref {fieldName});";
                }

                // Build argument setter
                string syncFuncArg = string.Empty;
                if (!string.IsNullOrWhiteSpace(field.Arg) && field.Arg != "#ARG#")
                {
                    var replacedFieldNames = new List<string>();

                    string arg = field.Arg;

                    // Replace tokens that apply to 'arg'
                    foreach (var token in nifXml.Tokens.Where(t => t.GetAttributesAsArray().Contains("arg")).SelectMany(t => t.Entries))
                    {
                        arg = arg.Replace(token.Token, token.String);
                    }

                    // Replace all field names in arg string
                    arg = SourceGenUtil.ReplaceFieldNames(arg, nifXml, nifObject, typeName);

                    syncFuncArg = $"stream.Argument = {arg};";
                }

                if (!string.IsNullOrWhiteSpace(syncFuncArg))
                {
                    syncFuncField = $"{syncFuncArg}\r\n{syncFuncField}";
                }

#if NIF_GENCALC // Calc function doesn't always make sense in IO function and doesn't always have the right order
                // Build calc setter
                string syncFuncCalc = string.Empty;
                if (!string.IsNullOrWhiteSpace(field.Calculate))
                {
                    var replacedFieldNames = new List<string>();

                    string calc = field.Calculate;

                    var calcFieldNames = SourceGenUtil.GetFieldNames(calc);

                    // Replace tokens that apply to 'calc'
                    foreach (var token in nifXml.Tokens.Where(t => t.GetAttributesAsArray().Contains("calc")).SelectMany(t => t.Entries))
                    {
                        calc = calc.Replace(token.Token, token.String);
                    }

                    // Replace all field names in calc string
                    calc = SourceGenUtil.ReplaceFieldNames(calc, nifXml, nifObject, typeName);
                    calc = calc.Replace("#THEN#", "?").Replace("#ELSE#", ":");

                    // Replace #LEN[field]# with array count
                    calc = Regex.Replace(calc, @"#LEN\[(\w+)\]#", (match) =>
                    {
                        return $"({match.Groups[1]}?.Count ?? 0)";
                    });

                    // Replace #LEN2[field]# with sum of numeric values in array
                    calc = Regex.Replace(calc, @"#LEN2\[(\w+)\]#", (match) =>
                    {
                        return $"({match.Groups[1]}?.Sum(n => n) ?? 0)";
                    });

                    syncFuncCalc = $"{fieldName} = ({fieldTypeName})({calc});\r\n";

                    foreach (var calcFieldName in calcFieldNames)
                    {
                        string normFieldName = SourceGenUtil.NormalizeFieldName(calcFieldName, typeName);

                        // Create additional conditions for all colliding field definitions (e.g. mismatching types)
                        string collisionConditions = SourceGenUtil.BuildFieldCollisionConditions(nifXml, calcFieldName, nifObject, typeName, syncFuncCalc, normFieldName);
                        if (!string.IsNullOrWhiteSpace(collisionConditions))
                        {
                            syncFuncCalc = collisionConditions;
                        }
                    }

                    // Some field values should not be calculated
                    // when they currently have a value of 0 to prevent unneeded data from being created
                    bool calcNotNull = SourceGenUtil.CalcNotNullFields.Contains(field.Name);

                    syncFuncCalc =
                        $"if (stream.CurrentMode == NiStreamReversible.Mode.Write{(calcNotNull ? $" &&\r\n    {fieldName} > 0" : "")})\r\n" +
                        $"{{\r\n" +
                        $"    {syncFuncCalc.Replace("\r\n", "\r\n    ").TrimEnd(' ')}" +
                        $"}}\r\n";
                }

                if (!string.IsNullOrWhiteSpace(syncFuncCalc))
                {
                    // FIXME: How to fix this in a proper manner??
                    syncFuncCalc = syncFuncCalc.Replace("+ (_numTriangles_us", "+ (uint)(_numTriangles_us");

                    syncFuncField = $"{syncFuncCalc}{syncFuncField}";
                }
#endif

                // Build conditions of field
                var syncFuncConditions = SourceGenUtil.BuildFieldConditions(nifXml, field, nifObject, typeName);

                // Combine conditions with &&
                string syncFuncCondition = string.Empty;
                foreach (var cond in syncFuncConditions)
                {
                    if (string.IsNullOrWhiteSpace(syncFuncCondition))
                        syncFuncCondition += $"{cond}";
                    else
                        syncFuncCondition += $" &&\r\n                {cond}";
                }

                // Build final if-branch with all conditions
                if (!string.IsNullOrWhiteSpace(syncFuncCondition))
                {
                    if (lastSyncFuncCondition != syncFuncCondition)
                    {
                        if (lastSyncFuncCondition != null)
                            syncFuncBody += "            }\r\n";

                        syncFuncField =
                            $"            if ({syncFuncCondition})\r\n" +
                            $"            {{\r\n" +
                            $"                {syncFuncField.Replace("\r\n", "\r\n                ")}\r\n";

                        if (!string.IsNullOrWhiteSpace(syncFuncBody))
                            syncFuncBody += "\r\n";
                    }
                    else
                    {
                        syncFuncField = $"                {syncFuncField.Replace("\r\n", "\r\n                ")}\r\n";
                    }

                    lastSyncFuncCondition = syncFuncCondition;
                }
                else
                {
                    if (lastSyncFuncCondition != null)
                        syncFuncBody += "            }\r\n";

                    if (!string.IsNullOrWhiteSpace(syncFuncBody))
                        syncFuncBody += "\r\n";

                    syncFuncField = $"            {syncFuncField.Replace("\r\n", "\r\n            ")}";
                    lastSyncFuncCondition = null;
                }

                syncFuncBody += syncFuncField;
            }

            if (lastSyncFuncCondition != null)
                syncFuncBody += "            }\r\n";

            syncFuncBody = syncFuncBody.TrimEnd();

            if (!string.IsNullOrWhiteSpace(refsBody))
            {
                // Property to enumerate all block references
                if (nifObject.IsStruct)
                {
                    fieldsSection +=
                        "\r\n" +
                        $"        public IEnumerable<INiRef> References\r\n" +
                        "        {\r\n" +
                        "            get\r\n" +
                        "            {\r\n" +
                        "                var list = new List<INiRef>();\r\n" +
                        $"{refsBody}" +
                        "                return list;\r\n" +
                        "            }\r\n" +
                        "        }\r\n";
                }
                else
                {
                    fieldsSection +=
                        "\r\n" +
                        $"        public override IEnumerable<INiRef> References\r\n" +
                        "        {\r\n" +
                        "            get\r\n" +
                        "            {\r\n" +
                        "                var list = new List<INiRef>();\r\n" +
                        "                list.AddRange(base.References);\r\n" +
                        $"{refsBody}" +
                        "                return list;\r\n" +
                        "            }\r\n" +
                        "        }\r\n";
                }
            }
            else
            {
                if (nifObject.IsStruct)
                {
                    // Empty property for block references
                    fieldsSection +=
                        "\r\n" +
                        $"        public IEnumerable<INiRef> References\r\n" +
                        "        {\r\n" +
                        "            get\r\n" +
                        "            {\r\n" +
                        "                return [];\r\n" +
                        "            }\r\n" +
                        "        }\r\n";
                }
            }

            if (!string.IsNullOrWhiteSpace(ptrsBody))
            {
                // Property to enumerate all block pointers
                if (nifObject.IsStruct)
                {
                    fieldsSection +=
                        "\r\n" +
                        $"        public IEnumerable<INiRef> Pointers\r\n" +
                        "        {\r\n" +
                        "            get\r\n" +
                        "            {\r\n" +
                        "                var list = new List<INiRef>();\r\n" +
                        $"{ptrsBody}" +
                        "                return list;\r\n" +
                        "            }\r\n" +
                        "        }\r\n";
                }
                else
                {
                    fieldsSection +=
                        "\r\n" +
                        $"        public override IEnumerable<INiRef> Pointers\r\n" +
                        "        {\r\n" +
                        "            get\r\n" +
                        "            {\r\n" +
                        "                var list = new List<INiRef>();\r\n" +
                        "                list.AddRange(base.Pointers);\r\n" +
                        $"{ptrsBody}" +
                        "                return list;\r\n" +
                        "            }\r\n" +
                        "        }\r\n";
                }
            }
            else
            {
                if (nifObject.IsStruct)
                {
                    // Empty property for block pointers
                    fieldsSection +=
                        "\r\n" +
                        $"        public IEnumerable<INiRef> Pointers\r\n" +
                        "        {\r\n" +
                        "            get\r\n" +
                        "            {\r\n" +
                        "                return [];\r\n" +
                        "            }\r\n" +
                        "        }\r\n";
                }
            }

            if (!string.IsNullOrWhiteSpace(refArraysBody))
            {
                // Property to enumerate all block reference arrays
                if (nifObject.IsStruct)
                {
                    fieldsSection +=
                        "\r\n" +
                        $"        public IEnumerable<NiRefArray> ReferenceArrays\r\n" +
                        "        {\r\n" +
                        "            get\r\n" +
                        "            {\r\n" +
                        "                var list = new List<NiRefArray>();\r\n" +
                        $"{refArraysBody}" +
                        "                return list;\r\n" +
                        "            }\r\n" +
                        "        }\r\n";
                }
                else
                {
                    fieldsSection +=
                        "\r\n" +
                        $"        public override IEnumerable<NiRefArray> ReferenceArrays\r\n" +
                        "        {\r\n" +
                        "            get\r\n" +
                        "            {\r\n" +
                        "                var list = new List<NiRefArray>();\r\n" +
                        "                list.AddRange(base.ReferenceArrays);\r\n" +
                        $"{refArraysBody}" +
                        "                return list;\r\n" +
                        "            }\r\n" +
                        "        }\r\n";
                }
            }
            else
            {
                if (nifObject.IsStruct)
                {
                    // Empty property for block reference arrays
                    fieldsSection +=
                        "\r\n" +
                        $"        public IEnumerable<NiRefArray> ReferenceArrays\r\n" +
                        "        {\r\n" +
                        "            get\r\n" +
                        "            {\r\n" +
                        "                return [];\r\n" +
                        "            }\r\n" +
                        "        }\r\n";
                }
            }

            if (!string.IsNullOrWhiteSpace(stringRefsBody))
            {
                // Property to enumerate all string references
                if (nifObject.IsStruct)
                {
                    fieldsSection +=
                        "\r\n" +
                        $"        public IEnumerable<NiStringRef> StringRefs\r\n" +
                        "        {\r\n" +
                        "            get\r\n" +
                        "            {\r\n" +
                        "                var list = new List<NiStringRef>();\r\n" +
                        $"{stringRefsBody}" +
                        "                return list;\r\n" +
                        "            }\r\n" +
                        "        }\r\n";
                }
                else
                {
                    fieldsSection +=
                        "\r\n" +
                        $"        public override IEnumerable<NiStringRef> StringRefs\r\n" +
                        "        {\r\n" +
                        "            get\r\n" +
                        "            {\r\n" +
                        "                var list = new List<NiStringRef>();\r\n" +
                        "                list.AddRange(base.StringRefs);\r\n" +
                        $"{stringRefsBody}" +
                        "                return list;\r\n" +
                        "            }\r\n" +
                        "        }\r\n";
                }
            }
            else
            {
                if (nifObject.IsStruct)
                {
                    // Empty property for string references
                    fieldsSection +=
                        "\r\n" +
                        $"        public IEnumerable<NiStringRef> StringRefs\r\n" +
                        "        {\r\n" +
                        "            get\r\n" +
                        "            {\r\n" +
                        "                return [];\r\n" +
                        "            }\r\n" +
                        "        }\r\n";
                }
            }

            string syncFuncArrayCounts = string.Empty;

            foreach (var members in dictArrayCountMembers)
            {
                string normFieldName = SourceGenUtil.NormalizeFieldName(members.Key, null);
                string normLengthFieldName = SourceGenUtil.NormalizeFieldName(members.Value.Name, null);
                string normLengthTypeName = SourceGenUtil.GetFieldTypeName(nifObject, members.Value, out _, out _, out _, out _);

                string arrayCountFunc = $"{normLengthFieldName} = ({normLengthTypeName})({normFieldName}?.Count ?? 0);\r\n";

                // Create additional conditions for all colliding field definitions (e.g. mismatching types) for the array field
                string collisionConditionsArray = SourceGenUtil.BuildFieldCollisionConditions(nifXml, members.Key, nifObject, typeName, arrayCountFunc, normFieldName, null, normLengthFieldName, "0");
                if (!string.IsNullOrWhiteSpace(collisionConditionsArray))
                {
                    arrayCountFunc = collisionConditionsArray.Replace("\r\n", "\r\n            ");
                }

                // Create additional conditions for all colliding field definitions (e.g. mismatching types) for the length field
                string collisionConditionsLengthType = SourceGenUtil.BuildFieldCollisionConditions(nifXml, members.Value.Name, nifObject, typeName, arrayCountFunc, normLengthFieldName, normLengthTypeName, normLengthFieldName, "0");
                if (!string.IsNullOrWhiteSpace(collisionConditionsLengthType))
                {
                    arrayCountFunc = collisionConditionsLengthType.Replace("\r\n", "\r\n            ");
                }

                arrayCountFunc = arrayCountFunc.TrimEnd();
                syncFuncArrayCounts += $"            {arrayCountFunc}\r\n";
            }

            if (!string.IsNullOrWhiteSpace(syncFuncArrayCounts))
                syncFuncArrayCounts += "\r\n";

            string typePrefix;
            string typeConstructor;
            if (nifObject.IsStruct)
            {
                // Type definition for structs
                typePrefix = "public partial struct";
                typeConstructor = $"        public {typeName}() {{ }}\r\n\r\n";
            }
            else
            {
                // Type definition for classes
                typePrefix = "public partial class";
                typeConstructor = string.Empty;
            }

            string syncFunc = string.Empty;
            if (!nifObject.IsStruct)
            {
                // Sync function definition for classes (call base.Sync)
                syncFunc =
                    $"        public override void Sync(NiStreamReversible stream)\r\n" +
                    $"        {{\r\n" +
                    $"            base.Sync(stream);\r\n" +
                    $"{syncFuncArrayCounts}" +
                    $"            this.BeforeSync(stream);\r\n" +
                    $"\r\n" +
                    $"{syncFuncBody}\r\n" +
                    $"\r\n" +
                    $"            this.AfterSync(stream);\r\n" +
                    $"        }}";
            }
            else
            {
                // Sync function definition for structs
                syncFunc =
                    $"        public void Sync(NiStreamReversible stream)\r\n" +
                    $"        {{\r\n" +
                    $"{syncFuncBody}\r\n" +
                    $"        }}";
            }

            string typeNameSuffix = string.Empty;
            if (nifObject.Generic)
            {
                // Make type generic
                typeNameSuffix = "<T>";
            }

            // Build up the class/struct source code
            string source =
$@"// <auto-generated/>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NiflySharp.Bitfields;
using NiflySharp.Enums;
using NiflySharp.Stream;
using {(nifObject.IsStruct ? "NiflySharp.Blocks" : "NiflySharp.Structs")};

namespace {(nifObject.IsStruct ? "NiflySharp.Structs" : "NiflySharp.Blocks")}
{{{classComment}
    {typePrefix} {typeName}{typeNameSuffix}{inherit}
    {{{fieldsSection}
{typeConstructor}{syncFunc}
    }}
}}
";

            // Add the source code to the compilation
            string hintName = $"{typeName}.g.cs";
            if (!string.IsNullOrWhiteSpace(nifObject.Module))
                hintName = $"{nifObject.Module}.{hintName}";

            context.AddSource(hintName, source);
            Debug.WriteLine($"Source (block): {hintName}");
        }
    }
}