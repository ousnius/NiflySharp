using NiflySharp.Enums;
using NiflySharp.Stream;
using NiflySharp.Structs;
using System.Collections.Generic;
using static NiflySharp.Helpers.ShaderHelper;

namespace NiflySharp.Blocks
{
    public partial class BSLightingShaderProperty : BSShaderProperty, INiShader
    {
        public bool HasTextureSet => !_textureSet?.IsEmpty() ?? false;
        public NiBlockRef<BSShaderTextureSet> TextureSetRef => _textureSet;

        public BSShaderType155 ShaderType_FO76_SF { get => _shaderType_BSST155; set => _shaderType_BSST155 = value; }

        /// <summary>
        /// Used in Skyrim
        /// </summary>
        public SkyrimShaderPropertyFlags1 ShaderFlags_SSPF1
        {
            get => _shaderFlags1_SSPF1;
            set => _shaderFlags1_SSPF1 = value;
        }
        /// <summary>
        /// Used in Skyrim
        /// </summary>
        public SkyrimShaderPropertyFlags2 ShaderFlags_SSPF2
        {
            get => _shaderFlags2_SSPF2;
            set => _shaderFlags2_SSPF2 = value;
        }

        /// <summary>
        /// Used in Fallout 4
        /// </summary>
        public Fallout4ShaderPropertyFlags1 ShaderFlags_F4SPF1
        {
            get => _shaderFlags1_F4SPF1;
            set => _shaderFlags1_F4SPF1 = value;
        }
        /// <summary>
        /// Used in Fallout 4
        /// </summary>
        public Fallout4ShaderPropertyFlags2 ShaderFlags_F4SPF2
        {
            get => _shaderFlags2_F4SPF2;
            set => _shaderFlags2_F4SPF2 = value;
        }

        /// <summary>
        /// Used in Fallout 76 and later
        /// </summary>
        public List<BSShaderCRC32> ShaderFlagsList1
        {
            get => _sF1;
            set => _sF1 = value;
        }
        /// <summary>
        /// Used in Fallout 76 and later
        /// </summary>
        public List<BSShaderCRC32> ShaderFlagsList2
        {
            get => _sF2;
            set => _sF2 = value;
        }

        public TexCoord UVOffset { get => _uVOffset; set => _uVOffset = value; }
        public TexCoord UVScale { get => _uVScale; set => _uVScale = value; }

        public Color4 EmissiveColor
        {
            get => new(_emissiveColor.R, _emissiveColor.G, _emissiveColor.B, 0.0f);
            set => _emissiveColor = new Color3(value.R, value.G, value.B);
        }

        public new void BeforeSync(NiStreamReversible stream)
        {
            _shaderFlags = 0;
            _shaderFlags2 = 0;

            if (stream.Version.StreamVersion < 130)
            {
                Type = ShaderGameType.SK;
                _shaderFlags1_F4SPF1 = 0;
                _shaderFlags2_F4SPF2 = 0;
            }

            if (stream.Version.StreamVersion >= 130)
            {
                Type = ShaderGameType.FO4;
                _shaderFlags1_SSPF1 = 0;
                _shaderFlags2_SSPF2 = 0;

                if (stream.Version.StreamVersion == 155)
                {
                    Type = ShaderGameType.FO76SF;
                    _shaderFlags1_F4SPF1 = 0;
                    _shaderFlags2_F4SPF2 = 0;
                }
            }

            _shaderType_BSST155 = (BSShaderType155)_shaderType;
        }
    }
}
