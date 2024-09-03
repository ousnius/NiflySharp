using NiflySharp.Enums;
using static NiflySharp.Helpers.ShaderHelper;

namespace NiflySharp.Blocks
{
    public partial class BSShaderProperty : NiShadeProperty, INiShader
    {
        public ShaderGameType Type { get; set; }

        public BSShaderType ShaderType_FO3_NV { get => _shaderType_BSST; set => _shaderType_BSST = value; }
        public BSLightingShaderType ShaderType_SK_FO4 { get => _shaderType; set => _shaderType = value; }

        public float EnvironmentMapScale { get => _environmentMapScale; set => _environmentMapScale = value; }

        /// <summary>
        /// Legacy Bethesda
        /// </summary>
        public BSShaderFlags ShaderFlags { get => _shaderFlags; set => _shaderFlags = value; }
        /// <summary>
        /// Legacy Bethesda
        /// </summary>
        public BSShaderFlags2 ShaderFlags2 { get => _shaderFlags2; set => _shaderFlags2 = value; }

        public bool IsTypeDefault => DefaultType(this);
        public bool IsTypeTallGrass => TallGrassType(this);
        public bool IsTypeWater => WaterType(this);
        public bool IsTypeLighting30 => Lighting30Type(this);
        public bool IsTypeTiled => TiledType(this);
        public bool IsTypeNoLighting => NoLightingType(this);
        public bool IsTypeEnvironmentMap => EnvironmentMapType(this);
        public bool IsTypeGlow => GlowType(this);
        public bool IsTypeParallax => ParallaxType(this);
        public bool IsTypeFaceTint => FaceTintType(this);
        public bool IsTypeSkinTint => SkinTintType(this);
        public bool IsTypeHairTint => HairTintType(this);
        public bool IsTypeParallaxOcclusion => ParallaxOcclusionType(this);
        public bool IsTypeMultitextureLandscape => MultitextureLandscapeType(this);
        public bool IsTypeLODLandscape => LODLandscapeType(this);
        public bool IsTypeSnow => SnowType(this);
        public bool IsTypeMultiLayerParallax => MultiLayerParallaxType(this);
        public bool IsTypeTreeAnim => TreeAnimType(this);
        public bool IsTypeLODObjects => LODObjectsType(this);
        public bool IsTypeSparkleSnow => SparkleSnowType(this);
        public bool IsTypeLODObjectsHD => LODObjectsHDType(this);
        public bool IsTypeEyeEnvironmentMap => EyeEnvironmentMapType(this);
        public bool IsTypeCloud => CloudType(this);
        public bool IsTypeLODLandscapeNoise => LODLandscapeNoiseType(this);
        public bool IsTypeMultitextureLandscapeLODBlend => MultitextureLandscapeLODBlendType(this);
        public bool IsTypeFO4Dismemberment => FO4DismembermentType(this);
        public bool IsTypeTerrain => TerrainType(this);

        /// <summary>
        /// Required for skinned meshes.
        /// </summary>
        public bool Skinned
        {
            get => HasFlagSF1(this, SkinnedFlagValue(this));
            set => SetFlagSF1(this, SkinnedFlagValue(this), value);
        }

        /// <summary>
        /// Double-sided/two-sided rendering.
        /// </summary>
        public bool DoubleSided
        {
            get => HasFlagSF2(this, DoubleSidedFlagValue(this));
            set => SetFlagSF2(this, DoubleSidedFlagValue(this), value);
        }

        /// <summary>
        /// Parallax flag (unused in vanilla game, used by mods)
        /// </summary>
        public bool Parallax
        {
            get => HasFlagSF1(this, ParallaxFlagValue(this));
            set => SetFlagSF1(this, ParallaxFlagValue(this), value);
        }

        /// <summary>
        /// Use model space normals and an external specular map.
        /// </summary>
        public bool ModelSpace
        {
            get => HasFlagSF1(this, ModelSpaceFlagValue(this));
            set => SetFlagSF1(this, ModelSpaceFlagValue(this), value);
        }

        /// <summary>
        /// Provides its own emittance color. (will not absorb light/ambient color?)
        /// </summary>
        public bool Emissive
        {
            get => HasFlagSF1(this, EmissiveFlagValue(this));
            set => SetFlagSF1(this, EmissiveFlagValue(this), value);
        }

        /// <summary>
        /// Enables specularity.
        /// </summary>
        public bool HasSpecular
        {
            get => HasFlagSF1(this, SpecularFlagValue(this));
            set => SetFlagSF1(this, SpecularFlagValue(this), value);
        }

        /// <summary>
        /// Enables rendering of/has vertex colors.
        /// </summary>
        public bool HasVertexColors
        {
            get => HasFlagSF2(this, VertexColorsFlagValue(this));
            set => SetFlagSF2(this, VertexColorsFlagValue(this), value);
        }

        /// <summary>
        /// Enables using alpha component of vertex colors.
        /// </summary>
        public bool HasVertexAlpha
        {
            get => HasFlagSF1(this, VertexAlphaFlagValue(this));
            set => SetFlagSF1(this, VertexAlphaFlagValue(this), value);
        }

        /// <summary>
        /// Use soft lighting map.
        /// </summary>
        public bool HasSoftlight
        {
            get => HasFlagSF2(this, SoftlightFlagValue(this));
            set => SetFlagSF2(this, SoftlightFlagValue(this), value);
        }

        /// <summary>
        /// Use back lighting map.
        /// </summary>
        public bool HasBacklight
        {
            get => HasFlagSF2(this, BacklightFlagValue(this));
            set => SetFlagSF2(this, BacklightFlagValue(this), value);
        }

        /// <summary>
        /// Use rim lighting map.
        /// </summary>
        public bool HasRimlight
        {
            get => HasFlagSF2(this, RimlightFlagValue(this));
            set => SetFlagSF2(this, RimlightFlagValue(this), value);
        }

        /// <summary>
        /// Use glow map.
        /// </summary>
        public bool HasGlowmap
        {
            get => HasFlagSF2(this, GlowmapFlagValue(this));
            set => SetFlagSF2(this, GlowmapFlagValue(this), value);
        }

        /// <summary>
        /// Use greyscale to palette color in BSEffectShaderProperty.
        /// </summary>
        public bool HasGreyscaleToPaletteColor
        {
            get => HasFlagSF1(this, GreyscaleColorFlagValue(this));
            set => SetFlagSF1(this, GreyscaleColorFlagValue(this), value);
        }

        /// <summary>
        /// Use greyscale to palette alpha in BSEffectShaderProperty.
        /// </summary>
        public bool HasGreyscaleToPaletteAlpha
        {
            get => HasFlagSF1(this, GreyscaleAlphaFlagValue(this));
            set => SetFlagSF1(this, GreyscaleAlphaFlagValue(this), value);
        }

        /// <summary>
        /// Environment mapping (uses environment map scale).
        /// </summary>
        public bool HasEnvironmentMapping
        {
            get => HasFlagSF1(this, EnvironmentMappingFlagValue(this));
            set => SetFlagSF1(this, EnvironmentMappingFlagValue(this), value);
        }

        /// <summary>
        /// Environment mapping for eyes.
        /// </summary>
        public bool HasEyeEnvironmentMapping
        {
            get => HasFlagSF1(this, EyeEnvironmentMappingFlagValue(this));
            set => SetFlagSF1(this, EyeEnvironmentMappingFlagValue(this), value);
        }

        /// <summary>
        /// Has external emittance.
        /// </summary>
        public bool HasExternalEmittance
        {
            get => HasFlagSF1(this, ExternalEmittanceFlagValue(this));
            set => SetFlagSF1(this, ExternalEmittanceFlagValue(this), value);
        }

        /// <summary>
        /// Tree animations flag.
        /// </summary>
        public bool HasTreeAnim
        {
            get => HasFlagSF2(this, TreeAnimFlagValue(this));
            set => SetFlagSF2(this, TreeAnimFlagValue(this), value);
        }
    }
}
