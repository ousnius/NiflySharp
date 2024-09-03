using NiflySharp.Extensions;
using NiflySharp.Stream;
using System.Runtime.InteropServices;

namespace NiflySharp.Blocks
{
    public partial class NiSkinData
    {
        public new void BeforeSync(NiStreamReversible stream)
        {
            if (_hasVertexWeights == null)
                _hasVertexWeights = true;

            var boneListSpan = CollectionsMarshal.AsSpan(_boneList);

            foreach (ref var bone in boneListSpan)
            {
                if (!_hasVertexWeights.GetValueOrDefault())
                    bone.NumVertices = 0;

                bone.VertexWeights = bone.VertexWeights.Resize(bone.NumVertices);
            }
        }

        public new void AfterSync(NiStreamReversible stream)
        {
            if (_hasVertexWeights == null)
                _hasVertexWeights = true;

            var boneListSpan = CollectionsMarshal.AsSpan(_boneList);

            foreach (ref var bone in boneListSpan)
            {
                if (!_hasVertexWeights.GetValueOrDefault())
                    bone.NumVertices = 0;

                bone.VertexWeights = bone.VertexWeights.Resize(bone.NumVertices);
            }
        }
    }
}
