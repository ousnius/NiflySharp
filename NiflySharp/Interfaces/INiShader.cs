using NiflySharp.Blocks;
using NiflySharp.Enums;
using NiflySharp.Structs;
using System.Collections.Generic;
using System.Numerics;
using static NiflySharp.Helpers.ShaderHelper;

namespace NiflySharp
{
    public interface INiShader : INiStreamable, INiObject
    {
        public ShaderGameType Type { get; set; }

        /// <summary>
        /// Shader type for FO3/NV
        /// </summary>
        BSShaderType ShaderType_FO3_NV { get => 0; set { } }

        /// <summary>
        /// Shader type for Skyrim and Fallout 4
        /// </summary>
        BSLightingShaderType ShaderType_SK_FO4 { get => 0; set { } }

        /// <summary>
        /// Shader type for Fallout 76 and Starfield
        /// </summary>
        BSShaderType155 ShaderType_FO76_SF { get => 0; set { } }

        /// <summary>
        /// Legacy Bethesda
        /// </summary>
        BSShaderFlags ShaderFlags { get => 0; set { } }

        /// <summary>
        /// Legacy Bethesda
        /// </summary>
        BSShaderFlags2 ShaderFlags2 { get => 0; set { } }

        /// <summary>
        /// Used in Skyrim
        /// </summary>
        SkyrimShaderPropertyFlags1 ShaderFlags_SSPF1 { get => 0; set { } }

        /// <summary>
        /// Used in Skyrim
        /// </summary>
        SkyrimShaderPropertyFlags2 ShaderFlags_SSPF2 { get => 0; set { } }

        /// <summary>
        /// Used in Fallout 4
        /// </summary>
        Fallout4ShaderPropertyFlags1 ShaderFlags_F4SPF1 { get => 0; set { } }

        /// <summary>
        /// Used in Fallout 4
        /// </summary>
        Fallout4ShaderPropertyFlags2 ShaderFlags_F4SPF2 { get => 0; set { } }

        /// <summary>
        /// Used in Fallout 76 and later
        /// </summary>
        List<BSShaderCRC32> ShaderFlagsList1 { get => null; set { } }

        /// <summary>
        /// Used in Fallout 76 and later
        /// </summary>
        List<BSShaderCRC32> ShaderFlagsList2 { get => null; set { } }

        bool HasTextureSet => false;
        NiBlockRef<BSShaderTextureSet> TextureSetRef => null;

        bool IsTypeDefault { get; }
        bool IsTypeTallGrass { get; }
        bool IsTypeWater { get; }
        bool IsTypeLighting30 { get; }
        bool IsTypeTiled { get; }
        bool IsTypeNoLighting { get; }
        bool IsTypeEnvironmentMap { get; }
        bool IsTypeGlow { get; }
        bool IsTypeParallax  { get; }
        bool IsTypeFaceTint  { get; }
        bool IsTypeSkinTint { get; }
        bool IsTypeHairTint { get; }
        bool IsTypeParallaxOcclusion { get; }
        bool IsTypeMultitextureLandscape { get; }
        bool IsTypeLODLandscape { get; }
        bool IsTypeSnow { get; }
        bool IsTypeMultiLayerParallax { get; }
        bool IsTypeTreeAnim { get; }
        bool IsTypeLODObjects { get; }
        bool IsTypeSparkleSnow { get; }
        bool IsTypeLODObjectsHD { get; }
        bool IsTypeEyeEnvironmentMap { get; }
        bool IsTypeCloud { get; }
        bool IsTypeLODLandscapeNoise { get; }
        bool IsTypeMultitextureLandscapeLODBlend { get; }
        bool IsTypeFO4Dismemberment { get; }
        bool IsTypeTerrain { get; }

        /// <summary>
        /// Required for skinned meshes.
        /// </summary>
        bool Skinned { get; set; }

        /// <summary>
        /// Double-sided/two-sided rendering.
        /// </summary>
        bool DoubleSided { get; set; }

        /// <summary>
        /// Parallax flag (unused in vanilla game, used by mods)
        /// </summary>
        bool Parallax { get; set; }

        /// <summary>
        /// Use model space normals and an external specular map.
        /// </summary>
        bool ModelSpace { get; set; }

        /// <summary>
        /// Provides its own emittance color. (will not absorb light/ambient color?)
        /// </summary>
        bool Emissive { get; set; }

        /// <summary>
        /// Enables specularity.
        /// </summary>
        bool HasSpecular { get; set; }

        /// <summary>
        /// Enables rendering of/has vertex colors.
        /// </summary>
        bool HasVertexColors { get; set; }

        /// <summary>
        /// Enables using alpha component of vertex colors.
        /// </summary>
        bool HasVertexAlpha { get; set; }

        /// <summary>
        /// Use soft lighting map.
        /// </summary>
        bool HasSoftlight { get; set; }

        /// <summary>
        /// Use back lighting map.
        /// </summary>
        bool HasBacklight { get; set; }

        /// <summary>
        /// Use rim lighting map.
        /// </summary>
        bool HasRimlight { get; set; }

        /// <summary>
        /// Use glow map.
        /// </summary>
        bool HasGlowmap { get; set; }

        /// <summary>
        /// Use greyscale to palette color in BSEffectShaderProperty.
        /// </summary>
        bool HasGreyscaleToPaletteColor { get; set; }

        /// <summary>
        /// Use greyscale to palette alpha in BSEffectShaderProperty.
        /// </summary>
        bool HasGreyscaleToPaletteAlpha { get; set; }

        /// <summary>
        /// Environment mapping (uses environment map scale).
        /// </summary>
        bool HasEnvironmentMapping { get; set; }

        /// <summary>
        /// Environment mapping for eyes.
        /// </summary>
        bool HasEyeEnvironmentMapping { get; set; }

        /// <summary>
        /// Use external emittance.
        /// </summary>
        bool HasExternalEmittance { get; set; }

        /// <summary>
        /// Tree animations flag.
        /// </summary>
        bool HasTreeAnim { get; set; }

        Vector2 UVOffset => Vector2.Zero;
        Vector2 UVScale => Vector2.One;
        Vector3 SpecularColor => Vector3.Zero;
        float SpecularStrength => 0.0f;
        float Glossiness => 0.0f;
        float EnvironmentMapScale => 0.0f;
        Color4 EmissiveColor => Color4.Zero;
        float EmissiveMultiple => 0.0f;
        float Alpha => 1.0f;
        float BacklightPower => 0.0f;
        float RimlightPower => 2.0f;
        float Softlight => 0.3f;
        float SubsurfaceRolloff => 0.3f;
        float GrayscaleToPaletteScale => 1.0f;
        float FresnelPower => 5.0f;
        string WetMaterialName => null;
    }
}
