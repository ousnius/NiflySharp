using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NiflySharp.Generator
{
    /// <summary>
    /// Utility and constants for the source generator
    /// </summary>
    public static class SourceGenUtil
    {
        /// <summary>
        /// Types to skip code generation for entirely
        /// </summary>
        public static readonly string[] SkippedTypes =
        {
            "string",
            "SizedString",
            "SizedString16",
            "Header",
            "NiObject",
            "Vector2",
            "Vector3",
            "Vector4",
            "Quaternion",
            "BSVertexData",
            "BSVertexDataSSE"
        };

        /// <summary>
        /// Classes that already provide a hand-written parameterless constructor (often with
        /// initialisation logic) in a partial file. The generator must not emit a default
        /// constructor for these to avoid CS0111 duplicates.
        /// </summary>
        public static readonly string[] ClassesWithHandWrittenDefaultCtor =
        {
            "NiAVObject",
            "NiNode",
            "NiGeometry",
            "NiGeometryData",
            "NiTriShapeData",
            "BSGeometry",
            "BSTriShape",
            "BSDynamicTriShape"
        };

        /// <summary>
        /// Classes to skip generation of public properties for
        /// </summary>
        public static readonly string[] ClassesWithoutProperties =
        {
            "NiGeometry",
            "NiGeometryData",
            "NiTriBasedGeomData",
            "NiTriShape",
            "NiTriShapeData",
            "NiTriStrips",
            "NiTriStripsData",
            "BSSegmentedTriShape",
            "BSTriShape",
            "BSDynamicTriShape",
            "BSSubIndexTriShape",
            "BSShaderProperty",
            "BSLightingShaderProperty",
            "BSEffectShaderProperty",
            "BSSkyShaderProperty",
            "BSWaterShaderProperty",
            "BSShaderPPLightingProperty",
            "BSShaderLightingProperty",
            "BSShaderNoLightingProperty"
        };

        /// <summary>
        /// Types that contain float values
        /// </summary>
        public static readonly string[] FloatValueTypes =
        {
            "float", "Vector3", "Vector4", "Color3", "Color4", "TexCoord", "Quaternion",
            "Matrix22", "Matrix33", "Matrix34", "Matrix44",
            "hkQuaternion", "hkMatrix3"
        };

        /// <summary>
        /// Hand-written structs that are pure value types (no reference-type members) and can be
        /// copied wholesale by plain struct assignment. Used as seeds for
        /// <see cref="ComputePureValueStructs(NifXml)"/>.
        /// </summary>
        /// <remarks>
        /// <c>BSVertexData</c> and <c>BSVertexDataSSE</c> qualify because their
        /// <c>BoneWeights</c> / <c>BoneIndices</c> members are inline-array value types
        /// (<c>BoneWeights4</c> / <c>BoneIndices4</c>) — there are no managed references left.
        /// </remarks>
        public static readonly string[] PureValueSkippedStructs =
        {
            "Vector2",
            "Vector3",
            "Vector4",
            "Quaternion",
            "BSVertexData",
            "BSVertexDataSSE"
        };

        /// <summary>
        /// Classifies every generated struct as "pure value" (safe to copy by plain assignment,
        /// requires no deep clone) or not. A struct is pure-value iff every one of its fields is
        /// itself a value type with no reference-type storage (primitives, enums, other pure-value
        /// structs, ...). Used by the clone generator to skip unnecessary per-field copies.
        /// </summary>
        public static HashSet<string> ComputePureValueStructs(NifXml nifXml)
        {
            var pure = new HashSet<string>(PureValueSkippedStructs);

            bool changed = true;
            while (changed)
            {
                changed = false;

                foreach (var s in nifXml.Structs)
                {
                    if (pure.Contains(s.Name))
                        continue;
                    if (SkippedTypes.Contains(s.Name))
                        continue; // BSVertexData / BSVertexDataSSE etc. are not pure.
                    if (s.Generic)
                        continue; // 'T' field has unknown type; keep conservative.

                    if (IsStructPureValue(nifXml, s, pure))
                    {
                        pure.Add(s.Name);
                        changed = true;
                    }
                }
            }

            return pure;
        }

        private static bool IsStructPureValue(NifXml nifXml, NifXmlStruct s, HashSet<string> pureSoFar)
        {
            foreach (var field in s.Fields)
            {
                string fieldTypeName = GetFieldTypeName(s, field, out bool isArray, out bool isRef, out bool isPtr, out bool isStringRef);

                if (isArray || isRef || isPtr || isStringRef)
                    return false;

                // Bitfields are emitted as classes.
                if (nifXml.IsBitfieldType(field))
                    return false;

                // Enums are value types.
                if (nifXml.IsEnumType(field))
                    continue;

                // Nested generated structs must themselves be pure-value.
                if (nifXml.IsStructType(field))
                {
                    if (!pureSoFar.Contains(field.Type))
                        return false;
                    continue;
                }

                // Reference-typed base mappings: string wrappers and block ref/ptr helper types.
                if (fieldTypeName == "NiStringRef" || fieldTypeName == "NiString" ||
                    fieldTypeName == "NiString1" || fieldTypeName == "NiString2" || fieldTypeName == "NiString4")
                    return false;

                if (fieldTypeName.StartsWith("NiBlockRef", StringComparison.Ordinal) ||
                    fieldTypeName.StartsWith("NiBlockPtr", StringComparison.Ordinal))
                    return false;

                if (fieldTypeName.StartsWith("List<", StringComparison.Ordinal) ||
                    fieldTypeName.EndsWith("[]", StringComparison.Ordinal))
                    return false;

                // Generic parameters: unknown, treat conservatively.
                if (fieldTypeName == "T" || fieldTypeName.Contains("<T>"))
                    return false;

                // Anything left is a base value type (primitive, System.Half, nullable bool?, etc.).
            }

            return true;
        }

        /// <summary>
        /// Classifies every generated struct as "ref-free": it contains no block refs, block ptrs
        /// or string refs, directly or through nested structs (lists of values are fine). The
        /// References / Pointers / ReferenceArrays / StringRefs getters of a block or struct skip
        /// fields of these types, which can only ever contribute nothing. Without this, e.g.
        /// <c>NiSkinPartition.References</c> walks every triangle of every partition to collect an
        /// empty list, and the getters are called for every block whenever refs are fixed up
        /// (block removal, sorting, header strings), which made them a large part of load and save
        /// time for skinned meshes.
        /// </summary>
        public static HashSet<string> ComputeRefFreeStructs(NifXml nifXml)
        {
            // Structs the generator never emits ref getters for (hand-written or base types).
            var refFree = new HashSet<string>(SkippedTypes);

            bool changed = true;
            while (changed)
            {
                changed = false;

                foreach (var s in nifXml.Structs)
                {
                    if (refFree.Contains(s.Name))
                        continue;
                    if (s.Generic)
                        continue; // 'T' field has unknown type; keep conservative.

                    if (IsStructRefFree(nifXml, s, refFree))
                    {
                        refFree.Add(s.Name);
                        changed = true;
                    }
                }
            }

            return refFree;
        }

        private static bool IsStructRefFree(NifXml nifXml, NifXmlStruct s, HashSet<string> refFreeSoFar)
        {
            foreach (var field in s.Fields)
            {
                string fieldTypeName = GetFieldTypeName(s, field, out _, out bool isRef, out bool isPtr, out bool isStringRef);

                if (isRef || isPtr || isStringRef)
                    return false;

                if (fieldTypeName == "T" || fieldTypeName.Contains("<T>"))
                    return false;

                if (nifXml.IsStructType(field) && !refFreeSoFar.Contains(field.Type))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Fields to only calculate ("calc") if value > 0
        /// </summary>
        public static readonly string[] CalcNotNullFields =
        {
            "Data Size",
            "Particle Data Size"
        };

        /// <summary>
        /// Splits a version string into four numeric values.
        /// </summary>
        /// <param name="verString">Version string</param>
        /// <returns>Tuple of major, minor, patch and intern version numbers (0, 0, 0, 0 for empty strings)</returns>
        public static (byte Major, byte Minor, byte Patch, byte Intern) VerToNumbers(string verString)
        {
            if (string.IsNullOrWhiteSpace(verString))
                return (0, 0, 0, 0);

            var verSplit = verString.Split(new[] { '.' }, 4);

            var verNumbers = Enumerable.Repeat("0", 4).ToArray();
            if (verSplit.Length > 0)
                verNumbers[0] = verSplit[0];
            if (verSplit.Length > 1)
                verNumbers[1] = verSplit[1];
            if (verSplit.Length > 2)
                verNumbers[2] = verSplit[2];
            if (verSplit.Length > 3)
                verNumbers[3] = verSplit[3];

            byte major = Convert.ToByte(verNumbers[0]);
            byte minor = Convert.ToByte(verNumbers[1]);
            byte patch = Convert.ToByte(verNumbers[2]);
            byte intern = Convert.ToByte(verNumbers[3]);
            return (major, minor, patch, intern);
        }

        /// <summary>
        /// Normalizes the name of a type.
        /// </summary>
        public static string NormalizeTypeName(string typeName)
        {
            return typeName.Replace("::", "_").Replace(":", "_");
        }

        /// <summary>
        /// Normalizes the name of a field.
        /// </summary>
        public static string NormalizeFieldName(string fieldName, string typeName, bool internalField = true)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                return fieldName;

            fieldName = fieldName.Replace("::", "_").Replace(":", "_");

            if (typeName != null && fieldName.Replace(" ", "") == typeName)
            {
                // Fix for field name conflicting with the name of the type (class, struct, ...)
                fieldName = fieldName.Replace(" ", "_");
            }

            fieldName = fieldName.Replace(" ", "");

            if (!char.IsNumber(fieldName[0]))
            {
                if (internalField)
                    fieldName = $"_{char.ToLower(fieldName[0])}{fieldName.Substring(1)}";
                else
                    fieldName = $"{char.ToUpper(fieldName[0])}{fieldName.Substring(1)}";
            }

            return fieldName;
        }

        /// <summary>
        /// Abbreviate the name of a type by taking upper-case and numeric characters only.
        /// Tries to returns an abbreviation with at least two characters. First character is always added.
        /// </summary>
        public static string AbbreviateType(string inputStr)
        {
            string abbr;
            int i;

            int indexTempl = inputStr.IndexOf('<');
            if (indexTempl != -1)
            {
                abbr = $"{inputStr.Substring(0, indexTempl)}_";
                i = indexTempl + 1;
            }
            else
            {
                abbr = inputStr[0].ToString();
                i = 1;
            }

            for (; i < inputStr.Length; i++)
            {
                if (char.IsUpper(inputStr[i]) || char.IsNumber(inputStr[i]))
                {
                    abbr += inputStr[i];
                }
            }

            if (abbr.Length == 1 && inputStr.Length > 1)
            {
                // Make sure there are at least 2 characters
                abbr += inputStr[1];
            }

            return abbr;
        }

        /// <summary>
        /// Build the full name for a field type.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <param name="field">Field</param>
        /// <param name="isArray">Returns if the type is an array</param>
        /// <param name="isRef">Returns if the type is a block reference</param>
        /// <param name="isPtr">Returns if the type is a block pointer</param>
        /// <param name="isStringRef">Returns if the type is a string ref</param>
        /// <returns>Field type name</returns>
        public static string GetFieldTypeName(INifXmlObject obj, NifXmlField field, out bool isArray, out bool isRef, out bool isPtr, out bool isStringRef)
        {
            isArray = false;
            isRef = false;
            isPtr = false;
            isStringRef = false;

            string fieldTypeName = NifXml.DoTypeMapping(NormalizeTypeName(field.Type));

            bool hasLength = !string.IsNullOrWhiteSpace(field.Length);
            bool hasFixedLength = hasLength && field.Length.All(char.IsDigit);

            if (!string.IsNullOrWhiteSpace(field.Template))
            {
                string fieldTemplate = NifXml.DoTypeMapping(NormalizeTypeName(field.Template)).Replace("#T#", "T");

                if (hasFixedLength)
                {
                    // Use arrays for fixed length
                    isArray = true;

                    if (fieldTypeName == "Ref")
                    {
                        fieldTypeName = $"NiBlockRefArray<{fieldTemplate}>";
                        isRef = true;
                    }
                    else if (fieldTypeName == "Ptr")
                    {
                        fieldTypeName = $"NiBlockPtrArray<{fieldTemplate}>";
                        isPtr = true;
                    }
                    else if (fieldTypeName == "NiFixedString")
                    {
                        fieldTypeName = "NiStringRef[]";
                        isStringRef = true;
                    }
                    else if (fieldTypeName == "SizedString")
                        fieldTypeName = "NiString4[]";
                    else if (fieldTypeName == "SizedString16")
                        fieldTypeName = "NiString2[]";
                    else
                        fieldTypeName = $"{fieldTypeName}<{fieldTemplate}>[]";
                }
                else if (hasLength)
                {
                    // Use lists for variable length
                    isArray = true;

                    if (fieldTypeName == "Ref")
                    {
                        fieldTypeName = $"NiBlockRefArray<{fieldTemplate}>";
                        isRef = true;
                    }
                    else if (fieldTypeName == "Ptr")
                    {
                        fieldTypeName = $"NiBlockPtrArray<{fieldTemplate}>";
                        isPtr = true;
                    }
                    else if (fieldTypeName == "NiFixedString")
                    {
                        fieldTypeName = "List<NiStringRef>";
                        isStringRef = true;
                    }
                    else if (fieldTypeName == "SizedString")
                        fieldTypeName = "List<NiString4>";
                    else if (fieldTypeName == "SizedString16")
                        fieldTypeName = "List<NiString2>";
                    else
                        fieldTypeName = $"List<{fieldTypeName}<{fieldTemplate}>>";
                }
                else
                {
                    if (fieldTypeName == "Ref")
                    {
                        fieldTypeName = $"NiBlockRef<{fieldTemplate}>";
                        isRef = true;
                    }
                    else if (fieldTypeName == "Ptr")
                    {
                        fieldTypeName = $"NiBlockPtr<{fieldTemplate}>";
                        isPtr = true;
                    }
                    else
                        fieldTypeName = $"{fieldTypeName}<{fieldTemplate}>";
                }
            }
            else
            {
                if (obj.Generic)
                    fieldTypeName = fieldTypeName.Replace("#T#", "T");

                if (hasFixedLength)
                {
                    isArray = true;

                    // Use arrays for fixed length
                    if (fieldTypeName == "NiFixedString")
                    {
                        fieldTypeName = "NiStringRef[]";
                        isStringRef = true;
                    }
                    else if (fieldTypeName == "SizedString")
                        fieldTypeName = "NiString4[]";
                    else if (fieldTypeName == "SizedString16")
                        fieldTypeName = "NiString2[]";
                    else
                    {
                        if (fieldTypeName == "NiStringRef")
                            isStringRef = true;
                        fieldTypeName = $"{fieldTypeName}[]";
                    }
                }
                else if (hasLength)
                {
                    isArray = true;

                    // Use lists for variable length
                    if (fieldTypeName == "NiFixedString")
                    {
                        fieldTypeName = "List<NiStringRef>";
                        isStringRef = true;
                    }
                    else if (fieldTypeName == "SizedString")
                        fieldTypeName = "List<NiString4>";
                    else if (fieldTypeName == "SizedString16")
                        fieldTypeName = "List<NiString2>";
                    else
                    {
                        if (fieldTypeName == "NiStringRef")
                            isStringRef = true;
                        fieldTypeName = $"List<{fieldTypeName}>";
                    }
                }
                else
                {
                    if (fieldTypeName == "NiFixedString")
                    {
                        fieldTypeName = "NiStringRef";
                        isStringRef = true;
                    }
                    else if (fieldTypeName == "SizedString")
                        fieldTypeName = "NiString4";
                    else if (fieldTypeName == "SizedString16")
                        fieldTypeName = "NiString2";
                    else if (fieldTypeName == "NiStringRef")
                        isStringRef = true;
                }
            }

            return fieldTypeName;
        }

        /// <summary>
        /// Get all field names in <paramref name="str"/>.
        /// </summary>
        /// <param name="str">String to find field names in</param>
        /// <param name="nifXml">XML instance</param>
        /// <param name="nifObject">Object</param>
        /// <param name="typeName">Type name of the object to check for collision</param>
        /// <returns>List of field names</returns>
        public static List<string> GetFieldNames(string str)
        {
            string pattern = @"\b[A-Za-z]+(?:\s{0,1}?\w)*";

            var matches = Regex.Matches(str, pattern);

            var fieldNames = new List<string>();
            foreach (Match match in matches)
                fieldNames.Add(match.Value);

            return fieldNames;
        }

        /// <summary>
        /// Replaces all field names in <paramref name="str"/> with their correctly formatted versions.
        /// </summary>
        /// <param name="str">String to replace field names in</param>
        /// <param name="nifXml">XML instance</param>
        /// <param name="nifObject">Object</param>
        /// <param name="typeName">Type name of the object to check for collision</param>
        /// <returns>New string</returns>
        public static string ReplaceFieldNames(string str, NifXml nifXml, INifXmlObject nifObject, string typeName)
        {
            string pattern = @"\b[A-Za-z]+(?:\s{0,1}?\w)*";

            str = Regex.Replace(str, pattern, (match) =>
            {
                string fieldName = match.ToString();

                // Only look at fields in the object's own inheritance
                var field = nifObject.Fields.FirstOrDefault(f => f.Name == fieldName);
                if (field == null)
                {
                    nifXml.IsFieldInTypeInheritance(nifObject, fieldName, out field);
                }

                if (field == null)
                    return fieldName;

                string normFieldName = NormalizeFieldName(fieldName, typeName, !nifObject.IsStruct);

                if (nifXml.IsEnumType(field))
                {
                    string enumStorageType = NifXml.DoTypeMapping(nifXml.GetEnumStorageType(field));
                    if (enumStorageType != null)
                    {
                        // Cast enum to storage type
                        normFieldName = $"({enumStorageType}){normFieldName}";
                    }
                }
                else if (nifXml.IsBitfieldType(field))
                {
                    // Get numeric value of bitfield
                    normFieldName = $"({normFieldName}?.Value ?? 0)";
                }
                else if (NifXml.DoTypeMapping(field.Type) == "NiBool")
                {
                    // Append GetValueOrDefault for nullable booleans
                    normFieldName = $"{normFieldName}.GetValueOrDefault()";
                }

                return normFieldName;
            });

            return str;
        }

        /// <summary>
        /// Builds a list of field conditions for <paramref name="field"/>.
        /// </summary>
        /// <param name="nifXml">XML instance</param>
        /// <param name="field">Field to build conditions for</param>
        /// <param name="nifObject">Object</param>
        /// <param name="typeName">Type name of the object to check for collision</param>
        /// <returns>List of conditions</returns>
        public static List<string> BuildFieldConditions(NifXml nifXml, NifXmlField field, INifXmlObject nifObject, string typeName)
        {
            // Store conditions in list
            var syncFuncConditions = new List<string>();

            if (!string.IsNullOrWhiteSpace(field.OnlyType))
            {
                syncFuncConditions.Add($"this is {NormalizeTypeName(field.OnlyType)}");
            }

            if (!string.IsNullOrWhiteSpace(field.ExcludeType))
            {
                syncFuncConditions.Add($"this is not {NormalizeTypeName(field.ExcludeType)}");
            }

            if (!string.IsNullOrWhiteSpace(field.VersionSince))
            {
                var (Major, Minor, Patch, Intern) = VerToNumbers(field.VersionSince);
                syncFuncConditions.Add($"stream.Version.FileVersion >= NiVersion.ToFile({Major}, {Minor}, {Patch}, {Intern})");
            }

            if (!string.IsNullOrWhiteSpace(field.VersionUntil))
            {
                var (Major, Minor, Patch, Intern) = VerToNumbers(field.VersionUntil);
                syncFuncConditions.Add($"stream.Version.FileVersion <= NiVersion.ToFile({Major}, {Minor}, {Patch}, {Intern})");
            }

            if (!string.IsNullOrWhiteSpace(field.VersionCondition))
            {
                string verCond = field.VersionCondition;

                // Replace tokens
                foreach (var token in nifXml.Tokens.Where(t => t.GetAttributesAsArray().Contains("vercond")).SelectMany(t => t.Entries))
                {
                    switch (token.Token)
                    {
                        case "#VER#":
                            verCond = verCond.Replace(token.Token, "stream.Version.FileVersion");
                            break;
                        case "#USER#":
                            verCond = verCond.Replace(token.Token, "stream.Version.UserVersion");
                            break;
                        case "#BSVER#":
                            verCond = verCond.Replace(token.Token, "stream.Version.StreamVersion");
                            break;
                        case "#FLT_INF#":
                            verCond = verCond.Replace(token.Token, "float.PositiveInfinity");
                            break;
                        default:
                            verCond = verCond.Replace(token.Token, token.String);
                            break;
                    }
                }

                if (verCond.Contains("stream.Version.FileVersion"))
                {
                    // Find version "a.b.c.d" pattern and replace with function
                    string pattern = @"(\d+)\.(\d+)\.(\d+)\.(\d+)";
                    string replacement = "NiVersion.ToFile($1, $2, $3, $4)";
                    verCond = Regex.Replace(verCond, pattern, replacement);
                }

                syncFuncConditions.Add($"{verCond}");
            }

            if (!string.IsNullOrWhiteSpace(field.Condition))
            {
                var replacedFieldNames = new List<string>();

                string cond = field.Condition;
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

                cond = ReplaceFieldNames(cond, nifXml, nifObject, typeName);

                if (cond.Contains("stream.Version.FileVersion"))
                {
                    // Find version "a.b.c.d" pattern and replace with function
                    string pattern = @"(\d+)\.(\d+)\.(\d+)\.(\d+)";
                    string replacement = "NiVersion.ToFile($1, $2, $3, $4)";
                    cond = Regex.Replace(cond, pattern, replacement);
                }

                syncFuncConditions.Add($"{cond}");
            }

            return syncFuncConditions;
        }

        /// <summary>
        /// Builds a condition string for field collisions (same name) to choose the right field based on conditions.
        /// </summary>
        /// <param name="nifXml">XML instance</param>
        /// <param name="fieldName">Field name</param>
        /// <param name="obj">Object</param>
        /// <param name="typeName">Type name of the object to check for collision</param>
        /// <param name="branchBody">Body to use for the condition branch</param>
        /// <param name="replaceFieldStr">Original field string for replacement</param>
        /// <param name="replaceTypeStr">Original type string for replacement</param>
        /// <param name="elseStrCmp">Original string for comparison that defines what to append lines to in an 'else' branch</param>
        /// <param name="elseValueStr">Original string for value assignment in an 'else' branch</param>
        /// <returns>Condition string</returns>
        public static string BuildFieldCollisionConditions(NifXml nifXml, string fieldName, INifXmlObject obj, string typeName, string branchBody,
            string replaceFieldStr, string replaceTypeStr = null,
            string elseStrCmp = null, string elseValueStr = null)
        {
            var fields = nifXml.GetFieldsWithName(obj, fieldName);
            if (fields.GroupBy(f => f.Type).Count() <= 1)
                return string.Empty;

            int conditionCount = 0;
            string conditions = string.Empty;

            var elseFieldNames = new HashSet<string>();

            foreach (var field in fields)
            {
                // Build conditions of field
                var fieldConditions = BuildFieldConditions(nifXml, field, obj, typeName);

                // Combine conditions with &&
                string fieldCondition = string.Empty;
                foreach (var cond in fieldConditions)
                {
                    if (string.IsNullOrWhiteSpace(fieldCondition))
                        fieldCondition += $"{cond}";
                    else
                        fieldCondition += $" &&\r\n    {cond}";
                }

                // Build final if-branch
                if (!string.IsNullOrWhiteSpace(fieldCondition))
                {
                    string normFieldName = NormalizeFieldName(field.Name, typeName, !obj.IsStruct);
                    string fieldTypeName = GetFieldTypeName(obj, field, out _, out _, out _, out _);
                    string typeAbbrev = AbbreviateType(fieldTypeName);
                    normFieldName += $"_{typeAbbrev}";

                    string branchBodyReplaced = branchBody;
                    if (replaceFieldStr != normFieldName)
                        branchBodyReplaced = branchBodyReplaced.Replace(replaceFieldStr, normFieldName);

                    if (replaceTypeStr != null)
                        branchBodyReplaced = branchBodyReplaced.Replace(replaceTypeStr, fieldTypeName);

                    if (elseStrCmp != null)
                    {
                        if (replaceFieldStr == elseStrCmp)
                        {
                            // Add replaced field name to list for assignment in 'else' branch
                            elseFieldNames.Add(normFieldName);
                        }
                        else
                        {
                            // Add else comparison field name to list for assignment in 'else' branch
                            elseFieldNames.Add(elseStrCmp);
                        }
                    }

                    string ifBranch =
                        $"if ({fieldCondition})\r\n" +
                        $"{{\r\n" +
                        $"    {branchBodyReplaced}" +
                        $"}}";

                    if (!string.IsNullOrWhiteSpace(conditions))
                        ifBranch = ifBranch.Replace("if (", "\r\nelse if (");

                    conditions += ifBranch;
                    conditionCount++;
                }
            }

            if (conditionCount == 1)
                conditions = string.Empty;

            if (elseValueStr != null && elseFieldNames.Count > 0)
            {
                string assignments = string.Empty;

                foreach (var assign in elseFieldNames)
                    assignments += $"    {assign} = {elseValueStr};\r\n";

                string elseBranch =
                    $"else\r\n" +
                    $"{{\r\n" +
                    $"{assignments}" +
                    $"}}";

                conditions += $"\r\n{elseBranch}";
            }

            return conditions + "\r\n";
        }

        /// <summary>
        /// Builds an expression that deep-clones the value of a field from a source instance
        /// into a newly constructed instance of the same object/struct.
        /// </summary>
        /// <param name="nifXml">XML instance</param>
        /// <param name="nifObject">Owning object/struct of the field</param>
        /// <param name="field">The field being cloned</param>
        /// <param name="fieldName">Emitted name of the field (e.g. <c>_skinTransform</c> for classes, <c>SkinTransform</c> for structs)</param>
        /// <param name="fieldTypeName">Full C# type name as produced by <see cref="GetFieldTypeName"/></param>
        /// <param name="isArray">Field is an array or list</param>
        /// <param name="isRef">Field is a block reference (or array thereof)</param>
        /// <param name="isPtr">Field is a block pointer (or array thereof)</param>
        /// <param name="isStringRef">Field is a string reference (or array thereof)</param>
        /// <param name="otherPrefix">Prefix for the source instance expression (e.g. <c>other.</c> or <c>this.</c>)</param>
        /// <returns>C# expression producing the deep-cloned field value</returns>
        public static string BuildFieldCloneExpression(
            NifXml nifXml, INifXmlObject nifObject, NifXmlField field,
            string fieldName, string fieldTypeName,
            bool isArray, bool isRef, bool isPtr, bool isStringRef,
            string otherPrefix,
            HashSet<string> pureValueStructs = null)
        {
            string otherExpr = $"{otherPrefix}{fieldName}";

            // Block refs/ptrs (single or array); all expose a covariant Clone() returning the same type.
            if (isRef || isPtr)
                return $"{otherExpr}?.Clone()";

            // String references
            if (isStringRef)
            {
                if (!isArray)
                    return $"{otherExpr}?.Clone()";
                if (fieldTypeName.EndsWith("[]"))
                    return $"{otherExpr} == null ? null : System.Array.ConvertAll({otherExpr}, e => e?.Clone())";
                // List<NiStringRef>
                return $"{otherExpr}?.ConvertAll(e => e?.Clone())";
            }

            // Enum (value type)
            if (nifXml.IsEnumType(field))
            {
                if (!isArray)
                    return otherExpr;
                if (fieldTypeName.EndsWith("[]"))
                    return $"({fieldTypeName}){otherExpr}?.Clone()";
                return $"{otherExpr} == null ? null : new {fieldTypeName}({otherExpr})";
            }

            // Bitfield (generated partial class with a Value storage property)
            if (nifXml.IsBitfieldType(field))
            {
                if (!isArray)
                    return $"{otherExpr} == null ? null : new {fieldTypeName}({otherExpr}.Value)";

                string elem = StripCollectionSuffix(fieldTypeName);
                if (fieldTypeName.EndsWith("[]"))
                    return $"{otherExpr} == null ? null : System.Array.ConvertAll({otherExpr}, e => e == null ? null : new {elem}(e.Value))";
                return $"{otherExpr}?.ConvertAll(e => e == null ? null : new {elem}(e.Value))";
            }

            // Sized string wrappers (NiString1/2/4)
            if (fieldTypeName == "NiString1" || fieldTypeName == "NiString2" || fieldTypeName == "NiString4")
                return $"({fieldTypeName}){otherExpr}?.Clone()";

            if (fieldTypeName.EndsWith("[]") &&
                (fieldTypeName == "NiString1[]" || fieldTypeName == "NiString2[]" || fieldTypeName == "NiString4[]"))
            {
                string elem = fieldTypeName.Substring(0, fieldTypeName.Length - 2);
                return $"{otherExpr} == null ? null : System.Array.ConvertAll({otherExpr}, e => ({elem})e?.Clone())";
            }

            if (fieldTypeName == "List<NiString1>" || fieldTypeName == "List<NiString2>" || fieldTypeName == "List<NiString4>")
            {
                string elem = fieldTypeName.Substring(5, fieldTypeName.Length - 6);
                return $"{otherExpr}?.ConvertAll(e => ({elem})e?.Clone())";
            }

            // Generated structs (includes skipped structs seeded as pure-value in
            // <see cref="PureValueSkippedStructs"/>, such as BSVertexData / BSVertexDataSSE).
            if (nifXml.IsStructType(field))
            {
                // Skipped value-type compounds like Vector2/3/4, Quaternion and the inline-array
                // based BSVertexData(SSE) have no DeepClone() and are safely copied via plain
                // value assignment.
                bool isValueTypeBase = SkippedTypes.Contains(field.Type);

                // Pure-value structs (no reference-type members anywhere) can be copied by
                // plain struct assignment — no DeepClone() call or per-field work required.
                bool isPureValue = pureValueStructs != null && pureValueStructs.Contains(field.Type);

                if (isValueTypeBase || isPureValue)
                {
                    if (!isArray)
                        return otherExpr;
                    if (fieldTypeName.EndsWith("[]"))
                        return $"({fieldTypeName}){otherExpr}?.Clone()";
                    return $"{otherExpr} == null ? null : new {fieldTypeName}({otherExpr})";
                }

                if (!isArray)
                    return $"{otherExpr}.DeepClone()";
                if (fieldTypeName.EndsWith("[]"))
                    return $"{otherExpr} == null ? null : System.Array.ConvertAll({otherExpr}, e => e.DeepClone())";
                return $"{otherExpr}?.ConvertAll(e => e.DeepClone())";
            }

            // Generic 'T' in generic generated types (structs like Key<T>, KeyGroup<T>, ...).
            // No generic constraint is available, so fall back to direct/shallow assignment.
            // Hand-written OnDeepClone partial methods can override this behaviour.
            if (nifObject.Generic && (fieldTypeName == "T" || fieldTypeName.Contains("<T>") || fieldTypeName.EndsWith("T[]") || fieldTypeName.EndsWith("T>")))
            {
                if (!isArray)
                    return otherExpr;
                if (fieldTypeName.EndsWith("[]"))
                    return $"({fieldTypeName}){otherExpr}?.Clone()";
                return $"{otherExpr} == null ? null : new {fieldTypeName}({otherExpr})";
            }

            // Arrays/lists of value/base types (float[], byte[], List<ushort>, List<Vector3>, ...)
            if (isArray)
            {
                if (fieldTypeName.EndsWith("[]"))
                    return $"({fieldTypeName}){otherExpr}?.Clone()";
                return $"{otherExpr} == null ? null : new {fieldTypeName}({otherExpr})";
            }

            // Default: primitive, base-type struct (Vector3, Quaternion), string, nullable bool, ...
            return otherExpr;
        }

        private static string StripCollectionSuffix(string typeName)
        {
            if (typeName.EndsWith("[]"))
                return typeName.Substring(0, typeName.Length - 2);
            if (typeName.StartsWith("List<") && typeName.EndsWith(">"))
                return typeName.Substring(5, typeName.Length - 6);
            return typeName;
        }
    }
}
