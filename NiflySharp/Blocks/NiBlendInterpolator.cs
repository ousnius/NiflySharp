using NiflySharp.Enums;
using NiflySharp.Stream;

namespace NiflySharp.Blocks
{
    public partial class NiBlendInterpolator
    {
        public new void BeforeSync(NiStreamReversible stream)
        {
            if (stream.Version.FileVersion >= NiVersion.ToFile(10, 1, 0, 112) &&
                _flags.HasFlag(InterpBlendFlags.ManagerControlled))
            {
                if (ArraySize_by == 0)
                    ArraySize_by = 2; // Default is 2 even though only used for 'interpArrayItems' when 'flags' are manager controlled?
            }
        }
    }
}
