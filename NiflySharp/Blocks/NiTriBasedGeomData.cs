using NiflySharp.Extensions;
using NiflySharp.Stream;
using NiflySharp.Structs;
using System.Collections.Generic;
using System.Numerics;

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
    }
}
