using System.Collections.Generic;

namespace NiflySharp
{
    public class NiBlockPtrArray<T> : NiBlockRefArray<T> where T : NiObject
    {
        public IEnumerable<INiRef> Pointers
        {
            get
            {
                return _refs;
            }
        }
    }
}
