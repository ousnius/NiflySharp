using SEB;
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
        /// Miniball algorithm
        /// </summary>
        /// <param name="points">List of points</param>
        public BoundingSphere(IList<Vector3> points)
        {
            if (points.Count == 0)
                return;

            var pointSet = new ArrayPointSet(3, points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                pointSet.Set(i, 0, points[i].X);
                pointSet.Set(i, 1, points[i].Y);
                pointSet.Set(i, 2, points[i].Z);
            }

            var mb = new Miniball(pointSet);
            Center = new Vector3((float)mb.Center[0], (float)mb.Center[1], (float)mb.Center[2]);
            Radius = (float)mb.Radius;

            // Miniball can return a sphere that leaves points outside (seen most often with small
            // sets), and the engine culls a shape whose bounds miss its own geometry. Grow the
            // radius to the farthest point then. Points on the sphere sit a rounding error outside,
            // so a margin keeps correctly solved spheres exactly as Miniball returned them.
            float farthest = 0.0f;
            for (int i = 0; i < points.Count; i++)
                farthest = System.MathF.Max(farthest, Vector3.Distance(points[i], Center));

            if (farthest > Radius * (1.0f + 1e-5f))
                Radius = farthest;
        }
    }
}
