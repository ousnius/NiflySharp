using NiflySharp.Extensions;
using NiflySharp.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace NiflySharp.Structs
{
    public partial struct SkinPartition
    {
        public readonly List<List<float>> GetVertexWeights()
        {
            return VertexWeights?.SplitByFixedSize(NumWeightsPerVertex).ToList();
        }

        public readonly List<List<byte>> GetVertexBoneIndices()
        {
            return BoneIndices?.SplitByFixedSize(NumWeightsPerVertex).ToList();
        }

        public readonly List<List<ushort>> GetStripsLists()
        {
            return Strips?.SplitByFlexSize(StripLengths).ToList();
        }

        public bool ConvertStripsToTriangles()
        {
            if (NumStrips == 0)
                return false;

            HasFaces = true;
            Triangles = IndicesHelper.GenerateTrianglesFromStrips(GetStripsLists());
            NumTriangles = (ushort)Triangles.Count;
            NumStrips = 0;
            Strips?.Clear();
            StripLengths?.Clear();
            TrianglesCopy?.Clear();
            return true;
        }

        public void GenerateTrueTrianglesFromMappedTriangles()
        {
            if ((VertexMap?.Count ?? 0) == 0 || (Triangles?.Count ?? 0) == 0)
            {
                TrianglesCopy ??= [];
                TrianglesCopy.Clear();
                if (NumStrips == 0)
                    NumTriangles = 0;
                return;
            }

            TrianglesCopy = TrianglesCopy.Resize(Triangles.Count);
            Triangles.CopyTo(CollectionsMarshal.AsSpan(TrianglesCopy));

            IndicesHelper.ApplyMapToTriangles(ref TrianglesCopy, VertexMap, out _);

            foreach (var t in TrianglesCopy)
                t.Rotate();

            if (Triangles.Count != TrianglesCopy.Count)
            {
                Triangles.Clear();
                NumTriangles = (ushort)TrianglesCopy.Count;
            }
        }

        public void GenerateMappedTrianglesFromTrueTrianglesAndVertexMap()
        {
            if (VertexMap?.Count == 0 || TrianglesCopy?.Count == 0)
            {
                Triangles ??= [];
                Triangles.Clear();
                if (NumStrips == 0)
                    NumTriangles = 0;
                return;
            }

            var invmap = new List<ushort>();
            for (ushort mi = 0; mi < VertexMap.Count; ++mi)
            {
                if (VertexMap[mi] >= invmap.Count)
                    invmap.Resize(VertexMap[mi] + 1);

                invmap[VertexMap[mi]] = mi;
            }

            Triangles = Triangles.Resize(TrianglesCopy.Count);
            TrianglesCopy.CopyTo(CollectionsMarshal.AsSpan(Triangles));

            IndicesHelper.ApplyMapToTriangles(ref Triangles, invmap, out _);

            foreach (var tri in Triangles)
                tri.Rotate();

            if (Triangles.Count != TrianglesCopy.Count)
            {
                TrianglesCopy.Clear();
                NumTriangles = (ushort)Triangles.Count;
            }
        }

        public void GenerateVertexMapFromTrueTriangles()
        {
            ushort maxInd = IndicesHelper.CalcMaxTriangleIndex(TrianglesCopy);

            var vertUsed = new List<bool>(maxInd + 1);
            vertUsed.Resize(maxInd + 1);

            foreach (var trueTriangle in TrianglesCopy)
            {
                vertUsed[trueTriangle.V1] = true;
                vertUsed[trueTriangle.V2] = true;
                vertUsed[trueTriangle.V3] = true;
            }

            VertexMap ??= [];
            VertexMap.Clear();

            for (ushort i = 0; i < vertUsed.Count; ++i)
            {
                if (vertUsed[i])
                    VertexMap.Add(i);
            }

            NumVertices = (ushort)VertexMap.Count;
        }
    }
}
