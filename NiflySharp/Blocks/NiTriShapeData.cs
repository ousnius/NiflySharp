using NiflySharp.Extensions;
using NiflySharp.Structs;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace NiflySharp.Blocks
{
    public partial class NiTriShapeData
    {
        public override List<Triangle> Triangles
        {
            get => _triangles;
            set
            {
                int count = value.Count;
                if (count > ushort.MaxValue)
                    _triangles = value.Take(ushort.MaxValue).ToList();
                else
                    _triangles = value;

                _hasTriangles = count > 0;
                _numTriangles = (ushort)_triangles.Count;
                _numTrianglePoints = (uint)(_numTriangles * 3);
            }
        }

        public NiTriShapeData()
        {
        }

        public NiTriShapeData(List<Vector3> vertices, List<Triangle> triangles, List<TexCoord> uvSets, List<Vector3> normals) : this()
        {
            Create(vertices, triangles, uvSets, normals);
        }

        public new void Create(List<Vector3> vertexPositions, List<Triangle> triangles, List<TexCoord> uvs, List<Vector3> normals)
        {
            base.Create(vertexPositions, triangles, uvs, normals);

            if (_numTriangles > 0)
            {
                _numTrianglePoints = (uint)_numTriangles * 3;
                _hasTriangles = true;
            }
            else
            {
                _numTrianglePoints = 0;
                _hasTriangles = false;
            }

            if (triangles != null)
            {
                _triangles = _triangles.Resize(_numTriangles);
                var trianglesSpan = CollectionsMarshal.AsSpan(_triangles);

                for (ushort t = 0; t < _numTriangles; t++)
                    trianglesSpan[t] = triangles[t];
            }

            _numMatchGroups = 0;

            // Calculate again, now with triangles
            CalcTangentSpace();
        }

        public override void CalcTangentSpace()
        {
            CalcTangentSpaceFromTriangles(_triangles);
        }
    }
}
