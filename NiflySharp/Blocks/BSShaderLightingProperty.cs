using NiflySharp.Enums;

namespace NiflySharp.Blocks
{
    public partial class BSShaderLightingProperty : BSShaderProperty
    {
        public TexClampMode TextureClampMode { get => _textureClampMode; set => _textureClampMode = value; }
    }
}
