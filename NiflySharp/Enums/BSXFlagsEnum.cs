namespace NiflySharp.Enums
{
    /// <summary>
    /// Describes the options on BSXFlags.
    /// Controls animation and collision.
    /// </summary>
    public enum BSXFlagsEnum : uint
    {
        /// <summary>
        /// Enable Havok, bAnimated (Skyrim)
        /// </summary>
        Animated = 1 << 0,

        /// <summary>
        /// Enable collision, bHavok (Skyrim)
        /// </summary>
        Havok = 1 << 1,

        /// <summary>
        /// Is skeleton nif? bRagdoll (Skyrim)
        /// </summary>
        Ragdoll = 1 << 2,

        /// <summary>
        /// Enable animation, bComplex (Skyrim)
        /// </summary>
        Complex = 1 << 3,

        /// <summary>
        /// FlameNodes present, bAddon (Skyrim)
        /// </summary>
        Addon = 1 << 4,

        /// <summary>
        /// EditorMarkers present, bEditorMarker (Skyrim)
        /// </summary>
        EditorMarker = 1 << 5,

        /// <summary>
        /// bDynamic (Skyrim)
        /// </summary>
        Dynamic = 1 << 6,

        /// <summary>
        /// bArticulated (Skyrim)
        /// </summary>
        Articulated = 1 << 7,

        /// <summary>
        /// bIKTarget (Skyrim) / needsTransformUpdates
        /// </summary>
        NeedsTransformUpdates = 1 << 8,

        /// <summary>
        /// bExternalEmit (Skyrim)
        /// </summary>
        ExternalEmittance = 1 << 9,

        /// <summary>
        /// bMagicShaderParticles (Skyrim)
        /// </summary>
        MagicShaderParticles = 1 << 10,

        /// <summary>
        /// bLights (Skyrim)
        /// </summary>
        Lights = 1 << 11,

        /// <summary>
        /// bBreakable (Skyrim)
        /// </summary>
        Breakable = 1 << 12,

        /// <summary>
        /// bSearchedBreakable (Skyrim). Runtime only?
        /// </summary>
        SearchedBreakable = 1 << 13
    }
}
