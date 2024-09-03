using NiflySharp.Blocks;
using NiflySharp.Enums;
using System;
using System.Linq;

namespace NiflySharp
{
    public static class NifFixes
    {
        /// <summary>
        /// Fixes inconsistent flags on "BSXFlags" block with name "BSX".
        /// Adds or removes the external emittance flag depending on if any shaders in the file are using it or not.
        /// </summary>
        /// <param name="nif">NIF file</param>
        public static void FixBSXFlags(this NifFile nif)
        {
            var bsxFlags = nif.FindBlockByName<BSXFlags>("BSX");
            if (bsxFlags == null)
                return;

            var flags = (BSXFlagsEnum)bsxFlags.IntegerData;
            if (flags.HasFlag(BSXFlagsEnum.ExternalEmittance))
            {
                // BSXFlags external emittance = on. Check if any shaders require that.
                bool flagUnnecessary = !nif.Blocks.OfType<INiShader>().Any(shader => shader.HasExternalEmittance);
                if (flagUnnecessary)
                {
                    // Unset unnecessary external emittance flag on BSXFlags
                    flags &= ~BSXFlagsEnum.ExternalEmittance;
                    bsxFlags.IntegerData = (uint)flags;
                }
            }
            else
            {
                // BSXFlags external emittance = off. Check if any shaders have it set regardless.
                bool flagMissing = nif.Blocks.OfType<INiShader>().Any(shader => shader.HasExternalEmittance);
                if (flagMissing)
                {
                    // Set missing external emittance flag on BSXFlags
                    flags |= BSXFlagsEnum.ExternalEmittance;
                    bsxFlags.IntegerData = (uint)flags;
                }
            }
        }

        /// <summary>
        /// Fixes inconsistent shader flags.
        /// Adds or removes the environment mapping flag depending on the shader type of a shader.
        /// </summary>
        /// <param name="nif">NIF file</param>
        public static void FixShaderFlags(this NifFile nif)
        {
            foreach (var shader in nif.Blocks.OfType<INiShader>())
            {
                if (!shader.IsTypeEnvironmentMap && shader.HasEnvironmentMapping)
                {
                    // Shader is no environment shader, remove unused shader flag
                    shader.HasEnvironmentMapping = false;
                }
                else if (shader.IsTypeEnvironmentMap && !shader.HasEnvironmentMapping)
                {
                    // Shader is environment shader, add missing shader flag
                    shader.HasEnvironmentMapping = true;
                }
            }
        }
    }
}
