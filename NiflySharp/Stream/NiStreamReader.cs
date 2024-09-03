using System.IO;
using System.Text;

namespace NiflySharp.Stream
{
    public class NiStreamReader
    {
        public BinaryReader Reader { get; }

        public NifFile File { get; }

        public NiStreamReader(System.IO.Stream stream, NifFile file)
        {
            Reader = new BinaryReader(stream, Encoding.UTF8, true);
            File = file;
        }

        public string GetLine(int maxCount)
        {
            var byteArr = new byte[maxCount];

            int i = 0;
            byte b;
            while ((b = Reader.ReadByte()) != '\n' && i < maxCount)
            {
                byteArr[i] = b;
                i++;
            }

            return Encoding.Latin1.GetString(byteArr, 0, i).TrimEnd('\0');
        }
    }
}
