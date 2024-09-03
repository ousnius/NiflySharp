using NiflySharp.Stream;

namespace NiflySharp
{
    public class NiBlockRef<T> : NiRef, INiStreamable
	{
		public NiBlockRef() { }

        public NiBlockRef(int id) { _index = id; }

        public NiBlockRef(NiRef otherRef) { _index = otherRef?.Index ?? NPOS; }

		public override NiBlockRef<T> Clone()
		{
			return new NiBlockRef<T>(this);
        }

        public override NiBlockRef<A> CloneRefAs<A>()
        {
            return new NiBlockRef<A>(this);
        }

        public override NiBlockPtr<A> ClonePtrAs<A>()
        {
            throw new System.NotSupportedException("Cannot clone a block ref as a ptr.");
        }

        public void Sync(NiStreamReversible stream)
		{
			stream.Sync(ref _index);
		}
	}
}
