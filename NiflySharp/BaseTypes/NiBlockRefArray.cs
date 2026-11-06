using NiflySharp.Stream;
using System.Collections.Generic;
using System.Linq;

namespace NiflySharp
{
    public class NiBlockRefArray<T> : NiRefArray where T : NiObject
    {
        protected List<NiBlockRef<T>> _refs = [];

        public override int Count => _refs.Count;

        public NiBlockRefArray()
        {
        }

        public NiBlockRefArray(List<NiBlockRef<T>> refs)
        {
            _refs = refs;
        }

        public override void Clear()
        {
            _refs.Clear();
            _listSizeStream = 0;
            KeepEmptyRefs = false;
        }

        public override void Resize(int size)
        {
            int cur = _refs.Count;
            if (size < cur)
            {
                _refs.RemoveRange(size, cur - size);
            }
            else if (size > cur)
            {
                if (size > _refs.Capacity)
                    _refs.Capacity = size;

                // Fill only the new slots from `cur` up to `size`
                for (int i = cur; i < size; i++)
                    _refs.Add(new NiBlockRef<T>() { List = _refs });
            }

            _listSizeStream = size;
        }

        public void SyncContent(NiStreamReversible stream)
        {
            foreach (var r in _refs)
            {
                r.Sync(stream);
                r.List = _refs;
            }
        }

        public override void Sync(NiStreamReversible stream)
        {
            stream.Sync(ref _listSizeStream);
            Resize(_listSizeStream);

            foreach (var r in _refs)
            {
                r.Sync(stream);
                r.List = _refs;
            }
        }

        public override void AddBlockRef(int id)
        {
            _refs.Add(new NiBlockRef<T>(id) { List = _refs });
            _listSizeStream = _refs.Count;
        }

        public override int GetBlockRef(int id)
        {
            if (id != NiRef.NPOS && _refs.Count > id)
                return _refs[id].Index;

            return NiRef.NPOS;
        }

        public override void SetBlockRef(int id, int index)
        {
            if (id != NiRef.NPOS && _refs.Count > id)
                _refs[id].Index = index;
        }

        public override void RemoveBlockRef(int id)
        {
            if (id != NiRef.NPOS && _refs.Count > id)
            {
                _refs.RemoveAt(id);
                _listSizeStream = _refs.Count;
            }
        }

        public override int RemoveRefsTo(int blockIndex)
        {
            if (blockIndex == NiRef.NPOS)
                return 0;

            if (!KeepEmptyRefs)
            {
                int removed = _refs.RemoveAll(r => r.Index == blockIndex);
                _listSizeStream = _refs.Count;
                return removed;
            }

            int cleared = 0;
            foreach (var r in _refs)
            {
                if (r.Index == blockIndex)
                {
                    r.Clear();
                    cleared++;
                }
            }
            return cleared;
        }

        public override IEnumerable<int> Indices
        {
            get
            {
                return _refs.Select(r => r.Index);
            }
        }

        public override IEnumerable<NiBlockRef<T>> References
        {
            get
            {
                return _refs;
            }
        }

        public IEnumerable<T> GetBlocks(NifFile file)
        {
            foreach (var r in _refs)
            {
                var block = file.GetBlock(r);
                if (block != null)
                    yield return block;
            }
        }

        public override void SetIndices(List<int> indices)
        {
            Resize(indices.Count);

            for (int i = 0; i < _listSizeStream; i++)
                _refs[i].Index = indices[i];
        }

        public override int CleanInvalidRefs()
        {
            if (!KeepEmptyRefs)
                _refs.RemoveAll(r => r.IsEmpty());

            _listSizeStream = _refs.Count;
            return _listSizeStream;
        }

        public override NiBlockRefArray<T> Clone()
        {
            var clone = new NiBlockRefArray<T>
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
