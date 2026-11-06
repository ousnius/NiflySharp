using NiflySharp.Extensions;
using NiflySharp.Stream;
using NiflySharp.Structs;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace NiflySharp.Blocks
{
    public partial class NiTriBasedGeomData
    {
        public override int NumTriangles => _numTriangles;

        public new void AfterSync(NiStreamReversible stream)
        {
            if (stream.CurrentMode == NiStreamReversible.Mode.Read)
            {
                if (_numTriangles > 0 && this is NiTriShapeData ntsd)
                {
                    ntsd.Triangles = ntsd.Triangles.Resize(_numTriangles);
                }
            }
        }

        internal void Create(List<Vector3> vertexPositions, List<Triangle> triangles, List<TexCoord> uvs, List<Vector3> normals)
        {
            Create(vertexPositions, uvs, normals);

            if (triangles == null || _numVertices == 0)
            {
                _numTriangles = 0;
                return;
            }

            if (triangles != null)
            {
                if (triangles.Count > ushort.MaxValue)
                    _numTriangles = ushort.MaxValue;
                else
                    _numTriangles = (ushort)triangles.Count;
            }
        }

        /// <summary>
        /// Calculates tangents and bitangents from the triangles <paramref name="tris"/> and the first UV set.
        /// Shared by <see cref="NiTriShapeData"/> and <see cref="NiTriStripsData"/>, which only differ in where the triangles come from.
        /// </summary>
        /// <param name="tris">Triangles (may be null)</param>
        protected void CalcTangentSpaceFromTriangles(List<Triangle> tris)
        {
            if (!HasNormals || !HasUVs)
                return;

            base.CalcTangentSpace();

            var tan1 = new List<Vector3>();
            var tan2 = new List<Vector3>();
            tan1.Resize(_numVertices);
            tan2.Resize(_numVertices);

            foreach (var triangle in tris ?? [])
            {
                int i1 = triangle.V1;
                int i2 = triangle.V2;
                int i3 = triangle.V3;

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

                float s1 = w2.U - w1.U;
                float s2 = w3.U - w1.U;
                float t1 = w2.V - w1.V;
                float t2 = w3.V - w1.V;

                float r = s1 * t2 - s2 * t1;
                r = r >= 0.0f ? +1.0f : -1.0f;

                var sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                var tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                sdir = sdir.NormalizeOrZero();
                tdir = tdir.NormalizeOrZero();

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
                    tangentsSpan[i] = tangentsSpan[i].NormalizeOrZero();
                    tangentsSpan[i] = tangentsSpan[i] - normalsSpan[i] * Vector3.Dot(normalsSpan[i], tangentsSpan[i]);
                    tangentsSpan[i] = tangentsSpan[i].NormalizeOrZero();

                    bitangentsSpan[i] = bitangentsSpan[i].NormalizeOrZero();

                    bitangentsSpan[i] = bitangentsSpan[i] - normalsSpan[i] * Vector3.Dot(normalsSpan[i], bitangentsSpan[i]);
                    bitangentsSpan[i] = bitangentsSpan[i] - tangentsSpan[i] * Vector3.Dot(tangentsSpan[i], bitangentsSpan[i]);

                    bitangentsSpan[i] = bitangentsSpan[i].NormalizeOrZero();
                }
            }
        }
    }
}
