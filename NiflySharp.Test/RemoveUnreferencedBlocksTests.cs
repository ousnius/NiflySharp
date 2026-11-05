using NiflySharp.Blocks;
using NiflySharp.Structs;
using System.Linq;
using Xunit;

namespace NiflySharp.Test
{
    public class RemoveUnreferencedBlocksTests
    {
        private static NiNode Node(NifFile nif, string name, out int id)
        {
            var node = new NiNode { Name = new NiStringRef(name) };
            id = nif.AddBlock(node);
            return node;
        }

        private static string[] Names(NifFile nif) =>
            nif.Blocks.Select(b => b switch
            {
                NiObjectNET named => named.Name.String,
                NiExtraData data => data.Name.String,
                _ => b.GetType().Name
            }).ToArray();

        [Fact(DisplayName = "RemoveUnreferencedBlocks removes a loose chain and keeps what the root reaches")]
        public void LooseChain_Removed()
        {
            var nif = new NifFile(NiVersion.GetSSE());
            var root = Node(nif, "Root", out _);
            Node(nif, "Kept", out int kept);
            root.Children.AddBlockRef(kept);
            var a = Node(nif, "LooseA", out _);
            var b = Node(nif, "LooseB", out int bId);
            Node(nif, "LooseC", out int cId);
            a.Children.AddBlockRef(bId);
            b.Children.AddBlockRef(cId);

            Assert.Equal(3, nif.RemoveUnreferencedBlocks());

            Assert.Equal(new[] { "Root", "Kept" }, Names(nif));
            Assert.Equal(1, root.Children.Indices.Single());
        }

        [Fact(DisplayName = "RemoveUnreferencedBlocks keeps a loose cycle and a self-referencing block")]
        public void CycleAndSelfReference_Kept()
        {
            var nif = new NifFile(NiVersion.GetSSE());
            Node(nif, "Root", out _);
            var a = Node(nif, "CycleA", out int aId);
            var b = Node(nif, "CycleB", out int bId);
            a.Children.AddBlockRef(bId);
            b.Children.AddBlockRef(aId);
            var self = Node(nif, "Self", out int selfId);
            self.Children.AddBlockRef(selfId);

            Assert.Equal(0, nif.RemoveUnreferencedBlocks());

            Assert.Equal(new[] { "Root", "CycleA", "CycleB", "Self" }, Names(nif));
        }

        [Fact(DisplayName = "RemoveUnreferencedBlocks<T> only removes blocks of type T")]
        public void TypeFilter_Respected()
        {
            var nif = new NifFile(NiVersion.GetSSE());
            Node(nif, "Root", out _);
            var looseNode = Node(nif, "LooseNode", out _);
            looseNode.ExtraDataList = new NiBlockRefArray<NiExtraData>();
            int heldData = nif.AddBlock(new NiStringExtraData { Name = new NiStringRef("HeldByLooseNode") });
            looseNode.ExtraDataList.AddBlockRef(heldData);
            nif.AddBlock(new NiStringExtraData { Name = new NiStringRef("LooseData") });

            Assert.Equal(1, nif.RemoveUnreferencedBlocks<NiExtraData>());

            Assert.Equal(new[] { "Root", "LooseNode", "HeldByLooseNode" }, Names(nif));
        }
    }
}
