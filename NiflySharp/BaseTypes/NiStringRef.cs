using NiflySharp.Stream;
using System.Text;

namespace NiflySharp
{
    public class NiStringRef : INiStreamable
    {
        public const int NPOS = -1;

        protected int _index = NPOS; // Temporary index storage for load/save

        protected string _str = string.Empty;

        public int Index { get => _index; set => _index = value; }

        public int Length { get => _str.Length; }

        public string String { get => _str; set => _str = value; }

        public NiStringRef()
        {
        }

        public NiStringRef(string str)
        {
            _str = str;
        }

        public void Clear()
        {
            Index = NPOS;
            _str = string.Empty;
        }

        public void Sync(NiStreamReversible stream)
        {
            if (stream.Version.FileVersion < NiFileVersion.V20_1_0_3)
            {
                int sz = _str.Length;
                stream.Sync(ref sz);

                const int maxLength = 2048;

                if (sz > maxLength)
                    sz = maxLength;

                var buf = new byte[sz];

                if (stream.CurrentMode == NiStreamReversible.Mode.Write)
                {
                    if (_str.Length > maxLength)
                        buf = Encoding.Latin1.GetBytes(_str[..maxLength]);
                    else
                        buf = Encoding.Latin1.GetBytes(_str);
                }

                stream.Sync(ref buf);

                if (stream.CurrentMode == NiStreamReversible.Mode.Read)
                    _str = Encoding.Latin1.GetString(buf);
            }
            else
                stream.Sync(ref _index);
        }

        public override bool Equals(object obj)
        {
            return obj is NiStringRef r && String == r.String;
        }

        public override int GetHashCode()
        {
            return _str.GetHashCode();
        }
    }
}
