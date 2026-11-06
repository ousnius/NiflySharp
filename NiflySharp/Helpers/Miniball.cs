//    C# port of Miniball.hpp (smallest enclosing ball of points)
//    Original: Copyright (C) 1999-2021, Bernd Gaertner, November 12, 2021
//    https://people.inf.ethz.ch/gaertner/subdir/software/miniball.html
//    (the same version nifly bundles as external/Miniball.hpp)
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
//    Port notes:
//    - The number type is fixed to double (NT = double) and the dimension to 3.
//    - Points are read from a span of Vector3 without copying (float to double is exact).
//      A point iterator (Pit) is an index into that span.
//    - std::list<Pit> becomes LinkedList<int>; a null node stands for L.end().
//    - Arithmetic is kept in the same order as the original, so results match it bit for bit.
//      Don't use FMA or SIMD dot products here, they round differently.

using System;
using System.Collections.Generic;
using System.Numerics;

namespace NiflySharp.Helpers
{
    /// <summary>
    /// Computes the smallest enclosing ball of a set of 3D points (B. Gaertner, Fast and Robust Smallest Enclosing Balls, ESA 1999).
    /// </summary>
    internal ref struct Miniball
    {
        private const int D = 3; // dimension

        // std::numeric_limits<double>::epsilon() (not double.Epsilon, which is the smallest denormal)
        private const double MachineEpsilon = 2.220446049250313E-16;

        // by how much do we allow points outside?
        private const double DefaultTol = 10.0 * MachineEpsilon;

        // data members...
        private readonly ReadOnlySpan<Vector3> points;

        //...for the algorithms
        private readonly LinkedList<int> L = new();
        private LinkedListNode<int> supportEnd; // null == L.end()
        private int fsize; // number of forced points
        private int ssize; // number of support points

        // ...for the ball updates
        private double[] currentC;
        private double currentSqrR = -1.0;
        private readonly double[][] c = [new double[D], new double[D], new double[D], new double[D]];
        private readonly double[] sqrR = new double[D + 1];

        // helper arrays
        private readonly double[] q0 = new double[D];
        private readonly double[] z = new double[D + 1];
        private readonly double[] f = new double[D + 1];
        private readonly double[][] v = [new double[D], new double[D], new double[D], new double[D]];
        private readonly double[][] a = [new double[D], new double[D], new double[D], new double[D]];

        /// <summary>
        /// Computes the smallest enclosing ball of <paramref name="points"/>.
        /// The span is only read during construction and <see cref="RelativeError"/>.
        /// </summary>
        /// <param name="points">Points (at least one)</param>
        public Miniball(ReadOnlySpan<Vector3> points)
        {
            if (points.IsEmpty)
                throw new ArgumentException("Expected at least one point.", nameof(points));

            this.points = points;
            supportEnd = L.First;

            // set initial center (c[0] is zero-initialized)
            currentC = c[0];

            // compute miniball
            PivotMb();
        }

        /// <summary>
        /// Center of the computed ball.
        /// </summary>
        public readonly (double X, double Y, double Z) Center => (currentC[0], currentC[1], currentC[2]);

        /// <summary>
        /// Squared radius of the computed ball.
        /// </summary>
        public readonly double SquaredRadius => currentSqrR;

        /// <summary>
        /// Number of support points of the computed ball (at most 4).
        /// They form a minimal set with the same smallest enclosing ball as the input and lie on its boundary.
        /// </summary>
        public readonly int SupportPointCount => ssize;

        /// <summary>
        /// Indices of the support points.
        /// </summary>
        public readonly int[] SupportPoints
        {
            get
            {
                var result = new int[ssize];
                int n = 0;
                for (var it = L.First; it != supportEnd; it = it.Next)
                    result[n++] = it.Value;
                return result;
            }
        }

        /// <summary>
        /// Maximum excess of any input point w.r.t. the computed ball, divided by the squared radius.
        /// The excess of a point is the difference between its squared distance from the center and the squared radius.
        /// Ideally, the return value is 0. <paramref name="subopt"/> is set to the absolute value of the most negative
        /// coefficient in the affine combination of the support points that yields the center (ideally 0).
        /// </summary>
        public readonly double RelativeError(out double subopt)
        {
            double e, maxE = 0.0;
            // compute maximum absolute excess of support points
            for (var it = L.First; it != supportEnd; it = it.Next)
            {
                e = Excess(it.Value);
                if (e < 0.0) e = -e;
                if (e > maxE)
                    maxE = e;
            }
            // compute maximum excess of any point
            for (int i = 0; i < points.Length; ++i)
                if ((e = Excess(i)) > maxE)
                    maxE = e;

            subopt = Suboptimality();
            return currentSqrR == 0.0 ? 0.0 : maxE / currentSqrR;
        }

        /// <summary>
        /// True if the relative error is at most 10 times the machine epsilon and the suboptimality is 0.
        /// </summary>
        public readonly bool IsValid()
        {
            return RelativeError(out double suboptimality) <= DefaultTol && suboptimality == 0;
        }

        private static double Sqr(double r) => r * r;

        private static int Distance(LinkedListNode<int> first, LinkedListNode<int> last)
        {
            int count = 0;
            for (var it = first; it != last; it = it.Next)
                ++count;
            return count;
        }

        private void MtfMb(LinkedListNode<int> n)
        {
            // Algorithm 1: mtf_mb (L_{n-1}, B), where L_{n-1} = [L.begin, n)
            // B: the set of forced points, defining the current ball
            // S: the superset of support points computed by the algorithm
            // --------------------------------------------------------------
            // from B. Gaertner, Fast and Robust Smallest Enclosing Balls, ESA 1999,
            // http://www.inf.ethz.ch/personal/gaertner/texts/own_work/esa99_final.pdf

            //   PRE: B = S
            supportEnd = L.First;
            if (fsize == D + 1)
                return;

            // incremental construction
            for (var i = L.First; i != n;)
            {
                // INV: (support_end - L.begin() == |S|-|B|)
                var j = i;
                i = i.Next;
                if (Excess(j.Value) > 0.0)
                {
                    if (Push(j.Value))
                    {          // B := B + p_i
                        MtfMb(j);            // mtf_mb (L_{i-1}, B + p_i)
                        Pop();               // B := B - p_i
                        MtfMoveToFront(j);
                    }
                }
            }
            // POST: the range [L.begin(), support_end) stores the set S\B
        }

        private void MtfMoveToFront(LinkedListNode<int> j)
        {
            if (supportEnd == j)
                supportEnd = j.Next;

            // L.splice(L.begin(), L, j)
            L.Remove(j);
            L.AddFirst(j);
        }

        private void PivotMb()
        {
            // Algorithm 2: pivot_mb (L_{n-1}), where L_{n-1} = [L.begin, n)
            // --------------------------------------------------------------
            // from B. Gaertner, Fast and Robust Smallest Enclosing Balls, ESA 1999,
            // http://www.inf.ethz.ch/personal/gaertner/texts/own_work/esa99_final.pdf
            uint loopsWithoutProgress = 0;
            double bestSqrR = currentSqrR;
            do
            {
                double previousSqrR = currentSqrR;

                // find the point with maximum excess (the center doesn't change in this loop)
                int pivot = 0;
                double maxE = 0.0;
                double cx = currentC[0], cy = currentC[1], cz = currentC[2];
                for (int k = 0; k < points.Length; ++k)
                {
                    var p = points[k];
                    double e = -previousSqrR;
                    e += Sqr(p.X - cx);
                    e += Sqr(p.Y - cy);
                    e += Sqr(p.Z - cz);
                    if (e > maxE)
                    {
                        maxE = e;
                        pivot = k;
                    }
                }

                if (previousSqrR < 0.0 || maxE > 0.0)
                {
                    // check if the pivot is already contained in the support set
                    if (!SupportContains(pivot))
                    {
                        if (Push(pivot))
                        {
                            MtfMb(supportEnd);
                            Pop();
                            PivotMoveToFront(pivot);
                        }
                    }
                }
                if (bestSqrR < currentSqrR)
                {
                    bestSqrR = currentSqrR;
                    loopsWithoutProgress = 0;
                }
                else
                    ++loopsWithoutProgress;
            } while (loopsWithoutProgress < 2);
        }

        // std::find(L.begin(), support_end, pivot) != support_end
        private readonly bool SupportContains(int pit)
        {
            for (var it = L.First; it != supportEnd; it = it.Next)
                if (it.Value == pit)
                    return true;

            return false;
        }

        private void PivotMoveToFront(int j)
        {
            L.AddFirst(j);
            if (Distance(L.First, supportEnd) == D + 2)
                supportEnd = supportEnd == null ? L.Last : supportEnd.Previous; // support_end--
        }

        private readonly double Excess(int pit)
        {
            var p = points[pit];
            double e = -currentSqrR;
            e += Sqr(p.X - currentC[0]);
            e += Sqr(p.Y - currentC[1]);
            e += Sqr(p.Z - currentC[2]);
            return e;
        }

        private void Pop()
        {
            --fsize;
        }

        private bool Push(int pit)
        {
            int i, j;
            double eps = Sqr(MachineEpsilon);

            var p = points[pit];

            if (fsize == 0)
            {
                for (i = 0; i < D; ++i)
                    q0[i] = p[i];
                for (i = 0; i < D; ++i)
                    c[0][i] = q0[i];
                sqrR[0] = 0.0;
            }
            else
            {
                // set v_fsize to Q_fsize
                for (i = 0; i < D; ++i)
                    v[fsize][i] = p[i] - q0[i];

                // compute the a_{fsize,i}, i< fsize
                for (i = 1; i < fsize; ++i)
                {
                    a[fsize][i] = 0.0;
                    for (j = 0; j < D; ++j)
                        a[fsize][i] += v[i][j] * v[fsize][j];
                    a[fsize][i] *= 2 / z[i];
                }

                // update v_fsize to Q_fsize-\bar{Q}_fsize
                for (i = 1; i < fsize; ++i)
                {
                    for (j = 0; j < D; ++j)
                        v[fsize][j] -= a[fsize][i] * v[i][j];
                }

                // compute z_fsize
                z[fsize] = 0.0;
                for (j = 0; j < D; ++j)
                    z[fsize] += Sqr(v[fsize][j]);
                z[fsize] *= 2;

                // reject push if z_fsize too small
                if (z[fsize] < eps * currentSqrR)
                    return false;

                // update c, sqr_r
                double e = -sqrR[fsize - 1];
                for (i = 0; i < D; ++i)
                    e += Sqr(p[i] - c[fsize - 1][i]);
                f[fsize] = e / z[fsize];

                for (i = 0; i < D; ++i)
                    c[fsize][i] = c[fsize - 1][i] + f[fsize] * v[fsize][i];
                sqrR[fsize] = sqrR[fsize - 1] + e * f[fsize] / 2;
            }
            currentC = c[fsize];
            currentSqrR = sqrR[fsize];
            ssize = ++fsize;
            return true;
        }

        private readonly double Suboptimality()
        {
            Span<double> l = stackalloc double[D + 1];
            double minL = 0.0;
            l[0] = 1.0;
            for (int i = ssize - 1; i > 0; --i)
            {
                l[i] = f[i];
                for (int k = ssize - 1; k > i; --k)
                    l[i] -= a[k][i] * l[k];
                if (l[i] < minL) minL = l[i];
                l[0] -= l[i];
            }
            if (l[0] < minL) minL = l[0];
            if (minL < 0.0)
                return -minL;
            return 0.0;
        }
    }
}
