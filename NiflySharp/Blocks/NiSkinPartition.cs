using NiflySharp.Extensions;
using NiflySharp.Stream;
using NiflySharp.Structs;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NiflySharp.Blocks
{
    public partial class NiSkinPartition
    {
        /// <summary>
        /// MappedIndices is not in the file; it is calculated from
        /// the file version.  If true, the vertex indices in triangles
        /// and strips are indices into the vertex map, not the shape's vertices.
        /// TrianglesCopy always uses indices into the shape's vertex list.
        /// </summary>
        internal bool mappedIndices = true;

        /// <summary>
        /// TriParts is not in the file; it is generated as needed.
        /// If not empty, its size should match the shape's triangle list.
        /// It gives the partition index (into "partitions") of each triangle.
        /// Whenever triParts is changed so it's not in sync with trueTriangles,
        /// GenerateTrueTrianglesFromTriParts should be called to get them back in sync.
        /// </summary>
        internal List<int> triParts = [];

        public void SetVertexData(List<BSVertexDataSSE> vertexData)
        {
            if (vertexData.Count > ushort.MaxValue)
                return;

            _vertexData = _vertexData.Resize(vertexData.Count);
            vertexData.CopyTo(CollectionsMarshal.AsSpan(_vertexData));
        }

        public bool ConvertStripsToTriangles()
        {
            var spanPartitions = CollectionsMarshal.AsSpan(_partitions);

            bool triangulated = false;
            foreach (ref var part in spanPartitions)
            {
                if (part.ConvertStripsToTriangles())
                    triangulated = true;
            }
            return triangulated;
        }

        public new void BeforeSync(NiStreamReversible stream)
        {
            if (stream.CurrentMode == NiStreamReversible.Mode.Write)
            {
                if (stream.Version.StreamVersion == 100)
                {
                    _vertexSize = (uint)(((_vertexDesc?.Value ?? 0) & 0xF) * 4);
                    _dataSize = (uint)((_vertexData?.Count ?? 0) * _vertexSize);
                }
                else
                {
                    _vertexSize = 0;
                    _dataSize = 0;
                }
            }
        }

        public void PrepareTrueTriangles()
        {
            var spanPartitions = CollectionsMarshal.AsSpan(_partitions);

            foreach (ref var part in spanPartitions)
            {
                if ((part.TrianglesCopy?.Count ?? 0) > 0)
                    continue;

                if (part.NumStrips > 0)
                    part.ConvertStripsToTriangles();

                if (mappedIndices)
                {
                    part.GenerateTrueTrianglesFromMappedTriangles();
                }
                else
                {
                    part.TrianglesCopy = part.TrianglesCopy.Resize(part.Triangles.Count);
                    part.Triangles.CopyTo(CollectionsMarshal.AsSpan(part.TrianglesCopy));
                }
            }
        }

        public void GenerateTriPartsFromTrueTriangles(IList<Triangle> triangles)
        {
            triParts.Clear();
            triParts.Resize(triangles.Count);

            // Make a map from triangles to their indices in 'triangles'
            var shapeTriInds = new Dictionary<Triangle, int>();

            for (int triInd = 0; triInd < triangles.Count; ++triInd)
            {
                var tri = triangles[triInd];
                tri.Rotate();
                shapeTriInds[tri] = triInd;
            }

            // Set triParts for each partition triangle
            for (int partInd = 0; partInd < _partitions.Count; ++partInd)
            {
                foreach (var pt in _partitions[partInd].TrianglesCopy)
                {
                    var tri = pt;
                    tri.Rotate();

                    if (shapeTriInds.TryGetValue(tri, out int ind))
                        triParts[ind] = partInd;
                }
            }
        }

        public void PrepareVertexMapsAndTriangles()
        {
            var spanPartitions = CollectionsMarshal.AsSpan(_partitions);

            foreach (ref var part in spanPartitions)
            {
                if ((part.VertexMap?.Count ?? 0) == 0)
                    part.GenerateVertexMapFromTrueTriangles();

                if ((part.Triangles?.Count ?? 0) == 0)
                {
                    if (mappedIndices)
                    {
                        part.GenerateMappedTrianglesFromTrueTrianglesAndVertexMap();
                    }
                    else
                    {
                        part.Triangles = part.Triangles.Resize(part.TrianglesCopy.Count);
                        part.TrianglesCopy.CopyTo(CollectionsMarshal.AsSpan(part.Triangles));
                    }
                }
            }
        }

        public void PrepareTriParts(IList<Triangle> tris)
        {
            if (tris.Count == triParts.Count)
                return; // already prepared

            PrepareTrueTriangles();
            GenerateTriPartsFromTrueTriangles(tris);
        }

        public void GenerateTrueTrianglesFromTriParts(IList<Triangle> tris)
        {
            if (tris.Count != triParts.Count)
                return;

            var spanPartitions = CollectionsMarshal.AsSpan(_partitions);

            foreach (ref var p in spanPartitions)
            {
                p.TrianglesCopy?.Clear();
                p.Triangles?.Clear();
                p.NumStrips = 0;
                p.Strips?.Clear();
                p.StripLengths?.Clear();
                p.HasFaces = true;
                p.VertexMap?.Clear();
                p.VertexWeights?.Clear();
                p.BoneIndices?.Clear();
            }

            for (int triInd = 0; triInd < tris.Count; ++triInd)
            {
                int partInd = triParts[triInd];
                if (partInd >= 0 && partInd < _partitions.Count)
                {
                    spanPartitions[partInd].TrianglesCopy ??= [];
                    spanPartitions[partInd].TrianglesCopy.Add(tris[triInd]);
                }
            }

            foreach (ref var p in spanPartitions)
                p.NumTriangles = (ushort)p.TrianglesCopy.Count;
        }
    }
}
