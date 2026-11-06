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
//    - The number type is fixed to double (NT = double).
//    - Points are indices into a flat coordinate array (d coordinates per point).
//    - std::list<Pit> becomes LinkedList<int>; a null node stands for L.end().
//    - Arithmetic is kept in the same order as the original, so results match it bit for bit.

using System;
using System.Collections.Generic;

namespace NiflySharp.Helpers
{
    /// <summary>
    /// Computes the smallest enclosing ball of a set of points (B. Gaertner, Fast and Robust Smallest Enclosing Balls, ESA 1999).
    /// </summary>
    internal sealed class Miniball
    {
        // std::numeric_limits<double>::epsilon() (not double.Epsilon, which is the smallest denormal)
        private const double MachineEpsilon = 2.220446049250313E-16;

        // data members...
        private readonly int d; // dimension
        private readonly double[] coords; // point i has coordinates coords[i * d] .. coords[i * d + d - 1]
        private readonly int pointCount;

        //...for the algorithms
        private readonly LinkedList<int> L = new();
        private LinkedListNode<int> supportEnd; // null == L.end()
        private int fsize; // number of forced points
        private int ssize; // number of support points

        // ...for the ball updates
        private double[] currentC;
        private double currentSqrR;
        private readonly double[][] c;
        private readonly double[] sqrR;

        // helper arrays
        private readonly double[] q0;
        private readonly double[] z;
        private readonly double[] f;
        private readonly double[][] v;
        private readonly double[][] a;

        // by how much do we allow points outside?
        private readonly double defaultTol;

        /// <summary>
        /// Computes the smallest enclosing ball of the points in <paramref name="coords"/>.
        /// </summary>
        /// <param name="d">Dimension</param>
        /// <param name="coords">Coordinates, <paramref name="d"/> per point (at least one point)</param>
        public Miniball(int d, double[] coords)
        {
            ArgumentNullException.ThrowIfNull(coords);
            if (d <= 0)
                throw new ArgumentOutOfRangeException(nameof(d));
            if (coords.Length == 0 || coords.Length % d != 0)
                throw new ArgumentException("Expected a non-empty list of points with d coordinates each.", nameof(coords));

            this.d = d;
            this.coords = coords;
            pointCount = coords.Length / d;
            supportEnd = L.First;
            currentSqrR = -1.0;
            defaultTol = 10.0 * MachineEpsilon;

            // create_arrays
            c = new double[d + 1][];
            v = new double[d + 1][];
            a = new double[d + 1][];
            for (int i = 0; i < d + 1; ++i)
            {
                c[i] = new double[d];
                v[i] = new double[d];
                a[i] = new double[d];
            }
            sqrR = new double[d + 1];
            q0 = new double[d];
            z = new double[d + 1];
            f = new double[d + 1];

            // set initial center (c[0] is zero-initialized)
            currentC = c[0];

            // compute miniball
            PivotMb(pointCount);
        }

        /// <summary>
        /// Center of the computed ball.
        /// </summary>
        public double[] Center => (double[])currentC.Clone();

        /// <summary>
        /// Squared radius of the computed ball.
        /// </summary>
        public double SquaredRadius => currentSqrR;

        /// <summary>
        /// Number of support points of the computed ball (at most d + 1).
        /// They form a minimal set with the same smallest enclosing ball as the input and lie on its boundary.
        /// </summary>
        public int SupportPointCount => ssize;

        /// <summary>
        /// Indices of the support points.
        /// </summary>
        public IEnumerable<int> SupportPoints
        {
            get
            {
                for (var it = L.First; it != supportEnd; it = it.Next)
                    yield return it.Value;
            }
        }

        /// <summary>
        /// Maximum excess of any input point w.r.t. the computed ball, divided by the squared radius.
        /// The excess of a point is the difference between its squared distance from the center and the squared radius.
        /// Ideally, the return value is 0. <paramref name="subopt"/> is set to the absolute value of the most negative
        /// coefficient in the affine combination of the support points that yields the center (ideally 0).
        /// </summary>
        public double RelativeError(out double subopt)
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
            for (int i = 0; i < pointCount; ++i)
                if ((e = Excess(i)) > maxE)
                    maxE = e;

            subopt = Suboptimality();
            return currentSqrR == 0.0 ? 0.0 : maxE / currentSqrR;
        }

        /// <summary>
        /// True if the relative error is at most 10 times the machine epsilon and the suboptimality is 0.
        /// </summary>
        public bool IsValid()
        {
            return RelativeError(out double suboptimality) <= defaultTol && suboptimality == 0;
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
            if (fsize == d + 1)
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

        private void PivotMb(int n)
        {
            // Algorithm 2: pivot_mb (L_{n-1}), where L_{n-1} = [L.begin, n)
            // --------------------------------------------------------------
            // from B. Gaertner, Fast and Robust Smallest Enclosing Balls, ESA 1999,
            // http://www.inf.ethz.ch/personal/gaertner/texts/own_work/esa99_final.pdf
            double[] center;
            int pivot, k;
            double e, maxE, previousSqrR;
            uint loopsWithoutProgress = 0;
            double bestSqrR = currentSqrR;
            do
            {
                previousSqrR = currentSqrR;

                pivot = 0;
                maxE = 0.0;
                for (k = 0; k != n; ++k)
                {
                    int p = k * d;
                    e = -previousSqrR;
                    center = currentC;
                    for (int j = 0; j < d; ++j)
                        e += Sqr(coords[p + j] - center[j]);
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
        private bool SupportContains(int pit)
        {
            for (var it = L.First; it != supportEnd; it = it.Next)
                if (it.Value == pit)
                    return true;

            return false;
        }

        private void PivotMoveToFront(int j)
        {
            L.AddFirst(j);
            if (Distance(L.First, supportEnd) == d + 2)
                supportEnd = supportEnd == null ? L.Last : supportEnd.Previous; // support_end--
        }

        private double Excess(int pit)
        {
            int p = pit * d;
            double e = -currentSqrR;
            double[] center = currentC;
            for (int k = 0; k < d; ++k)
                e += Sqr(coords[p + k] - center[k]);

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

            int p = pit * d;

            if (fsize == 0)
            {
                for (i = 0; i < d; ++i)
                    q0[i] = coords[p + i];
                for (i = 0; i < d; ++i)
                    c[0][i] = q0[i];
                sqrR[0] = 0.0;
            }
            else
            {
                // set v_fsize to Q_fsize
                for (i = 0; i < d; ++i)
                    v[fsize][i] = coords[p + i] - q0[i];

                // compute the a_{fsize,i}, i< fsize
                for (i = 1; i < fsize; ++i)
                {
                    a[fsize][i] = 0.0;
                    for (j = 0; j < d; ++j)
                        a[fsize][i] += v[i][j] * v[fsize][j];
                    a[fsize][i] *= 2 / z[i];
                }

                // update v_fsize to Q_fsize-\bar{Q}_fsize
                for (i = 1; i < fsize; ++i)
                {
                    for (j = 0; j < d; ++j)
                        v[fsize][j] -= a[fsize][i] * v[i][j];
                }

                // compute z_fsize
                z[fsize] = 0.0;
                for (j = 0; j < d; ++j)
                    z[fsize] += Sqr(v[fsize][j]);
                z[fsize] *= 2;

                // reject push if z_fsize too small
                if (z[fsize] < eps * currentSqrR)
                    return false;

                // update c, sqr_r
                double e = -sqrR[fsize - 1];
                for (i = 0; i < d; ++i)
                    e += Sqr(coords[p + i] - c[fsize - 1][i]);
                f[fsize] = e / z[fsize];

                for (i = 0; i < d; ++i)
                    c[fsize][i] = c[fsize - 1][i] + f[fsize] * v[fsize][i];
                sqrR[fsize] = sqrR[fsize - 1] + e * f[fsize] / 2;
            }
            currentC = c[fsize];
            currentSqrR = sqrR[fsize];
            ssize = ++fsize;
            return true;
        }

        private double Suboptimality()
        {
            var l = new double[d + 1];
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
