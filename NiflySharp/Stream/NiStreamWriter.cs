using System.IO;
using System.Text;

namespace NiflySharp.Stream
{
    public class NiStreamWriter
    {
        public BinaryWriter Writer { get; }

        public NifFile File { get; }

        public long BlockSizePos { get; set; }

        public NiStreamWriter(System.IO.Stream stream, NifFile file)
        {
            Writer = new BinaryWriter(stream, Encoding.UTF8, true);
            File = file;
        }
    }
}
