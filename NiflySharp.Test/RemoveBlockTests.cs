using NiflySharp.Blocks;
using NiflySharp.Structs;
using System.Linq;
using Xunit;

namespace NiflySharp.Test
{
    public class RemoveBlockTests
    {
        /// <summary>
        /// Root (block 0) with children "A" (block 1) and "B" (block 2), listed in the order given.
        /// </summary>
        private static (NifFile nif, NiNode root) BuildRootWithChildren(params int[] childOrder)
        {
            var nif = new NifFile(NiVersion.GetSSE());
            var root = new NiNode { Name = new NiStringRef("Root") };
            nif.AddBlock(root);
            nif.AddBlock(new NiNode { Name = new NiStringRef("A") });
            nif.AddBlock(new NiNode { Name = new NiStringRef("B") });
            foreach (int id in childOrder)
                root.Children.AddBlockRef(id);
            return (nif, root);
        }

        private static string[] ChildNames(NifFile nif, NiNode root) =>
            root.Children.Indices
                .Where(i => i != NiRef.NPOS)
                .Select(i => nif.GetBlock<NiNode>(i).Name.String)
                .ToArray();

        [Fact(DisplayName = "RemoveBlock keeps a sibling listed before the removed block")]
        public void RemoveBlock_SiblingListedFirst_IsKept()
        {
            // B (block 2) is listed before A (block 1). Removing A renumbers B to 1 first, and the
            // index-based list removal for A's ref then matched B's ref instead.
            var (nif, root) = BuildRootWithChildren(2, 1);

            Assert.True(nif.RemoveBlock(1));

            Assert.Equal(new[] { "B" }, ChildNames(nif, root));
            Assert.Equal(1, root.Children.Count);
        }

        [Fact(DisplayName = "RemoveBlock keeps a sibling listed after the removed block")]
        public void RemoveBlock_SiblingListedAfter_IsKept()
        {
            var (nif, root) = BuildRootWithChildren(1, 2);

            Assert.True(nif.RemoveBlock(1));

            Assert.Equal(new[] { "B" }, ChildNames(nif, root));
            Assert.Equal(1, root.Children.Count);
        }

        [Fact(DisplayName = "RemoveBlock clears instead of removes refs in arrays that keep empty refs")]
        public void RemoveBlock_KeepEmptyRefs_ClearsInPlace()
        {
            var (nif, root) = BuildRootWithChildren(1, 2);
            root.Children.KeepEmptyRefs = true;

            Assert.True(nif.RemoveBlock(1));

            Assert.Equal(new[] { NiRef.NPOS, 1 }, root.Children.Indices.ToArray());
        }

        [Fact(DisplayName = "RemoveBlock removes the right ref from a pointer array")]
        public void RemoveBlock_PointerArray_SiblingListedFirst_IsKept()
        {
            var (nif, _) = BuildRootWithChildren();
            var controller = new NiMultiTargetTransformController { ExtraTargets = new NiBlockPtrArray<NiAVObject>() };
            nif.AddBlock(controller);
            controller.ExtraTargets.AddBlockRef(2);
            controller.ExtraTargets.AddBlockRef(1);

            Assert.True(nif.RemoveBlock(1));

            // B (was block 2) is now block 1
            Assert.Equal(new[] { 1 }, controller.ExtraTargets.Indices.ToArray());
        }

        [Fact(DisplayName = "RemoveBlock keeps the sibling after saving and reloading")]
        public void RemoveBlock_SiblingListedFirst_SurvivesSave()
        {
            var (nif, _) = BuildRootWithChildren(2, 1);
            Assert.True(nif.RemoveBlock(1));

            using var stream = new System.IO.MemoryStream();
            Assert.Equal(0, nif.Save(stream));
            stream.Position = 0;
            var reloaded = new NifFile();
            Assert.Equal(0, reloaded.Load(stream));

            var reloadedRoot = reloaded.Blocks.OfType<NiNode>().First(n => n.Name.String == "Root");
            Assert.Equal(new[] { "B" }, ChildNames(reloaded, reloadedRoot));
        }
    }
}
