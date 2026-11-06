using NiflySharp.Helpers;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace NiflySharp.Structs
{
    public partial struct BoundingSphere
    {
        public Vector3 Center;
        public float Radius;

        public BoundingSphere() { }

        public BoundingSphere(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        /// <summary>
        /// Smallest enclosing sphere of <paramref name="points"/> (Miniball algorithm, as in nifly, in double precision).
        /// </summary>
        /// <param name="points">List of points</param>
        public BoundingSphere(IList<Vector3> points)
        {
            if (points.Count == 0)
                return;

            // Read the vertices in place where possible
            ReadOnlySpan<Vector3> span = points switch
            {
                List<Vector3> list => CollectionsMarshal.AsSpan(list),
                Vector3[] array => array,
                _ => [.. points],
            };

            var mb = new Miniball(span);
            var (x, y, z) = mb.Center;
            Center = new Vector3((float)x, (float)y, (float)z);
            Radius = (float)Math.Sqrt(mb.SquaredRadius);

            // Safety net: Miniball can still miss a point on rare degenerate inputs, and the engine culls
            // a shape whose bounds miss its own geometry. Grow the radius to the farthest point then.
            // Points on the sphere sit a rounding error outside, so a margin keeps solved spheres unchanged.
            float farthestSqr = 0.0f;
            foreach (var p in span)
                farthestSqr = MathF.Max(farthestSqr, Vector3.DistanceSquared(p, Center));

            float farthest = MathF.Sqrt(farthestSqr);
            if (farthest > Radius * (1.0f + 1e-5f))
                Radius = farthest;
        }
    }
}
