using NiflySharp.Stream;

namespace NiflySharp.Blocks
{
    public partial class NiKeyframeData
    {
        public new void BeforeSync(NiStreamReversible stream)
        {
            if ((uint)_rotationType == 4)
            {
                _numRotationKeys = 1;
            }
        }
    }
}
