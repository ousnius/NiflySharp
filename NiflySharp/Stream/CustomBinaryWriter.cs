using NiflySharp;
using System;
using System.Buffers.Binary;
using System.Text;
using System.IO;

namespace NiflySharp.Stream
{
    public class CustomBinaryWriter : BinaryWriter
    {
        public NiEndian Endian { get; set; } = NiEndian.Little;

        public CustomBinaryWriter(System.IO.Stream output)
            : base(output)
        {
        }

        public CustomBinaryWriter(System.IO.Stream output, Encoding encoding)
            : base(output, encoding)
        {
        }

        public CustomBinaryWriter(System.IO.Stream output, Encoding encoding, bool leaveOpen)
            : base(output, encoding, leaveOpen)
        {
        }

        protected CustomBinaryWriter()
        {
        }

        private void WriteSpan(ReadOnlySpan<byte> span)
        {
            OutStream.Write(span);
        }

        public override void Write(short value)
        {
            Span<byte> buffer = stackalloc byte[2];
            if (Endian == NiEndian.Big)
                BinaryPrimitives.WriteInt16BigEndian(buffer, value);
            else
                BinaryPrimitives.WriteInt16LittleEndian(buffer, value);

            WriteSpan(buffer);
        }

        public override void Write(ushort value)
        {
            Span<byte> buffer = stackalloc byte[2];
            if (Endian == NiEndian.Big)
                BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
            else
                BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);

            WriteSpan(buffer);
        }

        public override void Write(int value)
        {
            Span<byte> buffer = stackalloc byte[4];
            if (Endian == NiEndian.Big)
                BinaryPrimitives.WriteInt32BigEndian(buffer, value);
            else
                BinaryPrimitives.WriteInt32LittleEndian(buffer, value);

            WriteSpan(buffer);
        }

        public override void Write(uint value)
        {
            Span<byte> buffer = stackalloc byte[4];
            if (Endian == NiEndian.Big)
                BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
            else
                BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);

            WriteSpan(buffer);
        }

        public override void Write(long value)
        {
            Span<byte> buffer = stackalloc byte[8];
            if (Endian == NiEndian.Big)
                BinaryPrimitives.WriteInt64BigEndian(buffer, value);
            else
                BinaryPrimitives.WriteInt64LittleEndian(buffer, value);

            WriteSpan(buffer);
        }

        public override void Write(ulong value)
        {
            Span<byte> buffer = stackalloc byte[8];
            if (Endian == NiEndian.Big)
                BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
            else
                BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);

            WriteSpan(buffer);
        }

        public override void Write(float value)
        {
            Span<byte> buffer = stackalloc byte[4];
            BitConverter.TryWriteBytes(buffer, value);

            if (Endian == NiEndian.Big)
            {
                buffer[0] = buffer[3];
                buffer[1] = buffer[2];
                buffer[2] = buffer[1];
                buffer[3] = buffer[0];
            }

            WriteSpan(buffer);
        }

        public override void Write(double value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BitConverter.TryWriteBytes(buffer, value);

            if (Endian == NiEndian.Big)
            {
                buffer[0] = buffer[7];
                buffer[1] = buffer[6];
                buffer[2] = buffer[5];
                buffer[3] = buffer[4];
                buffer[4] = buffer[3];
                buffer[5] = buffer[2];
                buffer[6] = buffer[1];
                buffer[7] = buffer[0];
            }

            WriteSpan(buffer);
        }

        public override void Write(Half value)
        {
            Span<byte> buffer = stackalloc byte[2];
            if (Endian == NiEndian.Big)
                BinaryPrimitives.WriteHalfBigEndian(buffer, value);
            else
                BinaryPrimitives.WriteHalfLittleEndian(buffer, value);

            WriteSpan(buffer);
        }

        public override void Write(decimal value)
        {
            Span<byte> buffer = stackalloc byte[16];
            int[] bits = decimal.GetBits(value);
            for (int i = 0; i < 4; i++)
            {
                if (Endian == NiEndian.Big)
                    BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(i * 4, 4), bits[i]);
                else
                    BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(i * 4, 4), bits[i]);
            }
            WriteSpan(buffer);
        }
    }
}
