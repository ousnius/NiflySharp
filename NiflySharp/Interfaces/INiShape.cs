using NiflySharp.Blocks;
using NiflySharp.Structs;
using System.Collections.Generic;
using System.Numerics;

namespace NiflySharp
{
    public interface INiShape : INiStreamable, INiObject
    {
        NiStringRef Name { get; set; }

        NiBlockRef<NiTimeController> Controller { get; set; }
        NiBlockRef<NiCollisionObject> CollisionObject { get; set; }
        NiBlockRefArray<NiExtraData> ExtraDataList { get; set; }

        // <summary>
        // Basic flags for AV objects. For Bethesda streams above 26 only.
        // ALL: FO4 lacks the 0x80000 flag always. Skyrim lacks it sometimes.
        // BSTreeNode: 0x8080E (pre-FO4), 0x400E (FO4)
        // BSLeafAnimNode: 0x808000E (pre-FO4), 0x500E (FO4)
        // BSDamageStage, BSBlastNode: 0x8000F (pre-FO4), 0x2000000F (FO4)
        // </summary>
        uint Flags_ui { get; set; }

        // <summary>
        // Basic flags for AV objects.
        // </summary>
        ushort Flags_us { get; set; }

        // <summary>
        // The translation vector.
        // </summary>
        Vector3 Translation { get; set; }

        // <summary>
        // The rotation part of the transformation matrix.
        // </summary>
        Matrix33 Rotation { get; set; }

        // <summary>
        // Scaling part (only uniform scaling is supported).
        // </summary>
        float Scale { get; set; }

        NiGeometryData GeometryData { get; set; }

        bool HasData { get; }
        NiBlockRef<NiGeometryData> DataRef { get; set; }

        bool HasSkinInstance { get; }
        INiRef SkinInstanceRef { get; set; } // INiSKin

        bool HasShaderProperty { get; }
        INiRef ShaderPropertyRef { get; set; } // INiShader

        bool HasAlphaProperty { get; }
        NiBlockRef<NiAlphaProperty> AlphaPropertyRef { get; set; }

        NiBlockRefArray<NiProperty> Properties { get; set; }

        bool HasVertices { get; set; }
        bool HasUVs { get; set; }
        bool HasSecondUVs { get; set; }
        bool HasNormals { get; set; }
        bool HasTangents { get; set; }
        bool HasVertexColors { get; set; }
        bool IsSkinned { get; set; }
        bool HasEyeData { get; set; }
        bool CanChangePrecision { get; }
        bool IsFullPrecision { get; set; }

        ushort VertexCount { get; }
        int TriangleCount { get; }
        List<Triangle> Triangles { get; }
        void SetTriangles(NiVersion version, List<Triangle> triangles);

        BoundingSphere Bounds { get; set; }
        void UpdateBounds();

        /* FIXME
        bool ReorderTriangles(IList<uint> triInds);
        */
    }
}
