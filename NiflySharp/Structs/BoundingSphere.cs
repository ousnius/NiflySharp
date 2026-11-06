using NiflySharp.Helpers;
using System;
using System.Collections.Generic;
using System.Numerics;

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

            var coords = new double[points.Count * 3];
            for (int i = 0; i < points.Count; i++)
            {
                coords[i * 3] = points[i].X;
                coords[i * 3 + 1] = points[i].Y;
                coords[i * 3 + 2] = points[i].Z;
            }

            var mb = new Miniball(3, coords);
            var center = mb.Center;
            Center = new Vector3((float)center[0], (float)center[1], (float)center[2]);
            Radius = (float)Math.Sqrt(mb.SquaredRadius);

            // Safety net: Miniball can still miss a point on rare degenerate inputs, and the engine culls
            // a shape whose bounds miss its own geometry. Grow the radius to the farthest point then.
            // Points on the sphere sit a rounding error outside, so a margin keeps solved spheres unchanged.
            float farthest = 0.0f;
            for (int i = 0; i < points.Count; i++)
                farthest = MathF.Max(farthest, Vector3.Distance(points[i], Center));

            if (farthest > Radius * (1.0f + 1e-5f))
                Radius = farthest;
        }
    }
}
