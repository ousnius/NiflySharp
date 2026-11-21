using NiflySharp.Blocks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace NiflySharp.Test
{
    public class TriStripsDataTests
    {
        // The strip fields have no public accessors, so the test reaches them by reflection.
        private static T GetField<T>(NiTriStripsData data, string name) =>
            (T)typeof(NiTriStripsData).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(data)!;

        private static void SetField(NiTriStripsData data, string name, object value) =>
            typeof(NiTriStripsData).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(data, value);

        [Fact(DisplayName = "Save keeps the strip count of NiTriStripsData (SE)")]
        public void Save_KeepsStripCount()
        {
            // Two strips with 4 and 3 points. Points is stored flattened, so it holds 7 entries
            // while the strip count is 2.
            var strips = new NiTriStripsData();
            SetField(strips, "_stripLengths", new List<ushort> { 4, 3 });
            SetField(strips, "_points", new List<ushort> { 0, 1, 2, 3, 4, 5, 6 });

            var nif = new NifFile(NiVersion.GetSSE());
            nif.AddBlock(strips);

            using var stream = new MemoryStream();
            Assert.Equal(0, nif.Save(stream, new NifFileSaveOptions { RemoveUnreferencedBlocks = false }));

            stream.Position = 0;
            var reloaded = new NifFile(stream);
            var data = reloaded.Blocks.OfType<NiTriStripsData>().Single();

            Assert.Equal((ushort)2, GetField<ushort>(data, "_numStrips"));
            Assert.Equal(new ushort[] { 4, 3 }, GetField<List<ushort>>(data, "_stripLengths"));
            Assert.Equal(new ushort[] { 0, 1, 2, 3, 4, 5, 6 }, GetField<List<ushort>>(data, "_points"));
        }
    }
}
