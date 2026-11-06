using NiflySharp.Extensions;
using NiflySharp.Helpers;
using NiflySharp.Structs;
using System.Collections.Generic;
using System.Linq;

namespace NiflySharp.Blocks
{
    public partial class NiTriStripsData
    {
        /// <summary>
        /// Generates triangles from the triangle strips.
        /// Strips with less than 3 points and degenerate triangles are skipped.
        /// </summary>
        /// <returns>Triangles of all strips</returns>
        public List<Triangle> StripsToTris()
        {
            if (_points == null || _stripLengths == null)
                return [];

            return IndicesHelper.GenerateTrianglesFromStrips(_points.SplitByFlexSize(_stripLengths).ToList());
        }

        public override void CalcTangentSpace()
        {
            if (!HasNormals || !HasUVs)
                return;

            CalcTangentSpaceFromTriangles(StripsToTris());
        }
    }
}
