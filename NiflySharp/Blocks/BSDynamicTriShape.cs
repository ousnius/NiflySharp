using NiflySharp.Enums;
using NiflySharp.Extensions;
using NiflySharp.Stream;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace NiflySharp.Blocks
{
    public partial class BSDynamicTriShape
    {
        public List<Vector4> Vertices { get => _vertices; set => _vertices = value; }

        public BSDynamicTriShape() : base()
        {
            _vertexDesc.VertexAttributes &= ~VertexAttribute.Vertex;
            _vertexDesc.VertexAttributes |= VertexAttribute.Full_Precision;
        }

        public new void BeforeSync(NiStreamReversible stream)
        {
            if (stream.CurrentMode == NiStreamReversible.Mode.Write)
            {
                _dynamicDataSize = (uint)(_numVertices * 16);
            }
        }

        public void CalcDynamicData()
        {
            _dynamicDataSize = (uint)_numVertices * 16;

            _vertices = _vertices.Resize(_numVertices);

            var spanDynamicData = CollectionsMarshal.AsSpan(_vertices);

            if (_vertexData_List_BSVD != null)
            {
                var spanVertexData = CollectionsMarshal.AsSpan(_vertexData_List_BSVD);

                for (int i = 0; i < _numVertices; i++)
                {
                    ref var dynamicData = ref spanDynamicData[i];
                    ref var vertData = ref spanVertexData[i];

                    if (IsFullPrecision)
                    {
                        dynamicData.X = vertData._vertex_V3.X;
                        dynamicData.Y = vertData._vertex_V3.Y;
                        dynamicData.Z = vertData._vertex_V3.Z;
                        dynamicData.W = vertData.BitangentX_fl;
                    }
                    else
                    {
                        dynamicData.X = (float)vertData._vertex_HV3._x;
                        dynamicData.Y = (float)vertData._vertex_HV3._y;
                        dynamicData.Z = (float)vertData._vertex_HV3._z;
                        dynamicData.W = (float)vertData.BitangentX_SH;
                    }

                    if (dynamicData.X > 0.0f)
                        vertData._eyeData = 1.0f;
                    else
                        vertData._eyeData = 0.0f;
                }
            }
            else if (_vertexData_List_BSVDSSE != null)
            {
                var spanVertexData = CollectionsMarshal.AsSpan(_vertexData_List_BSVDSSE);

                for (int i = 0; i < _numVertices; i++)
                {
                    ref var dynamicData = ref spanDynamicData[i];
                    ref var vertData = ref spanVertexData[i];

                    dynamicData.X = vertData._vertex.X;
                    dynamicData.Y = vertData._vertex.Y;
                    dynamicData.Z = vertData._vertex.Z;
                    dynamicData.W = vertData._bitangentX;

                    if (dynamicData.X > 0.0f)
                        vertData._eyeData = 1.0f;
                    else
                        vertData._eyeData = 0.0f;
                }
            }
        }
    }
}
