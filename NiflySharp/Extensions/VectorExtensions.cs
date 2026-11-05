using System.Numerics;

namespace NiflySharp.Extensions
{
    public static class VectorExtensions
    {
        /// <summary>
        /// Normalizes <paramref name="v"/> as nifly's Vector3::Normalize does: a vector whose length
        /// is zero is returned unchanged. <see cref="Vector3.Normalize(Vector3)"/> returns NaN for it,
        /// which includes tiny non-zero vectors whose squared length underflows.
        /// </summary>
        public static Vector3 NormalizeOrZero(this Vector3 v)
        {
            float length = v.Length();
            return length == 0.0f ? v : v / length;
        }
    }
}
