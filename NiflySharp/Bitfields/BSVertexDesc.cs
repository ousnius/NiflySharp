using System;

namespace NiflySharp.Bitfields
{
    public partial class BSVertexDesc
    {
        public const ulong DESC_MASK_VERT = 0xFFFFFFFFFFFFFFF0;
        public const ulong DESC_MASK_UVS = 0xFFFFFFFFFFFFFF0F;
        public const ulong DESC_MASK_NBT = 0xFFFFFFFFFFFFF0FF;
        public const ulong DESC_MASK_SKCOL = 0xFFFFFFFFFFFF0FFF;
        public const ulong DESC_MASK_DATA = 0xFFFFFFFFFFF0FFFF;
        public const ulong DESC_MASK_OFFSET = 0xFFFFFF0000000000;
        public const ulong DESC_MASK_FLAGS = ~DESC_MASK_OFFSET;

        public void ClearAttributeOffsets()
        {
            Value &= DESC_MASK_OFFSET;
        }
    }
}
