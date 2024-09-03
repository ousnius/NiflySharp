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
        }
    }
}
