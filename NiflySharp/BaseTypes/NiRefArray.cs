using NiflySharp.Stream;
using System.Collections.Generic;

namespace NiflySharp
{
    public abstract class NiRefArray : INiStreamable
	{
		protected int _listSizeStream = 0;

		public abstract int Count { get; }

        public bool KeepEmptyRefs { get; set; }

		public void SetListSize(NiStreamReversible stream, int size)
		{
			_listSizeStream = size;
			Resize(_listSizeStream);
		}

        public abstract void Clear();

		public abstract void Resize(int size);

		public abstract void Sync(NiStreamReversible stream);

		public abstract void AddBlockRef(int id);

		public abstract int GetBlockRef(int id);

		public abstract void SetBlockRef(int id, int index);

		public abstract void RemoveBlockRef(int id);

        /// <summary>
        /// Removes all refs pointing to block <paramref name="blockIndex"/>.
        /// If <see cref="KeepEmptyRefs"/> is set, the refs are cleared instead so that positions of the other refs are kept.
        /// </summary>
        /// <param name="blockIndex">Block index the refs point to</param>
        /// <returns>Number of refs removed or cleared</returns>
        public abstract int RemoveRefsTo(int blockIndex);

		public abstract IEnumerable<int> Indices { get; }

        public abstract IEnumerable<NiRef> References { get; }

        public abstract void SetIndices(List<int> indices);

        public abstract int CleanInvalidRefs();

        public abstract NiRefArray Clone();
    }
}
