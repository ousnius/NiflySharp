using NiflySharp.Stream;
using System;
using System.Numerics;

namespace NiflySharp.Structs
{
    /// <summary>
    /// Full vertex data structure.
    /// Fields are used depending on the vertex description.
    /// Byte fields for normal, tangent and bitangent map [0, 255] to [-1, 1].
    /// </summary>
    public struct BSVertexData : INiStreamable
    {
        public Vector3 Vertex;

        public float BitangentX;

        internal uint UnusedW;

        public HalfVector3 VertexHalf;

        public Half BitangentXHalf;

        internal ushort UnusedWShort;

        public HalfTexCoord UV;

        public ByteVector3 Normal;

        public sbyte BitangentY;

        public ByteVector3 Tangent;

        public sbyte BitangentZ;

        public ByteColor4 VertexColors;

        public BoneWeights4 BoneWeights;

        public BoneIndices4 BoneIndices;

        public float EyeData;

        public BSVertexData() { }

        public void Sync(NiStreamReversible stream)
        {
            // The vertex attribute flags, converted once per vertex rather than once per attribute.
            int vertexFlags = Convert.ToInt32(stream.Argument);

            if ((vertexFlags & 0x401) == 0x401)
            {
                stream.Sync(ref Vertex);
            }

            if ((vertexFlags & 0x411) == 0x411)
            {
                stream.Sync(ref BitangentX);
            }

            if ((vertexFlags & 0x411) == 0x401)
            {
                stream.Sync(ref UnusedW);
            }

            if ((vertexFlags & 0x401) == 0x1)
            {
                stream.Sync(ref VertexHalf);
            }

            if ((vertexFlags & 0x411) == 0x11)
            {
                stream.Sync(ref BitangentXHalf);
            }

            if ((vertexFlags & 0x411) == 0x1)
            {
                stream.Sync(ref UnusedWShort);
            }

            if ((vertexFlags & 0x2) != 0)
            {
                stream.Sync(ref UV);
            }

            if ((vertexFlags & 0x8) != 0)
            {
                stream.Sync(ref Normal);
                stream.Sync(ref BitangentY);
            }

            if ((vertexFlags & 0x18) == 0x18)
            {
                stream.Sync(ref Tangent);
                stream.Sync(ref BitangentZ);
            }

            if ((vertexFlags & 0x20) != 0)
            {
                stream.Sync(ref VertexColors);
            }

            if ((vertexFlags & 0x40) != 0)
            {
                stream.Sync(ref BoneWeights[0]);
                stream.Sync(ref BoneWeights[1]);
                stream.Sync(ref BoneWeights[2]);
                stream.Sync(ref BoneWeights[3]);
                stream.Sync(ref BoneIndices[0]);
                stream.Sync(ref BoneIndices[1]);
                stream.Sync(ref BoneIndices[2]);
                stream.Sync(ref BoneIndices[3]);
            }

            if ((vertexFlags & 0x100) != 0)
            {
                stream.Sync(ref EyeData);
            }
        }
    }
}
