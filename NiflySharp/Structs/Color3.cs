namespace NiflySharp.Structs
{
    /// <summary>
    /// Color with 3 float components (RGB)
    /// </summary>
    public partial struct Color3
    {
		public static readonly Color3 Zero = new();

        public Color3(float r, float g, float b)
		{
			R = r;
			G = g;
			B = b;
        }

        public override readonly bool Equals(object obj)
		{
			return obj is Color3 c && this == c;
		}

		public override int GetHashCode()
		{
			return R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode();
		}

		public static bool operator ==(Color3 a, Color3 b)
		{
			return a.R == b.R && a.G == b.G && a.B == b.B;
		}

		public static bool operator !=(Color3 a, Color3 b)
		{
			return !(a == b);
        }
    }
}
