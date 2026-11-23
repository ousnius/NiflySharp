namespace NiflySharp
{
    public class NifFileLoadOptions
    {
        public bool IsTerrainFile = false;

        /// <summary>
        /// Fail the load if a block doesn't read exactly the size stored for it in the header.
        /// Catches malformed blocks before the following blocks read misaligned data.
        /// Only applies to file versions that store block sizes (>= 20.2.0.5) and to seekable streams.
        /// </summary>
        public bool CheckBlockSizes = false;
    }
}
