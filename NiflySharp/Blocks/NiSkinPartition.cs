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
                if ((part._trianglesCopy?.Count ?? 0) > 0)
                    continue;

                if (part._numStrips > 0)
                    part.ConvertStripsToTriangles();

                if (mappedIndices)
                {
                    part.GenerateTrueTrianglesFromMappedTriangles();
                }
                else
                {
                    part._trianglesCopy = part._trianglesCopy.Resize(part._triangles.Count);
                    part._triangles.CopyTo(CollectionsMarshal.AsSpan(part._trianglesCopy));
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
                foreach (var pt in _partitions[partInd]._trianglesCopy)
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
                if ((part._vertexMap?.Count ?? 0) == 0)
                    part.GenerateVertexMapFromTrueTriangles();

                if ((part._triangles?.Count ?? 0) == 0)
                {
                    if (mappedIndices)
                    {
                        part.GenerateMappedTrianglesFromTrueTrianglesAndVertexMap();
                    }
                    else
                    {
                        part._triangles = part._triangles.Resize(part._trianglesCopy.Count);
                        part._trianglesCopy.CopyTo(CollectionsMarshal.AsSpan(part._triangles));
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
                p._trianglesCopy?.Clear();
                p._triangles?.Clear();
                p._numStrips = 0;
                p._strips?.Clear();
                p._stripLengths?.Clear();
                p._hasFaces = true;
                p._vertexMap?.Clear();
                p._vertexWeights?.Clear();
                p._boneIndices?.Clear();
            }

            for (int triInd = 0; triInd < tris.Count; ++triInd)
            {
                int partInd = triParts[triInd];
                if (partInd >= 0 && partInd < _partitions.Count)
                {
                    spanPartitions[partInd]._trianglesCopy ??= [];
                    spanPartitions[partInd]._trianglesCopy.Add(tris[triInd]);
                }
            }

            foreach (ref var p in spanPartitions)
                p._numTriangles = (ushort)p._trianglesCopy.Count;
        }
    }
}
