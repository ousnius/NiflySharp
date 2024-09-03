namespace NiflySharp.Structs
{
	/// <summary>
	/// Vector with 2 float components (uv)
	/// </summary>
	public partial struct TexCoord
    {
		public static readonly TexCoord Zero = new();

		public TexCoord(float u, float v)
		{
			U = u;
			V = v;
		}

		public override readonly bool Equals(object obj)
		{
			return obj is TexCoord vec && this == vec;
		}

		public override int GetHashCode()
		{
			return U.GetHashCode() ^ V.GetHashCode();
		}

		public static bool operator ==(TexCoord a, TexCoord b)
		{
			return a.U == b.U && a.V == b.V;
		}

		public static bool operator !=(TexCoord a, TexCoord b)
		{
			return !(a == b);
		}

		public static TexCoord operator +(TexCoord a, TexCoord b)
		{
			return new TexCoord(a.U + b.U, a.V + b.V);
		}

		public static TexCoord operator -(TexCoord a, TexCoord b)
		{
			return new TexCoord(a.U - b.U, a.V - b.V);
        }
    }
}
