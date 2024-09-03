using NiflySharp.Structs;
using System;
using System.Collections.Generic;

namespace NiflySharp.Blocks
{
    public partial class NiGeometry : INiShape
    {
        /// <summary>
        /// Used for internal reference. Not in file.
        /// </summary>
        public NiGeometryData GeometryData { get; set; }

        public bool HasData => !DataRef?.IsEmpty() ?? false;
        public NiBlockRef<NiGeometryData> DataRef { get => _data; set => _data = value; }

        public bool HasSkinInstance => !SkinInstanceRef?.IsEmpty() ?? false;
        INiRef INiShape.SkinInstanceRef { get => _skinInstance; set => throw new NotSupportedException("Ref can only be set using the explicit block ref type."); }
        public NiBlockRef<NiSkinInstance> SkinInstanceRef { get => _skinInstance; set => _skinInstance = value; }

        public bool HasShaderProperty => !ShaderPropertyRef?.IsEmpty() ?? false;
        INiRef INiShape.ShaderPropertyRef { get => _shaderProperty; set => throw new NotSupportedException("Ref can only be set using the explicit block ref type."); }
        public NiBlockRef<BSShaderProperty> ShaderPropertyRef { get => _shaderProperty; set => _shaderProperty = value; }

        public bool HasAlphaProperty => !AlphaPropertyRef?.IsEmpty() ?? false;
        public NiBlockRef<NiAlphaProperty> AlphaPropertyRef { get => _alphaProperty; set => _alphaProperty = value; }

        public BoundingSphere Bounds
        {
            get => GeometryData?.Bounds ?? new BoundingSphere();
            set
            {
                if (GeometryData != null)
                    GeometryData.Bounds = value;
            }
        }

        public void UpdateBounds()
        {
            GeometryData?.UpdateBounds();
        }

        public bool HasVertices
        {
            get
            {
                return GeometryData?.HasVertices ?? false;
            }
            set
            {
                if (GeometryData != null)
                    GeometryData.HasVertices = value;
            }
        }

        public bool HasUVs
        {
            get
            {
                return GeometryData?.HasUVs ?? false;
            }
            set
            {
                if (GeometryData != null)
                    GeometryData.HasUVs = true;
            }
        }

        public bool HasNormals
        {
            get
            {
                return GeometryData?.HasNormals ?? false;
            }
            set
            {
                if (GeometryData != null)
                    GeometryData.HasNormals = value;
            }
        }

        public bool HasTangents
        {
            get
            {
                return GeometryData?.HasTangents ?? false;
            }
            set
            {
                if (GeometryData != null)
                    GeometryData.HasTangents = true;
            }
        }

        public bool HasVertexColors
        {
            get
            {
                return GeometryData?.HasVertexColors ?? false;
            }
            set
            {
                if (GeometryData != null)
                    GeometryData.HasVertexColors = value;
            }
        }

        public bool IsSkinned
        {
            get => HasSkinInstance;
            set { }
        }

        public bool HasSecondUVs { get => false; set { } }
        public bool HasEyeData { get => false; set { } }
        public bool CanChangePrecision => false;
        public bool IsFullPrecision { get => true; set { } }

        public ushort VertexCount => GeometryData?.NumVertices ?? 0;
        public int TriangleCount => GeometryData?.NumTriangles ?? 0;

        public List<Triangle> Triangles => GeometryData?.Triangles;

        public NiGeometry()
        {
            _materialData._materialNeedsUpdate = false;
        }

        public void SetTriangles(NiVersion version, List<Triangle> triangles)
        {
            if (GeometryData != null)
                GeometryData.Triangles = triangles;
        }

        /* FIXME
        public bool ReorderTriangles(IList<uint> triInds)
        {
            // TODO
            throw new System.NotImplementedException();

	        //std::vector<Triangle> trisOrdered;
	        //std::vector<Triangle> tris;
	        //if (!GetTriangles(tris))
		    //    return false;
            //
	        //if (tris.size() != triInds.size())
		    //    return false;
            //
	        //for (uint32_t id : triInds)
		    //    if (id < tris.size())
			//        trisOrdered.push_back(tris[id]);
            //
	        //if (trisOrdered.size() != tris.size())
		    //    return false;
            //
	        //SetTriangles(trisOrdered);
	        //return true;
        }
        */
    }
}
