using System.Numerics;

namespace NiflySharp.Structs
{
    public partial struct Matrix3
	{
		public static readonly Matrix3 Identity =
			new(
				new Vector3(1.0f, 0.0f, 0.0f),
				new Vector3(0.0f, 1.0f, 0.0f),
				new Vector3(0.0f, 0.0f, 1.0f));

		public Vector3[] Rows;

		public Matrix3(Vector3 r1, Vector3 r2, Vector3 r3)
        {
			Rows = [r1, r2, r3];
        }

		public Matrix3(Matrix3 other)
        {
			Rows =
            [
                other.Rows[0],
				other.Rows[1],
				other.Rows[2]
			];
        }

        public override readonly bool Equals(object obj)
        {
            return obj is Matrix3 mat && this == mat;
        }

        public override readonly int GetHashCode()
        {
			return Rows[0].GetHashCode() ^ Rows[1].GetHashCode() ^ Rows[2].GetHashCode();
        }

        public readonly bool IsIdentity()
		{
			return this == Identity;
		}

		public static bool operator ==(Matrix3 a, Matrix3 b)
		{
			return a.Rows[0] == b.Rows[0] && a.Rows[1] == b.Rows[1] && a.Rows[2] == b.Rows[2];
		}

		public static bool operator !=(Matrix3 a, Matrix3 b)
		{
			return !(a == b);
		}
	}
}
