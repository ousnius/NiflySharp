using NiflySharp.Extensions;
using NiflySharp.Stream;
using NiflySharp.Structs;
using System.Runtime.InteropServices;

namespace NiflySharp.Blocks
{
    public partial class NiSkinData
    {
        // Kept by BeforeSync, put back by AfterSync
        private BoneData[] _bonesBeforeWrite;

        public new void BeforeSync(NiStreamReversible stream)
        {
            RestoreStashedBones();

            // The flag is not in the file below that version, where weights are always present
            if (stream.Version.FileVersion < NiFileVersion.V4_2_1_0)
                _hasVertexWeights = true;

            if (_hasVertexWeights == null)
                _hasVertexWeights = true;

            var boneListSpan = CollectionsMarshal.AsSpan(_boneList);

            if (_hasVertexWeights.GetValueOrDefault())
            {
                foreach (ref var bone in boneListSpan)
                    bone.VertexWeights = bone.VertexWeights.Resize(bone.NumVertices);

                return;
            }

            if (stream.CurrentMode != NiStreamReversible.Mode.Write)
                return;

            // Zeros go to the file, the counts and weights come back in AfterSync
            _bonesBeforeWrite = boneListSpan.ToArray();

            foreach (ref var bone in boneListSpan)
            {
                bone.NumVertices = 0;
                bone.VertexWeights = [];
            }
        }

        public new void AfterSync(NiStreamReversible stream)
        {
            if (_hasVertexWeights == null)
                _hasVertexWeights = true;

            if (stream.CurrentMode == NiStreamReversible.Mode.Write)
            {
                RestoreStashedBones();
                return;
            }

            var boneListSpan = CollectionsMarshal.AsSpan(_boneList);

            foreach (ref var bone in boneListSpan)
            {
                if (!_hasVertexWeights.GetValueOrDefault())
                    bone.NumVertices = 0;

                bone.VertexWeights = bone.VertexWeights.Resize(bone.NumVertices);
            }
        }

        private void RestoreStashedBones()
        {
            if (_bonesBeforeWrite == null)
                return;

            var boneListSpan = CollectionsMarshal.AsSpan(_boneList);

            for (int i = 0; i < boneListSpan.Length && i < _bonesBeforeWrite.Length; i++)
                boneListSpan[i] = _bonesBeforeWrite[i];

            _bonesBeforeWrite = null;
        }
    }
}
