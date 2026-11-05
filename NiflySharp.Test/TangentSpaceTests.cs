using NiflySharp.Blocks;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;

namespace NiflySharp.Test
{
    public class TangentSpaceTests
    {
        const string AssetsDirectory = "Assets";

        private static bool IsFinite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);

        [Fact(DisplayName = "CalcTangentSpace (BSTriShape) stays finite with a zero-area triangle")]
        public void BSTriShape_DegenerateTriangle_NoNaN()
        {
            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/Static.nif"));
            var shape = nif.Blocks.OfType<BSTriShape>().First(s => s.HasNormals && s.HasUVs && s.VertexDataSSE != null);

            // Collapse one triangle to a point: its tangent direction is the zero vector.
            var tri = shape.Triangles[0];
            var vertices = shape.VertexDataSSE;
            var collapsed = vertices[tri.V2];
            collapsed.Vertex = vertices[tri.V1].Vertex;
            vertices[tri.V2] = collapsed;
            collapsed = vertices[tri.V3];
            collapsed.Vertex = vertices[tri.V1].Vertex;
            vertices[tri.V3] = collapsed;

            shape.CalcTangentSpace();

            Assert.All(shape.VertexDataSSE, v => Assert.True(float.IsFinite(v.BitangentX)));
            Assert.All(shape.Tangents, t => Assert.True(IsFinite(t)));
            Assert.All(shape.Bitangents, b => Assert.True(IsFinite(b)));
        }

        [Fact(DisplayName = "CalcTangentSpace (NiTriShapeData) stays finite with a zero-area triangle")]
        public void NiTriShapeData_DegenerateTriangle_NoNaN()
        {
            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.0.0.5/11/11/Skinned.nif"));
            var data = nif.Blocks.OfType<NiTriShapeData>()
                .First(d => d.Normals?.Count > 0 && d.Triangles?.Count > 0 && d.UVSets?.Count > 0);

            var tri = data.Triangles[0];
            var vertices = data.Vertices;
            vertices[tri.V2] = vertices[tri.V1];
            vertices[tri.V3] = vertices[tri.V1];

            data.CalcTangentSpace();

            Assert.All(data.Tangents, t => Assert.True(IsFinite(t)));
            Assert.All(data.Bitangents, b => Assert.True(IsFinite(b)));
        }
    }
}
