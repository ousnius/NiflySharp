using NiflySharp.Helpers;
using NiflySharp.Structs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Xunit;

namespace NiflySharp.Test
{
    public class MiniballTests
    {
        const string ReferenceFile = "Assets/Miniball/MiniballReference.txt";

        private static double[] Flatten(IEnumerable<Vector3> points) =>
            points.SelectMany(p => new double[] { p.X, p.Y, p.Z }).ToArray();

        private static List<Vector3> Points(params float[] xyz) =>
            Enumerable.Range(0, xyz.Length / 3).Select(i => new Vector3(xyz[i * 3], xyz[i * 3 + 1], xyz[i * 3 + 2])).ToList();

        /// <summary>
        /// Exact smallest enclosing ball by brute force, independent of Miniball: the smallest ball through
        /// 1 to 4 of the points (center in their affine hull) that contains all points.
        /// </summary>
        private static (Vector3D Center, double SqrRadius) BruteForceMinBall(IReadOnlyList<Vector3D> pts)
        {
            var best = (Center: new Vector3D(), SqrRadius: double.MaxValue);
            int n = pts.Count;

            void Try(int[] subset)
            {
                if (!SmallestBallThrough(subset.Select(i => pts[i]).ToArray(), out var c, out double r2) || r2 >= best.SqrRadius)
                    return;

                double tol = 1e-9 * Math.Max(1.0, r2);
                if (pts.All(p => (p - c).LengthSquared <= r2 + tol))
                    best = (c, r2);
            }

            for (int a = 0; a < n; a++)
            {
                Try([a]);
                for (int b = a + 1; b < n; b++)
                {
                    Try([a, b]);
                    for (int c = b + 1; c < n; c++)
                    {
                        Try([a, b, c]);
                        for (int d = c + 1; d < n; d++)
                            Try([a, b, c, d]);
                    }
                }
            }
            return best;
        }

        // Center = q0 + sum(l_i * u_i) with u_i = s_i - q0 and |center - s_i| = |center - q0|, i.e. G l = |u|^2 / 2
        private static bool SmallestBallThrough(Vector3D[] s, out Vector3D center, out double sqrRadius)
        {
            center = s[0];
            sqrRadius = 0.0;
            int k = s.Length - 1;
            if (k == 0)
                return true;

            var u = Enumerable.Range(1, k).Select(i => s[i] - s[0]).ToArray();
            var m = new double[k, k + 1];
            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < k; j++)
                    m[i, j] = u[i].Dot(u[j]);
                m[i, k] = u[i].LengthSquared / 2;
            }

            // Gaussian elimination with partial pivoting
            double scale = Enumerable.Range(0, k).Max(i => m[i, i]);
            for (int col = 0; col < k; col++)
            {
                int pivot = Enumerable.Range(col, k - col).MaxBy(r => Math.Abs(m[r, col]));
                if (Math.Abs(m[pivot, col]) <= 1e-9 * Math.Max(scale, 1e-300))
                    return false; // affinely dependent points

                for (int j = 0; j <= k; j++)
                    (m[col, j], m[pivot, j]) = (m[pivot, j], m[col, j]);
                for (int r = 0; r < k; r++)
                {
                    if (r == col)
                        continue;
                    double factor = m[r, col] / m[col, col];
                    for (int j = col; j <= k; j++)
                        m[r, j] -= factor * m[col, j];
                }
            }

            for (int i = 0; i < k; i++)
                center += u[i] * (m[i, k] / m[i, i]);
            sqrRadius = (center - s[0]).LengthSquared;
            return true;
        }

        private readonly record struct Vector3D(double X, double Y, double Z)
        {
            public static Vector3D operator -(Vector3D a, Vector3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
            public static Vector3D operator +(Vector3D a, Vector3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
            public static Vector3D operator *(Vector3D a, double f) => new(a.X * f, a.Y * f, a.Z * f);
            public double Dot(Vector3D b) => X * b.X + Y * b.Y + Z * b.Z;
            public double LengthSquared => Dot(this);
            public static implicit operator Vector3D(Vector3 v) => new(v.X, v.Y, v.Z);
        }

        private static IEnumerable<List<Vector3>> RandomIntegerSets(int seed, int count)
        {
            var rng = new Random(seed);
            for (int iter = 0; iter < count; iter++)
            {
                int n = rng.Next(3, 10), range = rng.Next(1, 11);
                yield return Enumerable.Range(0, n)
                    .Select(_ => new Vector3(rng.Next(-range, range + 1), rng.Next(-range, range + 1), rng.Next(-range, range + 1)))
                    .ToList();
            }
        }

        private static IEnumerable<List<Vector3>> RandomBoxSets(int seed, int count, int interior)
        {
            var rng = new Random(seed);
            Vector3 RandPt() => new((float)(rng.NextDouble() * 200 - 100), (float)(rng.NextDouble() * 200 - 100), (float)(rng.NextDouble() * 200 - 100));
            for (int t = 0; t < count; t++)
            {
                var size = RandPt();
                var origin = RandPt();
                var p = Enumerable.Range(0, 8).Select(i => origin + size * new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1)).ToList();
                p.AddRange(Enumerable.Range(0, interior).Select(_ => origin + size * new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble())));
                yield return p.OrderBy(_ => rng.Next()).ToList();
            }
        }

        [Fact(DisplayName = "Miniball matches nifly's Miniball.hpp bit for bit")]
        public void MatchesNativeMiniball()
        {
            var inv = CultureInfo.InvariantCulture;
            int checkedSets = 0;
            foreach (var line in File.ReadLines(ReferenceFile).Where(l => !l.StartsWith('#')))
            {
                var parts = line.Split('|');
                var input = parts[0].Split(' ').Select(x => float.Parse(x, inv)).ToArray();
                var expected = parts[1].Split(' ').Select(x => double.Parse(x, inv)).ToArray();
                var coords = input.Skip(1).Take((int)input[0] * 3).Select(x => (double)x).ToArray();

                var mb = new Miniball(3, coords);
                var c = mb.Center;

                Assert.True(c[0] == expected[0] && c[1] == expected[1] && c[2] == expected[2] && Math.Sqrt(mb.SquaredRadius) == expected[3],
                    $"{parts[0]}: got ({c[0]:R}, {c[1]:R}, {c[2]:R}) r={Math.Sqrt(mb.SquaredRadius):R}, expected {parts[1]}");
                checkedSets++;
            }
            Assert.Equal(1064, checkedSets);
        }

        [Theory(DisplayName = "Miniball solves known cases exactly")]
        [InlineData(new float[] { 1, 0, 0, 1, -1, 1, 0, 1, 0 }, 0.5, 0, 0.5, 1.5)]                              // seb (hbf/miniball#48) misses (1, -1, 1)
        [InlineData(new float[] { 1, 1, 1, 0, 0, 0, 2, 0, 0, 0, 2, 0, 2, 2, 0, 0, 0, 2, 2, 0, 2, 0, 2, 2, 2, 2, 2 }, 1, 1, 1, 3)] // cube, center first
        [InlineData(new float[] { 0, 0, 0, 2, 0, 0, 0, 2, 0, 2, 2, 0, 0, 0, 2, 2, 0, 2, 0, 2, 2, 2, 2, 2, 1, 1, 1 }, 1, 1, 1, 3)] // cube, center last
        [InlineData(new float[] { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, -2, 0 }, 0, -0.5, 0, 2.25)]            // hbf/miniball#41
        [InlineData(new float[] { 3, -4, 5 }, 3, -4, 5, 0)]                                                     // single point
        [InlineData(new float[] { 2, 2, 2, 2, 2, 2, 2, 2, 2 }, 2, 2, 2, 0)]                                     // identical points
        [InlineData(new float[] { -1, 0, 0, 3, 0, 0 }, 1, 0, 0, 4)]                                             // two points
        [InlineData(new float[] { 0, 0, 0, 1, 1, 1, 4, 4, 4, 2, 2, 2 }, 2, 2, 2, 12)]                           // collinear
        public void KnownCases(float[] xyz, double cx, double cy, double cz, double sqrRadius)
        {
            var mb = new Miniball(3, xyz.Select(x => (double)x).ToArray());
            var c = mb.Center;

            Assert.Equal(cx, c[0], 12);
            Assert.Equal(cy, c[1], 12);
            Assert.Equal(cz, c[2], 12);
            Assert.Equal(sqrRadius, mb.SquaredRadius, 12);
            Assert.InRange(mb.SupportPointCount, 1, 4);
        }

        [Fact(DisplayName = "Miniball support points lie on the ball")]
        public void SupportPointsOnBoundary()
        {
            foreach (var pts in RandomIntegerSets(7, 2000))
            {
                var coords = Flatten(pts);
                var mb = new Miniball(3, coords);
                var c = mb.Center;

                Assert.Equal(mb.SupportPointCount, mb.SupportPoints.Count());
                foreach (int i in mb.SupportPoints)
                {
                    double d2 = Math.Pow(coords[i * 3] - c[0], 2) + Math.Pow(coords[i * 3 + 1] - c[1], 2) + Math.Pow(coords[i * 3 + 2] - c[2], 2);
                    Assert.Equal(mb.SquaredRadius, d2, 1e-9 * Math.Max(1, mb.SquaredRadius));
                }
            }
        }

        [Fact(DisplayName = "Miniball equals the brute-force smallest enclosing ball")]
        public void MatchesBruteForce()
        {
            var sets = RandomIntegerSets(11, 5000).Concat(RandomBoxSets(12, 300, 2));
            foreach (var pts in sets)
            {
                var mb = new Miniball(3, Flatten(pts));
                var c = mb.Center;
                var (expectedCenter, expectedSqrRadius) = BruteForceMinBall(pts.Select(p => (Vector3D)p).ToList());

                double tol = 1e-7 * Math.Max(1, expectedSqrRadius);
                Assert.True(Math.Abs(mb.SquaredRadius - expectedSqrRadius) <= tol,
                    $"{string.Join(" ", pts)}: r^2 {mb.SquaredRadius:R}, brute force {expectedSqrRadius:R}");
                Assert.True((new Vector3D(c[0], c[1], c[2]) - expectedCenter).LengthSquared <= tol);
            }
        }

        [Fact(DisplayName = "Miniball rejects invalid input")]
        public void InvalidInput()
        {
            Assert.Throws<ArgumentNullException>(() => new Miniball(3, null));
            Assert.Throws<ArgumentException>(() => new Miniball(3, []));
            Assert.Throws<ArgumentException>(() => new Miniball(3, [1, 2]));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Miniball(0, [1, 2, 3]));
        }

        [Fact(DisplayName = "BoundingSphere is the smallest sphere containing all points")]
        public void BoundingSphere_SmallestAndContaining()
        {
            var sets = RandomIntegerSets(21, 3000).Concat(RandomBoxSets(22, 300, 2)).Concat(RandomBoxSets(23, 300, 42));
            foreach (var pts in sets)
            {
                var sphere = new BoundingSphere(pts);

                float outside = pts.Max(p => Vector3.Distance(p, sphere.Center) - sphere.Radius);
                Assert.True(outside <= 1e-5f * MathF.Max(1, sphere.Radius), $"{string.Join(" ", pts)}: {outside} outside");

                if (pts.Count <= 10)
                {
                    double minimal = Math.Sqrt(BruteForceMinBall(pts.Select(p => (Vector3D)p).ToList()).SqrRadius);
                    Assert.True(sphere.Radius <= minimal * (1 + 1e-5) + 1e-6, $"{string.Join(" ", pts)}: radius {sphere.Radius}, minimal {minimal}");
                }
            }
        }

        [Fact(DisplayName = "BoundingSphere keeps all points inside where Miniball itself misses one")]
        public void BoundingSphere_SafetyNet()
        {
            // Miniball.hpp (and this port) leaves (-1, 0, -1) about 0.47 outside for this set
            var pts = Points(0, -1, 0, -1, 0, -1, 1, -1, -1, 1, 1, 0, -1, -1, 0, -1, 0, 1, 0, 1, 1, -1, -1, 1);
            var mb = new Miniball(3, Flatten(pts));
            Assert.True(mb.RelativeError(out _) > 0.1);

            var sphere = new BoundingSphere(pts);

            Assert.All(pts, p => Assert.True(Vector3.Distance(p, sphere.Center) <= sphere.Radius * (1 + 1e-5f)));
        }

        [Fact(DisplayName = "BoundingSphere of no points is empty")]
        public void BoundingSphere_Empty()
        {
            var sphere = new BoundingSphere(new List<Vector3>());

            Assert.Equal(Vector3.Zero, sphere.Center);
            Assert.Equal(0.0f, sphere.Radius);
        }
    }
}
