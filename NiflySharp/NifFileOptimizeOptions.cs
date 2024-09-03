namespace NiflySharp
{
    public class NifFileOptimizeOptions
    {
        /// <summary>
        /// NiVersion target for the optimization process
        /// </summary>
        public required NiVersion TargetVersion;

        /// <summary>
        /// Use mesh formats required for head parts (use ONLY for head parts!)
        /// </summary>
        public bool HeadPartsOnly = false;

        /// <summary>
        /// Remove parallax shader flags and texture paths
        /// </summary>
        public bool RemoveParallax = true;

        /// <summary>
        /// Recalculate bounding spheres for unskinned meshes
        /// </summary>
        public bool CalculateBounds = true;

        /// <summary>
        /// Fix BSX flag values based on file contents
        /// </summary>
        public bool FixBSXFlags = true;

        /// <summary>
        /// Fix shader flag values based on file contents
        /// </summary>
        public bool FixShaderFlags = true;
    }
}
