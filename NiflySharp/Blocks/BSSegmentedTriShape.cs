using NiflySharp.Structs;
using System.Collections.Generic;

namespace NiflySharp.Blocks
{
    public partial class BSSegmentedTriShape : INiShape
    {
        public List<BSGeometrySegmentData> Segments { get => _segment; set => _segment = value; }
    }
}
