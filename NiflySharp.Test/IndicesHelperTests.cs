using NiflySharp.Helpers;
using NiflySharp.Structs;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NiflySharp.Test
{
    public class IndicesHelperTests
    {
        [Fact(DisplayName = "ApplyMapToTriangles with a ushort map matches the int map overload")]
        public void ApplyMapToTriangles_UShortMap_MatchesIntMap()
        {
            var map = new List<ushort> { 5, 4, 3, 2 };
            var input = new List<Triangle>
            {
                new(0, 1, 2),
                new(1, 4, 2), // index 4 is out of range of the map: deleted
                new(3, 2, 1),
            };

            var trisUShort = new List<Triangle>(input);
            IndicesHelper.ApplyMapToTriangles(ref trisUShort, map, out var deletedUShort);

            var trisInt = new List<Triangle>(input);
            IndicesHelper.ApplyMapToTriangles(ref trisInt, map.Select(e => (int)e).ToList(), out var deletedInt);

            Assert.Equal(new[] { new Triangle(5, 4, 3), new Triangle(2, 3, 4) }, trisUShort);
            Assert.Equal(trisInt, trisUShort);
            Assert.Equal(new[] { 1 }, deletedUShort);
            Assert.Equal(deletedInt, deletedUShort);
        }

        [Fact(DisplayName = "ApplyMapToTriangles with a ushort map rejects null arguments")]
        public void ApplyMapToTriangles_UShortMap_NullThrows()
        {
            var tris = new List<Triangle> { new(0, 1, 2) };
            List<Triangle> nullTris = null;

            Assert.Throws<System.ArgumentNullException>(() => IndicesHelper.ApplyMapToTriangles(ref tris, (List<ushort>)null, out _));
            Assert.Throws<System.ArgumentNullException>(() => IndicesHelper.ApplyMapToTriangles(ref nullTris, new List<ushort> { 0 }, out _));
        }
    }
}
