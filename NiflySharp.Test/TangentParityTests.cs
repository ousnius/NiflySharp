using NiflySharp.Blocks;
using NiflySharp.Structs;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;

namespace NiflySharp.Test
{
    public class TangentParityTests
    {
        const string AssetsDirectory = "Assets";

        // Quad as one strip: triangles (0, 1, 2) and (1, 3, 2)
        private static readonly List<Vector3> QuadVertices = [new(0, 0, 0), new(1, 0, 0), new(0, 1, 0), new(1, 1, 0)];
        private static readonly List<Vector3> QuadNormals = [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ];
        private static readonly List<TexCoord> QuadUVs = [new(0, 0), new(1, 0), new(0, 1), new(1, 1)];

        private class TestTriStripsData : NiTriStripsData
        {
            public void SetStrips(List<ushort> points, List<ushort> stripLengths)
            {
                _points = points;
                _stripLengths = stripLengths;
                _numStrips = (ushort)stripLengths.Count;
            }
        }

        private static List<Vector3> Zeros(int count) => Enumerable.Repeat(Vector3.Zero, count).ToList();

        [Fact(DisplayName = "CalcTangentSpace dispatches to NiTriShapeData through a NiGeometryData reference")]
        public void CalcTangentSpace_ThroughBaseType_UsesNiTriShapeData()
        {
            var data = new NiTriShapeData(QuadVertices, [new(0, 1, 2), new(1, 3, 2)], QuadUVs, QuadNormals);
            var expectedTangents = data.Tangents.ToList();
            var expectedBitangents = data.Bitangents.ToList();
            Assert.All(expectedTangents, t => Assert.NotEqual(Vector3.Zero, t));

            data.Tangents = Zeros(QuadVertices.Count);
            data.Bitangents = Zeros(QuadVertices.Count);

            // Called through the base type on purpose: a hiding ("new") method would only set the flag here
            NiGeometryData baseData = data;
            baseData.CalcTangentSpace();

            Assert.Equal(expectedTangents, data.Tangents);
            Assert.Equal(expectedBitangents, data.Bitangents);
        }

        [Fact(DisplayName = "NiTriStripsData.CalcTangentSpace matches NiTriShapeData with the strip triangles")]
        public void NiTriStripsData_CalcTangentSpace_MatchesTriShape()
        {
            var strips = new TestTriStripsData
            {
                Vertices = [.. QuadVertices],
                Normals = [.. QuadNormals],
                UVSets = [.. QuadUVs]
            };
            strips.SetStrips([0, 1, 2, 3], [4]);

            var tris = strips.StripsToTris();
            Assert.Equal(new[] { new Triangle(0, 1, 2), new Triangle(1, 3, 2) }, tris);

            NiGeometryData baseData = strips;
            baseData.CalcTangentSpace();

            var shapeData = new NiTriShapeData(QuadVertices, tris, QuadUVs, QuadNormals);
            Assert.Equal(shapeData.Tangents, strips.Tangents);
            Assert.Equal(shapeData.Bitangents, strips.Bitangents);
            Assert.True(strips.HasTangents);
        }

        [Fact(DisplayName = "NifFile.CalcTangentsForShape recalculates geometry data and BSTriShape tangents")]
        public void CalcTangentsForShape_RecalculatesBothShapeKinds()
        {
            var oldNif = new NifFile();
            Assert.Equal(0, oldNif.Load($"{AssetsDirectory}/V20.0.0.5/11/11/Skinned.nif"));
            var niTriShape = oldNif.GetShapes().OfType<NiTriShape>()
                .First(s => oldNif.GetBlock(s.DataRef) is NiTriShapeData d && d.HasNormals && d.HasUVs && d.Triangles?.Count > 0);
            var data = (NiTriShapeData)oldNif.GetBlock(niTriShape.DataRef);

            data.CalcTangentSpace();
            var expectedTangents = data.Tangents.ToList();
            data.Tangents = Zeros(data.Vertices.Count);
            Assert.NotEqual(expectedTangents, data.Tangents);
            oldNif.CalcTangentsForShape(niTriShape);
            Assert.Equal(expectedTangents, data.Tangents);

            var sseNif = new NifFile();
            Assert.Equal(0, sseNif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/Static.nif"));
            var bsTriShape = sseNif.GetShapes().OfType<BSTriShape>().First(s => s.HasNormals && s.HasUVs && s.VertexDataSSE != null);

            bsTriShape.CalcTangentSpace();
            var expectedPacked = bsTriShape.VertexDataSSE.Select(v => v.Tangent).ToList();
            bsTriShape.SetTangents(Zeros(bsTriShape.VertexCount));
            Assert.NotEqual(expectedPacked, bsTriShape.VertexDataSSE.Select(v => v.Tangent));
            sseNif.CalcTangentsForShape(bsTriShape);
            Assert.Equal(expectedPacked, bsTriShape.VertexDataSSE.Select(v => v.Tangent));
        }

        [Fact(DisplayName = "BSTriShape packs tangents rounding halves away from zero like nifly")]
        public void SetTangents_RoundsHalfAwayFromZero()
        {
            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/Static.nif"));
            var shape = nif.GetShapes().OfType<BSTriShape>().First(s => s.VertexDataSSE != null);

            // (t + 1) / 2 * 255 is exactly 64.5: banker's rounding gives 64, std::round (nifly) gives 65
            float t = (2 * 64 + 1) / 255.0f - 1.0f;
            Assert.Equal(64.5f, (t + 1.0f) / 2.0f * 255.0f);

            shape.SetTangents(Enumerable.Repeat(new Vector3(t, t, t), shape.VertexCount).ToList());

            var packed = shape.VertexDataSSE[0].Tangent;
            Assert.Equal(65, (byte)packed.X);
            Assert.Equal(65, (byte)packed.Y);
            Assert.Equal(65, (byte)packed.Z);
        }
    }
}
