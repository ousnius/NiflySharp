using NiflySharp.Stream;

namespace NiflySharp
{
    public class NiBlockPtr<T> : NiPtr, INiStreamable
    {
        public NiBlockPtr() { }

        public NiBlockPtr(int id) { _index = id; }

        public NiBlockPtr(NiPtr otherPtr) { _index = otherPtr?.Index ?? NPOS; }

        public override NiBlockPtr<T> Clone()
        {
            return new NiBlockPtr<T>(this);
        }

        public override NiBlockRef<A> CloneRefAs<A>()
        {
            throw new System.NotSupportedException("Cannot clone a block ptr as a ref.");
        }

        public override NiBlockPtr<A> ClonePtrAs<A>()
        {
            return new NiBlockPtr<A>(this);
        }

        public void Sync(NiStreamReversible stream)
        {
            stream.Sync(ref _index);
        }
    }
}
