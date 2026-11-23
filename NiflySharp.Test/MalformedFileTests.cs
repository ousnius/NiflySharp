using NiflySharp.Blocks;
using NiflySharp.Stream;
using System;
using System.IO;
using Xunit;

namespace NiflySharp.Test
{
    public class MalformedFileTests
    {
        const string SkinnedFile = "Assets/V20.2.0.7/12/100/Skinned.nif";

        // Changed bytes that make a block read a different number of bytes than its stored
        // size, in a file the default load still accepts:
        // NiSkinPartition partition data, and the BSShaderTextureSet texture count (9 -> 0).
        [Theory(DisplayName = "Block size check rejects a block that reads the wrong size (SSE)")]
        [InlineData(17860, 0xFF)]
        [InlineData(20324, 0x00)]
        [InlineData(21796, 0x00)]
        public void Load_CheckBlockSizes_RejectsWrongSize(int offset, byte value)
        {
            var bytes = File.ReadAllBytes(SkinnedFile);
            bytes[offset] = value;

            Assert.Equal(0, new NifFile().Load(new MemoryStream(bytes)));

            var options = new NifFileLoadOptions { CheckBlockSizes = true };
            Assert.Equal(3, new NifFile().Load(new MemoryStream(bytes), options));
        }

        [Fact(DisplayName = "Block size check accepts all valid test files")]
        public void Load_CheckBlockSizes_AcceptsValidFiles()
        {
            var options = new NifFileLoadOptions { CheckBlockSizes = true };

            foreach (var file in Directory.EnumerateFiles("Assets", "*.nif", SearchOption.AllDirectories))
            {
                if (new NifFile().Load(file) != 0)
                    continue;

                Assert.True(new NifFile().Load(file, options) == 0, $"Block size mismatch in {file}");
            }
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
