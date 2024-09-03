using NiflySharp.Stream;

namespace NiflySharp.Blocks
{
    public partial class NiMultiTargetTransformController
    {
        public new void BeforeSync(NiStreamReversible stream)
        {
            _extraTargets ??= new NiBlockPtrArray<NiAVObject>();
            _extraTargets.KeepEmptyRefs = true;
        }
    }
}
