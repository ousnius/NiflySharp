using NiflySharp.Enums;
using System.Linq;

namespace NiflySharp.Helpers
{
    public static class ShaderHelper
    {
        public enum ShaderGameType
        {
            None = 0,
            FO3NV,
            SK,
            FO4,
            FO76SF
        }

        #region Shader Types
        public static bool DefaultType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.FO3NV => shader.ShaderType_FO3_NV == BSShaderType.SHADER_DEFAULT,
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.Default,
            ShaderGameType.FO76SF => shader.ShaderType_FO76_SF == BSShaderType155.Default,
            _ => false
        };

        public static bool TallGrassType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.FO3NV => shader.ShaderType_FO3_NV == BSShaderType.SHADER_TALL_GRASS,
            _ => false
        };

        public static bool SkyType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.FO3NV => shader.ShaderType_FO3_NV == BSShaderType.SHADER_SKY,
            _ => false
        };

        public static bool WaterType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.FO3NV => shader.ShaderType_FO3_NV == BSShaderType.SHADER_WATER,
            _ => false
        };

        public static bool Lighting30Type(INiShader shader) => shader.Type switch
        {
            ShaderGameType.FO3NV => shader.ShaderType_FO3_NV == BSShaderType.SHADER_LIGHTING30,
            _ => false
        };

        public static bool TiledType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.FO3NV => shader.ShaderType_FO3_NV == BSShaderType.SHADER_TILE,
            _ => false
        };

        public static bool NoLightingType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.FO3NV => shader.ShaderType_FO3_NV == BSShaderType.SHADER_NOLIGHTING,
            _ => false
        };

        public static bool EnvironmentMapType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.EnvironmentMap,
            _ => false
        };

        public static bool GlowType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.GlowShader,
            ShaderGameType.FO76SF => shader.ShaderType_FO76_SF == BSShaderType155.Glow,
            _ => false
        };

        public static bool ParallaxType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.Parallax,
            _ => false
        };

        public static bool FaceTintType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.FaceTint,
            ShaderGameType.FO76SF => shader.ShaderType_FO76_SF == BSShaderType155.FaceTint,
            _ => false
        };

        public static bool SkinTintType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.FO3NV => shader.ShaderType_FO3_NV == BSShaderType.SHADER_SKIN,
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.SkinTint,
            ShaderGameType.FO76SF => shader.ShaderType_FO76_SF == BSShaderType155.SkinTint,
            _ => false
        };

        public static bool HairTintType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.HairTint,
            ShaderGameType.FO76SF => shader.ShaderType_FO76_SF == BSShaderType155.HairTint,
            _ => false
        };

        public static bool ParallaxOcclusionType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.ParallaxOcc,
            _ => false
        };

        public static bool MultitextureLandscapeType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.MultitextureLandscape,
            _ => false
        };

        public static bool LODLandscapeType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.LODLandscape,
            _ => false
        };

        public static bool SnowType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.Snow,
            _ => false
        };

        public static bool MultiLayerParallaxType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.MultiLayerParallax,
            _ => false
        };

        public static bool TreeAnimType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.TreeAnim,
            _ => false
        };

        public static bool LODObjectsType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.LODObjects,
            _ => false
        };

        public static bool SparkleSnowType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.SparkleSnow,
            _ => false
        };

        public static bool LODObjectsHDType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.LODObjectsHD,
            _ => false
        };

        public static bool EyeEnvironmentMapType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.EyeEnvmap,
            ShaderGameType.FO76SF => shader.ShaderType_FO76_SF == BSShaderType155.EyeEnvmap,
            _ => false
        };

        public static bool CloudType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.Cloud,
            _ => false
        };

        public static bool LODLandscapeNoiseType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.LODLandscapeNoise,
            _ => false
        };

        public static bool MultitextureLandscapeLODBlendType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.MultitextureLandscapeLODBlend,
            _ => false
        };

        public static bool FO4DismembermentType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK or ShaderGameType.FO4 => shader.ShaderType_SK_FO4 == BSLightingShaderType.FO4Dismemberment,
            _ => false
        };

        public static bool TerrainType(INiShader shader) => shader.Type switch
        {
            ShaderGameType.FO76SF => shader.ShaderType_FO76_SF == BSShaderType155.Terrain,
            _ => false
        };
        #endregion

        #region Shader Flags Functions
        public static bool HasFlagSF1(INiShader shader, uint flagValue) => shader.Type switch
        {
            ShaderGameType.FO3NV => shader.ShaderFlags.HasFlag((BSShaderFlags)flagValue),
            ShaderGameType.SK => shader.ShaderFlags_SSPF1.HasFlag((SkyrimShaderPropertyFlags1)flagValue),
            ShaderGameType.FO4 => shader.ShaderFlags_F4SPF1.HasFlag((Fallout4ShaderPropertyFlags1)flagValue),
            ShaderGameType.FO76SF => shader.ShaderFlagsList1.Any(sf => sf == (BSShaderCRC32)flagValue),
            _ => false
        };

        public static bool HasFlagSF2(INiShader shader, uint flagValue) => shader.Type switch
        {
            ShaderGameType.FO3NV => shader.ShaderFlags2.HasFlag((BSShaderFlags2)flagValue),
            ShaderGameType.SK => shader.ShaderFlags_SSPF2.HasFlag((SkyrimShaderPropertyFlags2)flagValue),
            ShaderGameType.FO4 => shader.ShaderFlags_F4SPF2.HasFlag((Fallout4ShaderPropertyFlags2)flagValue),
            ShaderGameType.FO76SF => shader.ShaderFlagsList2.Any(sf => sf == (BSShaderCRC32)flagValue),
            _ => false
        };

        public static void SetFlagSF1(INiShader shader, uint flagValue, bool set)
        {
            if (set)
            {
                switch (shader.Type)
                {
                    case ShaderGameType.FO3NV:
                        shader.ShaderFlags |= (BSShaderFlags)flagValue;
                        break;
                    case ShaderGameType.SK:
                        shader.ShaderFlags_SSPF1 |= (SkyrimShaderPropertyFlags1)flagValue;
                        break;
                    case ShaderGameType.FO4:
                        shader.ShaderFlags_F4SPF1 |= (Fallout4ShaderPropertyFlags1)flagValue;
                        break;
                    case ShaderGameType.FO76SF:
                        if (!shader.ShaderFlagsList1.Any(sf => sf == (BSShaderCRC32)flagValue))
                        {
                            shader.ShaderFlagsList1.Add((BSShaderCRC32)flagValue);
                        }
                        break;
                }
            }
            else
            {
                switch (shader.Type)
                {
                    case ShaderGameType.FO3NV:
                        shader.ShaderFlags &= ~(BSShaderFlags)flagValue;
                        break;
                    case ShaderGameType.SK:
                        shader.ShaderFlags_SSPF1 &= ~(SkyrimShaderPropertyFlags1)flagValue;
                        break;
                    case ShaderGameType.FO4:
                        shader.ShaderFlags_F4SPF1 &= ~(Fallout4ShaderPropertyFlags1)flagValue;
                        break;
                    case ShaderGameType.FO76SF:
                        shader.ShaderFlagsList1.RemoveAll(sf => sf == (BSShaderCRC32)flagValue);
                        break;
                }
            }
        }

        public static void SetFlagSF2(INiShader shader, uint flagValue, bool set)
        {
            if (set)
            {
                switch (shader.Type)
                {
                    case ShaderGameType.FO3NV:
                        shader.ShaderFlags2 |= (BSShaderFlags2)flagValue;
                        break;
                    case ShaderGameType.SK:
                        shader.ShaderFlags_SSPF2 |= (SkyrimShaderPropertyFlags2)flagValue;
                        break;
                    case ShaderGameType.FO4:
                        shader.ShaderFlags_F4SPF2 |= (Fallout4ShaderPropertyFlags2)flagValue;
                        break;
                    case ShaderGameType.FO76SF:
                        if (!shader.ShaderFlagsList2.Any(sf => sf == (BSShaderCRC32)flagValue))
                        {
                            shader.ShaderFlagsList2.Add((BSShaderCRC32)flagValue);
                        }
                        break;
                }
            }
            else
            {
                switch (shader.Type)
                {
                    case ShaderGameType.FO3NV:
                        shader.ShaderFlags2 &= ~(BSShaderFlags2)flagValue;
                        break;
                    case ShaderGameType.SK:
                        shader.ShaderFlags_SSPF2 &= ~(SkyrimShaderPropertyFlags2)flagValue;
                        break;
                    case ShaderGameType.FO4:
                        shader.ShaderFlags_F4SPF2 &= ~(Fallout4ShaderPropertyFlags2)flagValue;
                        break;
                    case ShaderGameType.FO76SF:
                        shader.ShaderFlagsList2.RemoveAll(sf => sf == (BSShaderCRC32)flagValue);
                        break;
                }
            }
        }
        #endregion

        #region Shader Flag Values
        public static uint SpecularFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags1.Specular,
            ShaderGameType.FO4 => (uint)Fallout4ShaderPropertyFlags1.Specular,
            _ => 0
        };

        public static uint SkinnedFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags1.Skinned,
            ShaderGameType.FO4 => (uint)Fallout4ShaderPropertyFlags1.Skinned,
            ShaderGameType.FO76SF => (uint)BSShaderCRC32.SKINNED,
            _ => 0
        };

        public static uint VertexAlphaFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags1.Vertex_Alpha,
            ShaderGameType.FO4 => (uint)Fallout4ShaderPropertyFlags1.Vertex_Alpha,
            ShaderGameType.FO76SF => (uint)BSShaderCRC32.VERTEX_ALPHA,
            _ => 0
        };

        public static uint GreyscaleColorFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags1.Greyscale_To_PaletteColor,
            ShaderGameType.FO4 => (uint)Fallout4ShaderPropertyFlags1.GreyscaleToPalette_Color,
            ShaderGameType.FO76SF => (uint)BSShaderCRC32.GRAYSCALE_TO_PALETTE_COLOR,
            _ => 0
        };

        public static uint GreyscaleAlphaFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags1.Greyscale_To_PaletteAlpha,
            ShaderGameType.FO4 => (uint)Fallout4ShaderPropertyFlags1.GreyscaleToPalette_Alpha,
            ShaderGameType.FO76SF => (uint)BSShaderCRC32.GRAYSCALE_TO_PALETTE_ALPHA,
            _ => 0
        };

        public static uint EnvironmentMappingFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags1.Environment_Mapping,
            ShaderGameType.FO4 => (uint)Fallout4ShaderPropertyFlags1.Environment_Mapping,
            ShaderGameType.FO76SF => (uint)BSShaderCRC32.ENVMAP,
            _ => 0
        };

        public static uint FalloffFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags1.Use_Falloff,
            ShaderGameType.FO4 => (uint)Fallout4ShaderPropertyFlags1.Use_Falloff,
            ShaderGameType.FO76SF => (uint)BSShaderCRC32.FALLOFF,
            _ => 0
        };

        public static uint ParallaxFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags1.Parallax,
            _ => 0
        };

        public static uint ModelSpaceFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags1.Model_Space_Normals,
            ShaderGameType.FO4 => (uint)Fallout4ShaderPropertyFlags1.Model_Space_Normals,
            ShaderGameType.FO76SF => (uint)BSShaderCRC32.MODELSPACENORMALS,
            _ => 0
        };

        public static uint EyeEnvironmentMappingFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags1.Eye_Environment_Mapping,
            ShaderGameType.FO4 => (uint)Fallout4ShaderPropertyFlags1.Eye_Environment_Mapping,
            _ => 0
        };

        public static uint EmissiveFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags1.Own_Emit,
            ShaderGameType.FO4 => (uint)Fallout4ShaderPropertyFlags1.Own_Emit,
            ShaderGameType.FO76SF => (uint)BSShaderCRC32.EMIT_ENABLED,
            _ => 0
        };

        public static uint ExternalEmittanceFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags1.External_Emittance,
            ShaderGameType.FO4 => (uint)Fallout4ShaderPropertyFlags1.External_Emittance,
            ShaderGameType.FO76SF => (uint)BSShaderCRC32.EXTERNAL_EMITTANCE,
            _ => 0
        };

        public static uint DoubleSidedFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags2.Double_Sided,
            ShaderGameType.FO4 => (uint)Fallout4ShaderPropertyFlags2.Double_Sided,
            ShaderGameType.FO76SF => (uint)BSShaderCRC32.TWO_SIDED,
            _ => 0
        };

        public static uint VertexColorsFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags2.Vertex_Colors,
            ShaderGameType.FO4 => (uint)Fallout4ShaderPropertyFlags2.Vertex_Colors,
            ShaderGameType.FO76SF => (uint)BSShaderCRC32.VERTEXCOLORS,
            _ => 0
        };

        public static uint GlowmapFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags2.Glow_Map,
            ShaderGameType.FO4 => (uint)Fallout4ShaderPropertyFlags2.Glow_Map,
            ShaderGameType.FO76SF => (uint)BSShaderCRC32.GLOWMAP,
            _ => 0
        };

        public static uint SoftlightFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags2.Soft_Lighting,
            _ => 0
        };

        public static uint RimlightFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags2.Rim_Lighting,
            _ => 0
        };

        public static uint BacklightFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags2.Back_Lighting,
            _ => 0
        };

        public static uint TreeAnimFlagValue(INiShader shader) => shader.Type switch
        {
            ShaderGameType.SK => (uint)SkyrimShaderPropertyFlags2.Tree_Anim,
            ShaderGameType.FO4 => (uint)Fallout4ShaderPropertyFlags2.Tree_Anim,
            _ => 0
        };
        #endregion
    }
}
