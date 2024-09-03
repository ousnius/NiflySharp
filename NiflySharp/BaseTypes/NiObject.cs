using NiflySharp.Stream;
using System;
using System.Collections.Generic;

namespace NiflySharp
{
    public class NiObject : INiObject
    {
        protected int blockSize = 0;
        protected uint groupId = 0;

        public virtual IEnumerable<INiRef> References
        {
            get
            {
                return Array.Empty<INiRef>();
            }
        }

        public virtual IEnumerable<INiRef> Pointers
        {
            get
            {
                return Array.Empty<INiRef>();
            }
        }

        public virtual IEnumerable<NiRefArray> ReferenceArrays
        {
            get
            {
                return Array.Empty<NiRefArray>();
            }
        }

        public virtual IEnumerable<NiStringRef> StringRefs
        {
            get
            {
                return Array.Empty<NiStringRef>();
            }
        }

        public void BeforeSync(NiStreamReversible stream) { }
        public void AfterSync(NiStreamReversible stream) { }

        public virtual void Sync(NiStreamReversible stream)
        {
            if (stream.Version.FileVersion >= NiFileVersion.V10_0_0_0 && stream.Version.FileVersion < NiFileVersion.V10_1_0_114)
                stream.Sync(ref groupId);
        }
    }
}
