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

            var trianglesCopySpan = CollectionsMarshal.AsSpan(TrianglesCopy);
            for (int i = 0; i < trianglesCopySpan.Length; i++)
                trianglesCopySpan[i].Rotate();

            if (Triangles.Count != TrianglesCopy.Count)
            {
                Triangles.Clear();
                NumTriangles = (ushort)TrianglesCopy.Count;
            }
        }

        public void GenerateMappedTrianglesFromTrueTrianglesAndVertexMap()
        {
            if ((VertexMap?.Count ?? 0) == 0 || (TrianglesCopy?.Count ?? 0) == 0)
            {
                Triangles ??= [];
                Triangles.Clear();
                if (NumStrips == 0)
                    NumTriangles = 0;
                return;
            }

            // Sized once: growing it per vertex reallocates the whole list each time (Resize sets an exact capacity)
            int maxMapped = -1;
            foreach (ushort mapped in CollectionsMarshal.AsSpan(VertexMap))
                maxMapped = System.Math.Max(maxMapped, mapped);

            var invmap = new List<int>(maxMapped + 1);
            invmap.Resize(maxMapped + 1);
            var invmapSpan = CollectionsMarshal.AsSpan(invmap);
            var vertexMapSpan = CollectionsMarshal.AsSpan(VertexMap);
            for (int mi = 0; mi < vertexMapSpan.Length; ++mi)
                invmapSpan[vertexMapSpan[mi]] = (ushort)mi;

            Triangles = Triangles.Resize(TrianglesCopy.Count);
            TrianglesCopy.CopyTo(CollectionsMarshal.AsSpan(Triangles));

            IndicesHelper.ApplyMapToTriangles(ref Triangles, invmap, out _);

            var trianglesSpan = CollectionsMarshal.AsSpan(Triangles);
            for (int i = 0; i < trianglesSpan.Length; i++)
                trianglesSpan[i].Rotate();

            if (Triangles.Count != TrianglesCopy.Count)
            {
                TrianglesCopy.Clear();
                NumTriangles = (ushort)Triangles.Count;
            }
        }

        public void GenerateVertexMapFromTrueTriangles()
        {
            VertexMap ??= [];
            VertexMap.Clear();

            if ((TrianglesCopy?.Count ?? 0) == 0)
            {
                NumVertices = 0;
                return;
            }

            ushort maxInd = IndicesHelper.CalcMaxTriangleIndex(TrianglesCopy);

            var vertUsed = new List<bool>(maxInd + 1);
            vertUsed.Resize(maxInd + 1);

            foreach (var trueTriangle in TrianglesCopy)
            {
                vertUsed[trueTriangle.V1] = true;
                vertUsed[trueTriangle.V2] = true;
                vertUsed[trueTriangle.V3] = true;
            }

            // 'int' counter: with maxInd == ushort.MaxValue, a ushort counter would wrap and never terminate
            for (int i = 0; i < vertUsed.Count; ++i)
            {
                if (vertUsed[i])
                    VertexMap.Add((ushort)i);
            }

            NumVertices = (ushort)VertexMap.Count;
        }
    }
}
