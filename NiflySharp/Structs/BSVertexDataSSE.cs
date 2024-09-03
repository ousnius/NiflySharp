using NiflySharp.Stream;
using System;
using System.Numerics;

namespace NiflySharp.Structs
{
    /// <summary>
    /// Full vertex data structure for SSE.
    /// Fields are used depending on the vertex description.
    /// Byte fields for normal, tangent and bitangent map [0, 255] to [-1, 1].
    /// </summary>
    public struct BSVertexDataSSE : INiStreamable
    {
        public Vector3 Vertex;

        public float BitangentX;

        internal uint UnusedW;

        public HalfTexCoord UV;

        public ByteVector3 Normal;

        public sbyte BitangentY;

        public ByteVector3 Tangent;

        public sbyte BitangentZ;

        public ByteColor4 VertexColors;

        public Half[] BoneWeights;

        public byte[] BoneIndices;

        public float EyeData;

        public BSVertexDataSSE() { }

        public void Sync(NiStreamReversible stream)
        {
            if ((Convert.ToInt32(stream.Argument) & 0x1) != 0)
            {
                stream.Sync(ref Vertex);
            }

            if ((Convert.ToInt32(stream.Argument) & 0x11) == 0x11)
            {
                stream.Sync(ref BitangentX);
            }

            if ((Convert.ToInt32(stream.Argument) & 0x11) == 0x1)
            {
                stream.Sync(ref UnusedW);
            }

            if ((Convert.ToInt32(stream.Argument) & 0x2) != 0)
            {
                stream.Sync(ref UV);
            }

            if ((Convert.ToInt32(stream.Argument) & 0x8) != 0)
            {
                stream.Sync(ref Normal);
                stream.Sync(ref BitangentY);
            }

            if ((Convert.ToInt32(stream.Argument) & 0x18) == 0x18)
            {
                stream.Sync(ref Tangent);
                stream.Sync(ref BitangentZ);
            }

            if ((Convert.ToInt32(stream.Argument) & 0x20) != 0)
            {
                stream.Sync(ref VertexColors);
            }

            if ((Convert.ToInt32(stream.Argument) & 0x40) != 0)
            {
                stream.InitArraySize(ref BoneWeights, 4);
                stream.SyncArrayContent(BoneWeights);
                stream.InitArraySize(ref BoneIndices, 4);
                stream.SyncArrayContent(BoneIndices);
            }

            if ((Convert.ToInt32(stream.Argument) & 0x100) != 0)
            {
                stream.Sync(ref EyeData);
            }
        }
    }
}
