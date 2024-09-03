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

        public new void CalcTangentSpace()
        {
            if (!HasNormals || !HasUVs)
                return;

            base.CalcTangentSpace();

            var tan1 = new List<Vector3>();
            var tan2 = new List<Vector3>();
            tan1.Resize(_numVertices);
            tan2.Resize(_numVertices);

            foreach (var triangle in _triangles)
            {
                int i1 = triangle._v1;
                int i2 = triangle._v2;
                int i3 = triangle._v3;

                if (i1 >= _numVertices || i2 >= _numVertices || i3 >= _numVertices)
                    continue;

                Vector3 v1 = _vertices[i1];
                Vector3 v2 = _vertices[i2];
                Vector3 v3 = _vertices[i3];

                // Use first set of UVs
                TexCoord w1 = _uVSets[i1];
                TexCoord w2 = _uVSets[i2];
                TexCoord w3 = _uVSets[i3];

                float x1 = v2.X - v1.X;
                float x2 = v3.X - v1.X;
                float y1 = v2.Y - v1.Y;
                float y2 = v3.Y - v1.Y;
                float z1 = v2.Z - v1.Z;
                float z2 = v3.Z - v1.Z;

                float s1 = w2._u - w1._u;
                float s2 = w3._u - w1._u;
                float t1 = w2._v - w1._v;
                float t2 = w3._v - w1._v;

                float r = s1 * t2 - s2 * t1;
                r = r >= 0.0f ? +1.0f : -1.0f;

                var sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                var tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                sdir = Vector3.Normalize(sdir);
                tdir = Vector3.Normalize(tdir);

                tan1[i1] += tdir;
                tan1[i2] += tdir;
                tan1[i3] += tdir;

                tan2[i1] += sdir;
                tan2[i2] += sdir;
                tan2[i3] += sdir;
            }

            _tangents = _tangents.Resize(_numVertices);
            _bitangents = _bitangents.Resize(_numVertices);

            var normalsSpan = CollectionsMarshal.AsSpan(_normals);
            var tangentsSpan = CollectionsMarshal.AsSpan(_tangents);
            var bitangentsSpan = CollectionsMarshal.AsSpan(_bitangents);

            for (ushort i = 0; i < _numVertices; i++)
            {
                tangentsSpan[i] = tan1[i];
                bitangentsSpan[i] = tan2[i];

                if (tangentsSpan[i] == Vector3.Zero || bitangentsSpan[i] == Vector3.Zero)
                {
                    tangentsSpan[i].X = normalsSpan[i].Y;
                    tangentsSpan[i].Y = normalsSpan[i].Z;
                    tangentsSpan[i].Z = normalsSpan[i].X;
                    bitangentsSpan[i] = Vector3.Cross(normalsSpan[i], tangentsSpan[i]);
                }
                else
                {
                    tangentsSpan[i] = Vector3.Normalize(tangentsSpan[i]);
                    tangentsSpan[i] = tangentsSpan[i] - normalsSpan[i] * Vector3.Dot(normalsSpan[i], tangentsSpan[i]);
                    tangentsSpan[i] = Vector3.Normalize(tangentsSpan[i]);

                    bitangentsSpan[i] = Vector3.Normalize(bitangentsSpan[i]);

                    bitangentsSpan[i] = bitangentsSpan[i] - normalsSpan[i] * Vector3.Dot(normalsSpan[i], bitangentsSpan[i]);
                    bitangentsSpan[i] = bitangentsSpan[i] - tangentsSpan[i] * Vector3.Dot(tangentsSpan[i], bitangentsSpan[i]);

                    bitangentsSpan[i] = Vector3.Normalize(bitangentsSpan[i]);
                }
            }
        }
    }
}
