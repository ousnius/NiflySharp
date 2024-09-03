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
            return _vertexWeights?.SplitByFixedSize(_numWeightsPerVertex).ToList();
        }

        public readonly List<List<byte>> GetVertexBoneIndices()
        {
            return _boneIndices?.SplitByFixedSize(_numWeightsPerVertex).ToList();
        }

        public readonly List<List<ushort>> GetStripsLists()
        {
            return _strips?.SplitByFlexSize(_stripLengths).ToList();
        }

        public bool ConvertStripsToTriangles()
        {
            if (_numStrips == 0)
                return false;

            _hasFaces = true;
            _triangles = IndicesHelper.GenerateTrianglesFromStrips(GetStripsLists());
            _numTriangles = (ushort)_triangles.Count;
            _numStrips = 0;
            _strips?.Clear();
            _stripLengths?.Clear();
            _trianglesCopy?.Clear();
            return true;
        }

        public void GenerateTrueTrianglesFromMappedTriangles()
        {
            if ((_vertexMap?.Count ?? 0) == 0 || (_triangles?.Count ?? 0) == 0)
            {
                _trianglesCopy ??= [];
                _trianglesCopy.Clear();
                if (_numStrips == 0)
                    _numTriangles = 0;
                return;
            }

            _trianglesCopy = _trianglesCopy.Resize(_triangles.Count);
            _triangles.CopyTo(CollectionsMarshal.AsSpan(_trianglesCopy));

            IndicesHelper.ApplyMapToTriangles(ref _trianglesCopy, _vertexMap, out _);

            foreach (var t in _trianglesCopy)
                t.Rotate();

            if (_triangles.Count != _trianglesCopy.Count)
            {
                _triangles.Clear();
                _numTriangles = (ushort)_trianglesCopy.Count;
            }
        }

        public void GenerateMappedTrianglesFromTrueTrianglesAndVertexMap()
        {
            if (_vertexMap?.Count == 0 || _trianglesCopy?.Count == 0)
            {
                _triangles ??= [];
                _triangles.Clear();
                if (_numStrips == 0)
                    _numTriangles = 0;
                return;
            }

            var invmap = new List<ushort>();
            for (ushort mi = 0; mi < _vertexMap.Count; ++mi)
            {
                if (_vertexMap[mi] >= invmap.Count)
                    invmap.Resize(_vertexMap[mi] + 1);

                invmap[_vertexMap[mi]] = mi;
            }

            _triangles = _triangles.Resize(_trianglesCopy.Count);
            _trianglesCopy.CopyTo(CollectionsMarshal.AsSpan(_triangles));

            IndicesHelper.ApplyMapToTriangles(ref _triangles, invmap, out _);

            foreach (var tri in _triangles)
                tri.Rotate();

            if (_triangles.Count != _trianglesCopy.Count)
            {
                _trianglesCopy.Clear();
                _numTriangles = (ushort)_triangles.Count;
            }
        }

        public void GenerateVertexMapFromTrueTriangles()
        {
            ushort maxInd = IndicesHelper.CalcMaxTriangleIndex(_trianglesCopy);

            var vertUsed = new List<bool>(maxInd + 1);
            vertUsed.Resize(maxInd + 1);

            foreach (var trueTriangle in _trianglesCopy)
            {
                vertUsed[trueTriangle._v1] = true;
                vertUsed[trueTriangle._v2] = true;
                vertUsed[trueTriangle._v3] = true;
            }

            _vertexMap ??= [];
            _vertexMap.Clear();

            for (ushort i = 0; i < vertUsed.Count; ++i)
            {
                if (vertUsed[i])
                    _vertexMap.Add(i);
            }

            _numVertices = (ushort)_vertexMap.Count;
        }
    }
}
