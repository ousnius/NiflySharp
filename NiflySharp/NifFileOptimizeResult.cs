using System.Collections.Generic;

namespace NiflySharp
{
    public struct NifFileOptimizeResult
    {
        /// <summary>
        /// Indicates if versions are unsupported for the optimization process
        /// </summary>
        public bool VersionMismatch = false;

        /// <summary>
        /// Indicates if there were duplicate shape names that have been renamed
        /// </summary>
        public bool DuplicatesRenamed = false;

        /// <summary>
        /// Shapes that had their vertex colors removed
        /// </summary>
        public List<INiShape> ShapesVertexColorsRemoved = [];

        /// <summary>
        /// Shapes that had their normals removed
        /// </summary>
        public List<INiShape> ShapesNormalsRemoved = [];

        /// <summary>
        /// Shapes that had their partitions triangulated
        /// </summary>
        public List<INiShape> ShapesPartitionsTriangulated = [];

        /// <summary>
        /// Shapes that received missing tangents/bitangents
        /// </summary>
        public List<INiShape> ShapesTangentsAdded = [];

        /// <summary>
        /// Shapes that had their parallax settings
        /// </summary>
        public List<INiShape> ShapesParallaxRemoved = [];

        public NifFileOptimizeResult()
        {
        }
    }
}
