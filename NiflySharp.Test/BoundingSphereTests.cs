using NiflySharp.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;

namespace NiflySharp.Test
{
    public class BoundingSphereTests
    {
        private static float Outside(BoundingSphere sphere, IEnumerable<Vector3> points) =>
            points.Max(p => Vector3.Distance(p, sphere.Center) - sphere.Radius);

        [Fact(DisplayName = "BoundingSphere contains all points of a three-point set")]
        public void ThreePoints_AllContained()
        {
            // On main the sphere is centered at (0.5, 0.5, 0) with radius 0.707 and leaves
            // (1, -1, 1) about 1.16 outside.
            var points = new List<Vector3> { new(1, 0, 0), new(1, -1, 1), new(0, 1, 0) };

            var sphere = new BoundingSphere(points);

            Assert.True(Outside(sphere, points) <= 1e-5f);
        }

        [Fact(DisplayName = "BoundingSphere contains all points of random small sets")]
        public void RandomSmallSets_AllContained()
        {
            // About 4% of these sets have a point outside the sphere on main.
            var rng = new Random(1);
            for (int iter = 0; iter < 20000; iter++)
            {
                int count = rng.Next(3, 10), range = rng.Next(1, 11);
                var points = Enumerable.Range(0, count)
                    .Select(_ => new Vector3(rng.Next(-range, range + 1), rng.Next(-range, range + 1), rng.Next(-range, range + 1)))
                    .ToList();

                var sphere = new BoundingSphere(points);

                Assert.True(Outside(sphere, points) <= 1e-5f * MathF.Max(1, sphere.Radius),
                    $"set {iter}: {string.Join(" ", points)}");
            }
        }

        [Fact(DisplayName = "BoundingSphere of a correctly solved set is unchanged")]
        public void SolvedSet_Unchanged()
        {
            // Opposite corners of a cube: the minimal sphere is centered at the origin.
            var points = new List<Vector3> { new(-1, -1, -1), new(1, 1, 1), new(1, -1, 1), new(-1, 1, -1) };

            var sphere = new BoundingSphere(points);

            Assert.Equal(MathF.Sqrt(3), sphere.Radius, 4);
            Assert.True(sphere.Center.Length() < 1e-4f);
        }
    }
}
