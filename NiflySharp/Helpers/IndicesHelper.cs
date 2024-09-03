using NiflySharp.Extensions;
using NiflySharp.Structs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NiflySharp.Helpers
{
    public static class IndicesHelper
    {
		/// <summary>
		/// Calculates the highest index used in the list of <paramref name="triangles"/>.
		/// </summary>
		/// <param name="triangles">List of triangles</param>
		/// <returns>Highest index</returns>
		public static ushort CalcMaxTriangleIndex(IList<Triangle> triangles)
		{
			ushort maxInd = 0;
			for (int i = 0; i < triangles.Count; ++i)
            {
                maxInd = Math.Max(maxInd, triangles[i].V1);
                maxInd = Math.Max(maxInd, triangles[i].V2);
                maxInd = Math.Max(maxInd, triangles[i].V3);
            }

			return maxInd;
		}

        /// <summary>
        /// Generate triangle indices from strips indices.
        /// Strips with less than 3 points are skipped as they cannot become a triangle.
        /// </summary>
        /// <param name="strips">Strips index data</param>
        /// <returns>Triangle indices list</returns>
        public static List<Triangle> GenerateTrianglesFromStrips(List<List<ushort>> strips)
		{
			var tris = new List<Triangle>();

			foreach (var strip in strips)
			{
				if (strip.Count < 3)
					continue;

				ushort a = strip[0];
				ushort b = strip[1];

				for (int i = 2; i < strip.Count; ++i)
				{
					ushort c = strip[i];
					if (a != b && b != c && c != a)
					{
						if ((i & 1) == 0)
							tris.Add(new Triangle(a, b, c));
						else
							tris.Add(new Triangle(a, c, b));
					}

					a = b;
					b = c;
				}
			}

			return tris;
		}

        /// <summary>
        /// Applies a vertex index renumbering map to p1, p2, and p3 of a vector of triangles.
        /// If a triangle has an index out of range of the map
        /// or if an index maps to a negative number, the triangle is removed.
        /// </summary>
        /// <param name="tris">Triangles</param>
        /// <param name="map">Index map</param>
        /// <param name="deletedTris">Deleted triangle indices</param>
        public static void ApplyMapToTriangles(ref List<Triangle> tris, List<ushort> map, out List<int> deletedTris)
		{
			ApplyMapToTriangles(ref tris, map.Select(e => (int)e).ToList(), out deletedTris);
		}

        /// <summary>
        /// Applies a vertex index renumbering map to p1, p2, and p3 of a vector of triangles.
        /// If a triangle has an index out of range of the map
        /// or if an index maps to a negative number, the triangle is removed.
        /// </summary>
        /// <param name="tris">Triangles</param>
        /// <param name="map">Index map</param>
        /// <param name="deletedTris">Deleted triangle indices</param>
        public static void ApplyMapToTriangles(ref List<Triangle> tris, List<int> map, out List<int> deletedTris)
		{
			int mapsz = map.Count;
			int di = 0;

			deletedTris = [];

			for (int si = 0; si < tris.Count; ++si)
			{
				// Triangle's indices are unsigned, but index type might be signed.
				if (tris[si].V1 >= mapsz || tris[si].V2 >= mapsz || tris[si].V3 >= mapsz ||
					map[tris[si].V1] < 0 || map[tris[si].V2] < 0 || map[tris[si].V3] < 0)
				{
					deletedTris.Add(si);
					continue;
				}

				tris[di] = new Triangle
				{
					V1 = (ushort)map[tris[si].V1],
					V2 = (ushort)map[tris[si].V2],
					V3 = (ushort)map[tris[si].V3]
				};
				++di;
			}

			tris = tris.Resize(di);
		}
    }
}
