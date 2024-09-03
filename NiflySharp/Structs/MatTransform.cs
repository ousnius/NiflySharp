using System.Numerics;

namespace NiflySharp.Structs
{
    public partial struct MatTransform
	{
		public static readonly MatTransform Identity = new()
		{
			Translation = Vector3.Zero,
			Rotation = Matrix3.Identity,
			Scale = 1.0f
		};

		/* On MatTransform and coordinate-system (CS) transformations:

		A MatTransform can represent a "similarity transform", where
		it scales, rotates, and moves geometry; or it can represent a
		"coordinate-system transform", where the geometry itself does
		not change, but its representation changes from one CS to another.

		If CS1 is the source CS and CS2 is the target CS, then:
		ApplyTransform(v) converts a point v represented in CS1 to CS2.
		translation is CS1's origin represented in CS2.
		rotation has columns the basis vectors of CS1 represented in CS2.
		scale gives how much farther apart points appear to be in CS2 than in CS1.

		Note that we do not force "rotation" to actually be a rotation
		matrix.  A rotation matrix's inverse is its transpose.  Instead,
		we only assume "rotation" is invertible, which means its inverse
		must be calculated (using Matrix3::Invert).  Even though we always
		treat "rotation" as a general invertible matrix and not a rotation
		matrix, in practice it is always a rotation matrix.
		*/
		public Vector3 Translation;
		public Matrix3 Rotation;   // must be invertible
		public float Scale;        // must be nonzero

		public MatTransform(MatTransform other)
        {
			Translation = new Vector3(other.Translation.X, other.Translation.Y, other.Translation.Z);
			Rotation = new Matrix3(other.Rotation);
			Scale = other.Scale;
        }
	}
}
