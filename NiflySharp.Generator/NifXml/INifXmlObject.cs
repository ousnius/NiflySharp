using System.Collections.Generic;

namespace NiflySharp.Generator
{
    /// <summary>
    /// Interface for common properties of objects and structs.
    /// </summary>
    public interface INifXmlObject
    {
        string Name { get; set; }

        string Module { get; set; }

        string Inherit { get; set; }

        string StopCondition { get; set; }

        bool Generic { get; set; }

        string Comment { get; set; }

        List<NifXmlField> Fields { get; set; }

        bool Abstract { get; set; }

        bool IsStruct { get; set; }
    }
}
