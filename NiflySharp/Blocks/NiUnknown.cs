using NiflySharp.Stream;

namespace NiflySharp.Blocks
{
    public class NiUnknown : NiObject, INiStreamable
    {
        private byte[] data;

        public byte[] Data { get => data; set => data = value; }

        public NiUnknown() { }

        public NiUnknown(NiStreamReversible stream, int size)
        {
            data = new byte[size];
            blockSize = size;

            Sync(stream);
        }

        public NiUnknown(int size)
        {
            data = new byte[size];
            blockSize = size;
        }

        public override void Sync(NiStreamReversible stream)
        {
            if (data == null || data.Length == 0)
                return;

            stream.Sync(ref data);
        }
    }
}
