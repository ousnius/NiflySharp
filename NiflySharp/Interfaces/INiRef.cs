using System.Collections;

namespace NiflySharp
{
    public interface INiRef
    {
		int Index { get; set; }
        IList List { get; set; }

		void Clear();
		bool IsEmpty();
        INiRef Clone();
        NiBlockRef<T> CloneRefAs<T>();
        NiBlockPtr<T> ClonePtrAs<T>();
    }
}
