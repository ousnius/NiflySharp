using NiflySharp.Structs;

namespace NiflySharp.Blocks
{
    public partial class BSShaderPPLightingProperty : BSShaderLightingProperty, INiShader
    {
        public bool HasTextureSet => !_textureSet?.IsEmpty() ?? false;
        public NiBlockRef<BSShaderTextureSet> TextureSetRef => _textureSet;

        public Color4 EmissiveColor { get => _emissiveColor; set => _emissiveColor = value; }
    }
}
