using System;
using System.Buffers.Binary;
using System.Text;
using System.IO;

namespace NiflySharp.Stream
{
    public class CustomBinaryReader : BinaryReader
    {
        public NiEndian Endian { get; set; } = NiEndian.Little;

        public CustomBinaryReader(System.IO.Stream input)
            : base(input)
        {
        }

        public CustomBinaryReader(System.IO.Stream input, Encoding encoding)
            : base(input, encoding)
        {
        }

        public CustomBinaryReader(System.IO.Stream input, Encoding encoding, bool leaveOpen)
            : base(input, encoding, leaveOpen)
        {
        }

        private ReadOnlySpan<byte> ReadSpan(int byteCount)
        {
            byte[] buffer = new byte[byteCount];
            int read = BaseStream.Read(buffer, 0, byteCount);
            if (read != byteCount)
                throw new EndOfStreamException();
            return buffer;
        }

        public override short ReadInt16()
        {
            var span = ReadSpan(2);
            return Endian == NiEndian.Big
                ? BinaryPrimitives.ReadInt16BigEndian(span)
                : BinaryPrimitives.ReadInt16LittleEndian(span);
        }

        public override ushort ReadUInt16()
        {
            var span = ReadSpan(2);
            return Endian == NiEndian.Big
                ? BinaryPrimitives.ReadUInt16BigEndian(span)
                : BinaryPrimitives.ReadUInt16LittleEndian(span);
        }

        public override int ReadInt32()
        {
            var span = ReadSpan(4);
            return Endian == NiEndian.Big
                ? BinaryPrimitives.ReadInt32BigEndian(span)
                : BinaryPrimitives.ReadInt32LittleEndian(span);
        }

        public override uint ReadUInt32()
        {
            var span = ReadSpan(4);
            return Endian == NiEndian.Big
                ? BinaryPrimitives.ReadUInt32BigEndian(span)
                : BinaryPrimitives.ReadUInt32LittleEndian(span);
        }

        public override long ReadInt64()
        {
            var span = ReadSpan(8);
            return Endian == NiEndian.Big
                ? BinaryPrimitives.ReadInt64BigEndian(span)
                : BinaryPrimitives.ReadInt64LittleEndian(span);
        }

        public override ulong ReadUInt64()
        {
            var span = ReadSpan(8);
            return Endian == NiEndian.Big
                ? BinaryPrimitives.ReadUInt64BigEndian(span)
                : BinaryPrimitives.ReadUInt64LittleEndian(span);
        }

        public override float ReadSingle()
        {
            var span = ReadSpan(4);
            if (Endian == NiEndian.Big)
            {
                byte[] reversed = span.ToArray();
                Array.Reverse(reversed);
                return BitConverter.ToSingle(reversed, 0);
            }
            return BitConverter.ToSingle(span);
        }

        public override double ReadDouble()
        {
            var span = ReadSpan(8);
            if (Endian == NiEndian.Big)
            {
                byte[] reversed = span.ToArray();
                Array.Reverse(reversed);
                return BitConverter.ToDouble(reversed, 0);
            }
            return BitConverter.ToDouble(span);
        }

        public override Half ReadHalf()
        {
            var span = ReadSpan(2);
            return Endian == NiEndian.Big
                ? BinaryPrimitives.ReadHalfBigEndian(span)
                : BinaryPrimitives.ReadHalfLittleEndian(span);
        }
    }
}
