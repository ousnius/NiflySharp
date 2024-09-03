using NiflySharp.Stream;

namespace NiflySharp
{
    public interface INiStreamable
    {
        void Sync(NiStreamReversible stream);
    }
}
