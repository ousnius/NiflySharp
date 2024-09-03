using NiflySharp.Stream;
using NiflySharp.Structs;
using System.Collections.Generic;

namespace NiflySharp.Blocks
{
    public partial class BSSubIndexTriShape
    {
        public List<BSGeometrySegmentData> Segments { get => _segment; set => _segment = value; }
        public BSGeometrySegmentSharedData SegmentData { get => _segmentData; set => _segmentData = value; }

        public new void BeforeSync(NiStreamReversible stream)
        {
            if (stream.CurrentMode == NiStreamReversible.Mode.Write)
            {
                if (stream.Version.StreamVersion >= 130 && _dataSize > 0)
                    _numPrimitives = (uint)(_triangles?.Count ?? 0);
                else
                    _numPrimitives = 0;
            }
        }
    }
}
