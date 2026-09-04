namespace NiflySharp
{
    public static class NifConstants
    {
        public const float EPSILON = 0.0001f;

        /// <summary>
        /// Arbitrary limit for the element count of an array for file IO validation.
        /// Keeps misinterpreted or corrupted file data from causing huge allocations.
        /// </summary>
        public const int ArraySizeLimit = 1024 * 1024 * 10;

        /// <summary>
        /// Arbitrary limit for block indices for file IO validation.
        /// </summary>
        public const int BlockIndexLimit = 1024 * 1024;

        /// <summary>
        /// Arbitrary limit for string indices for file IO validation.
        /// </summary>
        public const int StringIndexLimit = 1024 * 1024;
    }
}
