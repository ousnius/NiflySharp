using NiflySharp.Blocks;
using Xunit;

namespace NiflySharp.Test
{
    public class RefArrayTests
    {
        public static TheoryData<NiRefArray> Arrays => new()
        {
            new NiBlockRefArray<NiAVObject>(),
            new NiBlockRefShortArray<NiAVObject>(),
            new NiBlockPtrArray<NiAVObject>(),
            new NiBlockPtrShortArray<NiAVObject>(),
        };

        [Theory(DisplayName = "Clone keeps KeepEmptyRefs and the ref indices")]
        [MemberData(nameof(Arrays))]
        public void Clone_KeepsKeepEmptyRefs(NiRefArray array)
        {
            array.AddBlockRef(NiRef.NPOS);
            array.AddBlockRef(3);
            array.KeepEmptyRefs = true;

            var clone = array.Clone();

            Assert.Equal(array.GetType(), clone.GetType());
            Assert.True(clone.KeepEmptyRefs);
            Assert.Equal(new[] { NiRef.NPOS, 3 }, clone.Indices);
        }
    }
}
