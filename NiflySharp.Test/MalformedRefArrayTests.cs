using NiflySharp.Blocks;
using NiflySharp.Stream;
using System;
using System.IO;
using System.Threading;
using Xunit;

namespace NiflySharp.Test
{
    public class MalformedRefArrayTests
    {
        const string SkinnedFile = "Assets/V20.2.0.7/12/100/Skinned.nif";

        // Byte 5 of the vertex descriptor of each NiSkinPartition, and a byte later in each.
        // Zeroing one makes the partition read short, so the next block reads a garbage
        // extra data count and used to allocate a ref for each of them.
        [Theory(DisplayName = "Load fails fast on a garbage ref count (SSE)")]
        [InlineData(1798)]
        [InlineData(7243)]
        [InlineData(12417)]
        [InlineData(17862)]
        public void Load_GarbageRefCount_FailsFast(int offset)
        {
            var bytes = File.ReadAllBytes(SkinnedFile);
            Assert.Equal(0, new NifFile().Load(new MemoryStream(bytes)));

            bytes[offset] = 0x00;

            int result = -1;
            long allocated = -1;
            var thread = new Thread(() =>
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                result = new NifFile().Load(new MemoryStream(bytes));
                allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            })
            { IsBackground = true };

            thread.Start();
            Assert.True(thread.Join(TimeSpan.FromSeconds(5)), "Load didn't finish");
            Assert.NotEqual(0, result);
            Assert.InRange(allocated, 0, 16 * 1024 * 1024);
        }

        private static NiStreamReversible ReadStream(byte[] data) =>
            new(new NiStreamReader(new MemoryStream(data), new NifFile()));

        [Fact(DisplayName = "Ref array rejects a count larger than the remaining data")]
        public void RefArraySync_CountPastEnd_Throws()
        {
            // Count of 3 refs, but only 2 refs (8 bytes) follow
            var data = new byte[4 + 8];
            BitConverter.GetBytes(3).CopyTo(data, 0);

            var refs = new NiBlockRefArray<NiNode>();
            Assert.Throws<InvalidDataException>(() => refs.Sync(ReadStream(data)));

            BitConverter.GetBytes(int.MaxValue).CopyTo(data, 0);
            Assert.Throws<InvalidDataException>(() => refs.Sync(ReadStream(data)));

            BitConverter.GetBytes(-1).CopyTo(data, 0);
            Assert.Throws<InvalidDataException>(() => refs.Sync(ReadStream(data)));

            BitConverter.GetBytes(2).CopyTo(data, 0);
            refs.Sync(ReadStream(data));
            Assert.Equal(2, refs.Count);
        }

        [Fact(DisplayName = "Short ref array rejects a count larger than the remaining data")]
        public void RefShortArraySync_CountPastEnd_Throws()
        {
            var data = new byte[2 + 8];
            BitConverter.GetBytes((short)3).CopyTo(data, 0);

            var refs = new NiBlockRefShortArray<NiNode>();
            Assert.Throws<InvalidDataException>(() => refs.Sync(ReadStream(data)));

            BitConverter.GetBytes((short)-1).CopyTo(data, 0);
            Assert.Throws<InvalidDataException>(() => refs.Sync(ReadStream(data)));

            BitConverter.GetBytes((short)2).CopyTo(data, 0);
            refs.Sync(ReadStream(data));
            Assert.Equal(2, refs.Count);
        }
    }
}
