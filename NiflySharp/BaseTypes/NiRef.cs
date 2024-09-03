using System.Collections;

namespace NiflySharp
{
    public abstract class NiRef : INiRef
    {
        public const int NPOS = -1;

        protected int _index = NPOS;

        protected IList _list = null;

        public int Index { get => _index; set => _index = value; }

        public IList List { get => _list; set => _list = value; }

        public void Clear()
        {
            Index = NPOS;
        }

        public bool IsEmpty()
        {
            return Index == NPOS;
        }

        public abstract INiRef Clone();
        public abstract NiBlockRef<T> CloneRefAs<T>();
        public abstract NiBlockPtr<T> ClonePtrAs<T>();

        public override bool Equals(object obj)
        {
            return obj is INiRef r && this == r;
        }

        public override int GetHashCode()
        {
            return _index.GetHashCode();
        }

        public static bool operator ==(NiRef a, INiRef b)
        {
            if (ReferenceEquals(a, b))
                return true;

            return (a?.Index ?? NPOS) == (b?.Index ?? NPOS);
        }

        public static bool operator !=(NiRef a, INiRef b)
        {
            return !(a == b);
        }

        public static bool operator ==(NiRef a, int index)
        {
            return a.Index == index;
        }

        public static bool operator !=(NiRef a, int index)
        {
            return !(a == index);
        }
    }
}
