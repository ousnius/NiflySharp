using System;

using NiflySharp.Stream;

namespace NiflySharp
{
    public class NiBlockRefShortArray<T> : NiBlockRefArray<T> where T : NiObject
    {
        public override void Sync(NiStreamReversible stream)
        {
            if (_listSizeStream > short.MaxValue)
                throw new OverflowException("Size too large for short list");

            short shortArraySize = (short)_listSizeStream;
            stream.Sync(ref shortArraySize);
            _listSizeStream = shortArraySize;

            Resize(_listSizeStream);

            foreach (var r in _refs)
            {
                r.Sync(stream);
                r.List = _refs;
            }
        }

        public override NiBlockRefShortArray<T> Clone()
        {
            var clone = new NiBlockRefShortArray<T>
            {
                KeepEmptyRefs = KeepEmptyRefs
            };
            clone.Resize(Count);

            for (int i = 0; i < clone.Count; i++)
                clone.SetBlockRef(i, GetBlockRef(i));

            return clone;
        }
    }
}
