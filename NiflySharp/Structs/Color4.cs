namespace NiflySharp.Structs
{
    /// <summary>
    /// Color with 4 float components (RGBA)
    /// </summary>
    public partial struct Color4
    {
		public static readonly Color4 Zero = new();

        public Color4(float r, float g, float b, float a)
		{
			R = r;
			G = g;
			B = b;
            A = a;
        }

        public override readonly bool Equals(object obj)
		{
			return obj is Color4 c && this == c;
		}

		public override int GetHashCode()
		{
			return R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode() ^ A.GetHashCode();
		}

		public static bool operator ==(Color4 a, Color4 b)
		{
			return a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
		}

		public static bool operator !=(Color4 a, Color4 b)
		{
			return !(a == b);
        }
    }
}
