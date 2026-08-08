using NiflySharp.Blocks;
using NiflySharp.Structs;
using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NiflySharp.Test
{
    public class NifTests
    {
        const string AssetsDirectory = "Assets";
        const string ExpectedDirectory = $"{AssetsDirectory}/Expected";
        const string OutputDirectory = $"{AssetsDirectory}/Output";

        public NifTests()
        {
            Directory.CreateDirectory(OutputDirectory);
        }

        private static bool FilesAreEqual(FileInfo first, FileInfo second)
        {
            const int BYTES_TO_READ = sizeof(long);

            if (first.Length != second.Length)
                return false;

            if (string.Equals(first.FullName, second.FullName, StringComparison.OrdinalIgnoreCase))
                return true;

            int iterations = (int)Math.Ceiling((double)first.Length / BYTES_TO_READ);

            using FileStream fs1 = first.OpenRead();
            using FileStream fs2 = second.OpenRead();
            byte[] one = new byte[BYTES_TO_READ];
            byte[] two = new byte[BYTES_TO_READ];

            for (int i = 0; i < iterations; i++)
            {
                int bytesToRead = (int)Math.Min(BYTES_TO_READ, first.Length - (long)i * BYTES_TO_READ);
                Array.Clear(one);
                Array.Clear(two);
                fs1.ReadExactly(one, 0, bytesToRead);
                fs2.ReadExactly(two, 0, bytesToRead);

                if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                    return false;
            }

            return true;
        }

        /* Manual tests for batch debugging
        [Fact]
        public void LoadAll()
        {
            const string TestName = "LoadAll";
            const string TestDirectory = $"{AssetsDirectory}/{TestName}";

            Assert.True(Directory.Exists(TestDirectory));

            foreach (var file in Directory.EnumerateFiles(TestDirectory, "*.nif", SearchOption.AllDirectories))
            {
                Debug.WriteLine($"Loading '{file}'...");

                var nif = new NifFile();
                Assert.Equal(0, nif.Load(file));
            }
        }

        [Fact]
        public void LoadAndSaveAll()
        {
            const string TestName = "LoadAndSaveAll";
            const string TestDirectory = $"{AssetsDirectory}/{TestName}";

            Assert.True(Directory.Exists(TestDirectory));

            foreach (var file in Directory.EnumerateFiles($"{TestDirectory}/input", "*.nif", SearchOption.AllDirectories))
            {
                Debug.WriteLine($"Loading '{file}'...");

                var nif = new NifFile();
                Assert.Equal(0, nif.Load(file));

                string saveFileName = file.Replace($"{TestDirectory}/input", $"{TestDirectory}/output");
                Debug.WriteLine($"Saving '{saveFileName}'...");
                Directory.CreateDirectory(Path.GetDirectoryName(saveFileName));

                Assert.Equal(0, nif.Save(saveFileName));
                Assert.True(File.Exists(saveFileName));
            }
        }
        */

        [Fact(DisplayName = "Load not existing file")]
        public void Load_NotExisting()
        {
            var nif = new NifFile();
            Assert.NotEqual(0, nif.Load($"{AssetsDirectory}/NotHere.nif"));
        }

        [Fact(DisplayName = "Create and save new file (SE)")]
        public void CreateAndSave_SE()
        {
            const string TestName = "CreateAndSave_SE";

            var nif = new NifFile(NiVersion.GetSSE(), true);

            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save static file (SE)")]
        public void LoadAndSave_Static_SE()
        {
            const string TestName = "LoadAndSave_Static_SE";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/Static.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save static file (FO4)")]
        public void LoadAndSave_Static_FO4()
        {
            const string TestName = "LoadAndSave_Static_FO4";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/130/Static.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save static file (FO4, Version 132)")]
        public void LoadAndSave_Static_FO4_132()
        {
            const string TestName = "LoadAndSave_Static_FO4_132";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/132/Static.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save static file (FO4, Version 139)")]
        public void LoadAndSave_Static_FO4_139()
        {
            const string TestName = "LoadAndSave_Static_FO4_139";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/139/Static.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save skinned file (OB)")]
        public void LoadAndSave_Skinned_OB()
        {
            const string TestName = "LoadAndSave_Skinned_OB";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.0.0.5/11/11/Skinned.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save skinned file (LE)")]
        public void LoadAndSave_Skinned_LE()
        {
            const string TestName = "LoadAndSave_Skinned_LE";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/83/Skinned.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save skinned file (SE)")]
        public void LoadAndSave_Skinned_SE()
        {
            const string TestName = "LoadAndSave_Skinned_SE";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/Skinned.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save skinned file (FO4)")]
        public void LoadAndSave_Skinned_FO4()
        {
            const string TestName = "LoadAndSave_Skinned_FO4";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/130/Skinned.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save skinned file (SF)")]
        public void LoadAndSave_Skinned_SF()
        {
            const string TestName = "LoadAndSave_Skinned_sf";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/172/Skinned.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save skinned, dynamic file (SE)")]
        public void LoadAndSave_SkinnedDynamic_SE()
        {
            const string TestName = "LoadAndSave_SkinnedDynamic_SE";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/SkinnedDynamic.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        /// <summary>
        /// Flattens triangles into their vertex indices, so a failed comparison
        /// shows the differing index instead of an opaque list of triangles.
        /// </summary>
        private static IEnumerable<ushort> VertexIndices(IEnumerable<Triangle> triangles)
        {
            return triangles.SelectMany(tri => new[] { tri.V1, tri.V2, tri.V3 });
        }

        /// <summary>
        /// Replaces the shape's single skin partition with two partitions, giving the first
        /// one the triangles up to <paramref name="splitAt"/> and the second one the rest.
        /// The shipped fixtures only have one partition with an identity vertex map, where
        /// indices into the map and into the shape are the same.
        /// </summary>
        private static NiSkinPartition SplitSkinPartitionInTwo(NifFile nif, INiShape shape, int splitAt)
        {
            var skinInst = nif.GetBlock<NiSkinInstance>(shape.SkinInstanceRef);
            Assert.NotNull(skinInst);

            var skinPart = nif.GetBlock(skinInst.SkinPartition);
            Assert.NotNull(skinPart);
            Assert.Single(skinPart.Partitions);

            var source = skinPart.Partitions[0];
            var partTris = new List<Triangle>(source.TrianglesCopy);

            var first = source;
            first.TrianglesCopy = partTris.GetRange(0, splitAt);
            first.NumTriangles = (ushort)first.TrianglesCopy.Count;

            var second = source;
            second.TrianglesCopy = partTris.GetRange(splitAt, partTris.Count - splitAt);
            second.NumTriangles = (ushort)second.TrianglesCopy.Count;

            skinPart.Partitions = [first, second];
            skinPart.NumPartitions = 2;

            // The partition index of each triangle is derived from the partitions' triangles
            skinPart.GenerateTriPartsFromTrueTriangles(shape.Triangles);

            if (skinInst is BSDismemberSkinInstance dismemberSkinInst)
                dismemberSkinInst.Partitions.Add(dismemberSkinInst.Partitions[0]);

            return skinPart;
        }

        [Fact(DisplayName = "Update skin partitions with unmapped triangle indices (SE)")]
        public void UpdateSkinPartitions_UnmappedIndices_SE()
        {
            const string TestName = "UpdateSkinPartitions_UnmappedIndices_SE";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/Skinned.nif"));

            foreach (var shape in nif.GetShapes())
            {
                var skinPart = SplitSkinPartitionInTwo(nif, shape, shape.Triangles.Count / 2);
                nif.UpdateSkinPartitions(shape);

                // Both partitions must end up with a partial vertex map, or mapped and
                // unmapped indices would be indistinguishable and the test wouldn't gate anything
                Assert.All(skinPart.Partitions, part => Assert.True(part.VertexMap.Count < shape.VertexCount));

                // SE indexes the shape's vertex list, not the partition's vertex map
                Assert.All(skinPart.Partitions, part => Assert.Equal(VertexIndices(part.TrianglesCopy), VertexIndices(part.Triangles)));
            }

            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            // The saved indices are what other tools read, so check them after a round trip
            var nifReloaded = new NifFile();
            Assert.Equal(0, nifReloaded.Load($"{OutputDirectory}/{TestName}.nif"));

            foreach (var shape in nifReloaded.GetShapes())
            {
                var skinInst = nifReloaded.GetBlock<NiSkinInstance>(shape.SkinInstanceRef);
                Assert.NotNull(skinInst);

                var skinPart = nifReloaded.GetBlock(skinInst.SkinPartition);
                Assert.NotNull(skinPart);

                Assert.Equal(2, skinPart.Partitions.Count);
                Assert.All(skinPart.Partitions, part => Assert.True(part.VertexMap.Count < shape.VertexCount));
                Assert.All(skinPart.Partitions, part => Assert.Equal(VertexIndices(part.TrianglesCopy), VertexIndices(part.Triangles)));
            }
        }

        [Fact(DisplayName = "Update skin partitions with a partition without triangles (SE)")]
        public void UpdateSkinPartitions_EmptyPartition_SE()
        {
            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/Skinned.nif"));

            foreach (var shape in nif.GetShapes())
            {
                // The second partition gets no triangles at all
                var skinPart = SplitSkinPartitionInTwo(nif, shape, shape.Triangles.Count);
                nif.UpdateSkinPartitions(shape);

                Assert.Equal(2, skinPart.Partitions.Count);
                Assert.Equal(shape.Triangles.Count, skinPart.Partitions[0].Triangles.Count);
                Assert.Empty(skinPart.Partitions[1].Triangles);
                Assert.Equal(0, skinPart.Partitions[1].NumBones);
            }
        }

        [Fact(DisplayName = "Load and save file with non-zero index root node (LE)")]
        public void LoadAndSave_RootNonZero_LE()
        {
            const string TestName = "LoadAndSave_RootNonZero_LE";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/83/RootNonZero.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save file without weights in NiSkinData (SE)")]
        public void LoadAndSave_NoNiSkinDataWeights_SE()
        {
            const string TestName = "LoadAndSave_NoNiSkinDataWeights_SE";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/NoNiSkinDataWeights.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save animated file (LE)")]
        public void LoadAndSave_Animated_LE()
        {
            const string TestName = "LoadAndSave_Animated_LE";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/83/Animated.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save animated file (FO76)")]
        public void LoadAndSave_Animated_FO76()
        {
            const string TestName = "LoadAndSave_Animated_FO76";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/155/Animated.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save furniture file with collision (SE)")]
        public void LoadAndSave_FurnitureCollision_SE()
        {
            const string TestName = "LoadAndSave_FurnitureCollision_SE";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/FurnitureCollision.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save file with loose blocks (SE)")]
        public void LoadAndSave_LooseBlocks_SE()
        {
            const string TestName = "LoadAndSave_LooseBlocks_SE";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/LooseBlocks.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save file with multi bound node (SE)")]
        public void LoadAndSave_MultiBound_SE()
        {
            const string TestName = "LoadAndSave_MultiBound_SE";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/MultiBound.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save file with ordered node (SE)")]
        public void LoadAndSave_OrderedNode_SE()
        {
            const string TestName = "LoadAndSave_OrderedNode_SE";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/OrderedNode.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load, optimize (LE to SE) and save file")]
        public void Optimize_LE_to_SE()
        {
            const string TestName = "Optimize_LE_to_SE";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/83/Skinned.nif"));

            var optOptions = new NifFileOptimizeOptions()
            {
                TargetVersion = NiVersion.GetSSE(),
                CalculateBounds = false // Bounding sphere calculation produces slightly different values
            };
            nif.OptimizeFor(optOptions);

            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load, optimize (LE to SE, dynamic headparts) and save file")]
        public void Optimize_Dynamic_LE_to_SE()
        {
            const string TestName = "Optimize_Dynamic_LE_to_SE";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/83/SkinnedDynamic.nif"));

            var optOptions = new NifFileOptimizeOptions()
            {
                TargetVersion = NiVersion.GetSSE(),
                HeadPartsOnly = true,
                CalculateBounds = false // Bounding sphere calculation produces slightly different values
            };
            nif.OptimizeFor(optOptions);

            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load, optimize (SE to LE) and save file")]
        public void Optimize_SE_to_LE()
        {
            const string TestName = "Optimize_SE_to_LE";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/Skinned.nif"));

            var optOptions = new NifFileOptimizeOptions()
            {
                TargetVersion = NiVersion.GetSK(),
                CalculateBounds = false // Bounding sphere calculation produces slightly different values
            };
            nif.OptimizeFor(optOptions);

            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load, optimize (SE to LE, dynamic headparts) and save file")]
        public void Optimize_Dynamic_SE_to_LE()
        {
            const string TestName = "Optimize_Dynamic_SE_to_LE";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/SkinnedDynamic.nif"));

            var optOptions = new NifFileOptimizeOptions()
            {
                TargetVersion = NiVersion.GetSK(),
                HeadPartsOnly = true,
                CalculateBounds = false // Bounding sphere calculation produces slightly different values
            };
            nif.OptimizeFor(optOptions);

            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Remove blocks from existing file (SE)")]
        public void RemoveBlock()
        {
            const string TestName = "RemoveBlock";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/Skinned.nif"));

            var blocks = nif.Blocks.OfType<BSTriShape>().ToArray();
            foreach (var block in blocks)
            {
                Assert.True(nif.RemoveBlock(block));
            }

            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save file with recursive union BV (SE)")]
        public void RecursiveUnionBV()
        {
            const string TestName = "UnionBV";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/UnionBV.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save file with deep scene graph (SE)")]
        public void DeepGraph()
        {
            const string TestName = "DeepGraph";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/DeepGraph.nif"));
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Fixes: BSXFlags - add external emittance (SE)")]
        public void FixBSXFlags_AddExtEmit()
        {
            const string TestName = "FixBSXFlags_AddExtEmit";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/FixBSXFlags_AddExtEmit.nif"));
            nif.FixBSXFlags();
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Fixes: BSXFlags - remove external emittance (SE)")]
        public void FixBSXFlags_RemoveExtEmit()
        {
            const string TestName = "FixBSXFlags_RemoveExtEmit";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/FixBSXFlags_RemoveExtEmit.nif"));
            nif.FixBSXFlags();
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Fixes: Shader flags - add environment mapping (SE)")]
        public void FixShaderFlags_AddEnvMap()
        {
            const string TestName = "FixShaderFlags_AddEnvMap";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/FixShaderFlags_AddEnvMap.nif"));
            nif.FixShaderFlags();
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Fixes: Shader flags - remove environment mapping (SE)")]
        public void FixShaderFlags_RemoveEnvMap()
        {
            const string TestName = "FixShaderFlags_RemoveEnvMap";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/FixShaderFlags_RemoveEnvMap.nif"));
            nif.FixShaderFlags();
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Clone file")]
        public void CloneFile()
        {
            const string TestName = "Clone";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/Skinned.nif"));

            var clonedNif = nif.Clone() as NifFile;
            Assert.Equal(0, clonedNif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Clone shape")]
        public void CloneShape()
        {
            const string TestName = "CloneShape";

            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/Skinned.nif"));

            var srcShape = nif.FindBlockByName<INiShape>("cylinder_1");
            var clonedShape = nif.CloneShape(srcShape, "cylinder_cloned");

            var srcSkinInst = nif.GetBlock<NiSkinInstance>(srcShape.SkinInstanceRef);
            var srcSkinData = nif.GetBlock(srcSkinInst.Data);
            var clonedSkinInst = nif.GetBlock<NiSkinInstance>(clonedShape.SkinInstanceRef);
            var clonedSkinData = nif.GetBlock(clonedSkinInst.Data);

            // Make sure the clone is deep and not just a reference copy.
            Assert.NotSame(srcSkinData.BoneList[0].VertexWeights, clonedSkinData.BoneList[0].VertexWeights);

            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Clone shape from file")]
        public void CloneShapeFromFile()
        {
            const string TestName = "CloneShapeFromFile";

            var nif = new NifFile(NiVersion.GetSSE(), withRootNode: true);

            var srcNif = new NifFile();
            Assert.Equal(0, srcNif.Load($"{AssetsDirectory}/V20.2.0.7/12/100/Skinned.nif"));

            var srcShape = srcNif.FindBlockByName<INiShape>("cylinder_1");
            Assert.NotNull(srcShape);

            var clonedShape = nif.CloneShape(srcShape, "cylinder_cloned", srcNif);
            Assert.NotNull(clonedShape);

            Assert.Equal(0, nif.Save($"{OutputDirectory}/{TestName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{TestName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{TestName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        // Morrowind (file version 4.0.0.2) files use inline block types, a boolean size of four
        // bytes, a linked list of extra data and blocks that were removed in later versions.
        const string MorrowindDirectory = $"{AssetsDirectory}/V4.0.0.2";

        private static int GetBlockIndex(NifFile nif, INiObject block)
        {
            Assert.True(nif.GetBlockIndex(block, out int index));
            return index;
        }

        private static void LoadAndSaveMorrowind(string testName, string fileName)
        {
            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{MorrowindDirectory}/{fileName}"));
            Assert.True(nif.Header.Version.IsMW());
            Assert.True(nif.Header.HasInlineBlockTypes);
            Assert.False(nif.HasUnknownBlocks);
            Assert.Equal(0, nif.Save($"{OutputDirectory}/{testName}.nif"));

            var fileInfoOutput = new FileInfo($"{OutputDirectory}/{testName}.nif");
            var fileInfoExpected = new FileInfo($"{ExpectedDirectory}/{testName}.nif");
            Assert.True(FilesAreEqual(fileInfoOutput, fileInfoExpected));
        }

        [Fact(DisplayName = "Load and save static file (MW)")]
        public void LoadAndSave_Static_MW()
        {
            LoadAndSaveMorrowind("LoadAndSave_Static_MW", "Static.nif");
        }

        [Fact(DisplayName = "Load and save billboard file (MW)")]
        public void LoadAndSave_Billboard_MW()
        {
            LoadAndSaveMorrowind("LoadAndSave_Billboard_MW", "Billboard.nif");
        }

        [Fact(DisplayName = "Load and save skinned file (MW)")]
        public void LoadAndSave_Skinned_MW()
        {
            LoadAndSaveMorrowind("LoadAndSave_Skinned_MW", "Skinned.nif");
        }

        [Fact(DisplayName = "Load and save particle file (MW)")]
        public void LoadAndSave_Particles_MW()
        {
            LoadAndSaveMorrowind("LoadAndSave_Particles_MW", "Particles.nif");
        }

        [Fact(DisplayName = "Load and save rotating particle file (MW)")]
        public void LoadAndSave_RotatingParticles_MW()
        {
            LoadAndSaveMorrowind("LoadAndSave_RotatingParticles_MW", "RotatingParticles.nif");
        }

        [Fact(DisplayName = "Load and save UV controller file (MW)")]
        public void LoadAndSave_UVController_MW()
        {
            LoadAndSaveMorrowind("LoadAndSave_UVController_MW", "UVController.nif");
        }

        [Fact(DisplayName = "Load and save texture effect file (MW)")]
        public void LoadAndSave_TextureEffect_MW()
        {
            LoadAndSaveMorrowind("LoadAndSave_TextureEffect_MW", "TextureEffect.nif");
        }

        [Fact(DisplayName = "Load and save morph file (MW)")]
        public void LoadAndSave_Morph_MW()
        {
            LoadAndSaveMorrowind("LoadAndSave_Morph_MW", "Morph.nif");
        }

        [Fact(DisplayName = "Load and save path controller file (MW)")]
        public void LoadAndSave_PathController_MW()
        {
            LoadAndSaveMorrowind("LoadAndSave_PathController_MW", "PathController.nif");
        }

        [Fact(DisplayName = "Load and save animation sequence file (MW)")]
        public void LoadAndSave_Sequence_MW()
        {
            LoadAndSaveMorrowind("LoadAndSave_Sequence_MW", "Sequence.kf");
        }

        [Fact(DisplayName = "Save unmodified file without changes (MW)")]
        public void SaveUnmodified_MW()
        {
            // Every file of the game has to be written back byte for byte when nothing is changed
            string[] fileNames = [
                "Static.nif",
                "Billboard.nif",
                "Skinned.nif",
                "Particles.nif",
                "RotatingParticles.nif",
                "UVController.nif",
                "TextureEffect.nif",
                "Morph.nif",
                "PathController.nif",
                "Sequence.kf"];

            var saveOptions = new NifFileSaveOptions
            {
                RemoveUnreferencedBlocks = false,
                SortBlocks = false,
                UpdateBounds = false
            };

            foreach (var fileName in fileNames)
            {
                byte[] original = File.ReadAllBytes($"{MorrowindDirectory}/{fileName}");

                var nif = new NifFile();
                using var input = new MemoryStream(original, false);
                Assert.Equal(0, nif.Load(input));

                using var output = new MemoryStream();
                Assert.Equal(0, nif.Save(output, saveOptions));

                Assert.True(original.AsSpan().SequenceEqual(output.ToArray()), fileName);
            }
        }

        [Fact(DisplayName = "Read blocks of static file (MW)")]
        public void ReadBlocks_Static_MW()
        {
            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{MorrowindDirectory}/Static.nif"));

            var root = nif.GetRootNode();
            Assert.NotNull(root);
            Assert.Equal("EditorMarker_box_02", root.Name?.String);
            Assert.Equal([0], nif.Header.RootBlockIds);

            // Extra data is a linked list up to file version 4.2.2.0
            var extraData = nif.GetBlock<NiStringExtraData>(root.ExtraData);
            Assert.NotNull(extraData);
            Assert.Equal("MRK", extraData.StringData?.String);
            Assert.True(extraData.NextExtraData.IsEmpty());

            Assert.NotNull(nif.Blocks.OfType<RootCollisionNode>().FirstOrDefault());

            var shapes = nif.GetShapes().ToList();
            Assert.Equal(2, shapes.Count);

            var shape = shapes[0];
            Assert.Equal("Tri EditorMarker_box_02", shape.Name?.String);
            Assert.True(shape.HasVertices);
            Assert.True(shape.VertexCount > 0);

            // Morrowind stores the render state in properties instead of a shader property
            Assert.True(shape.Properties.Count > 0);
            Assert.NotNull(nif.GetBlock<NiMaterialProperty>(shape.Properties.GetBlockRef(0)));
        }

        [Fact(DisplayName = "Read blocks of particle file (MW)")]
        public void ReadBlocks_Particles_MW()
        {
            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{MorrowindDirectory}/Particles.nif"));

            var particleNode = nif.FindBlockByName<NiBSParticleNode>("Blizzard01");
            Assert.NotNull(particleNode);

            var emitterNode = nif.FindBlockByName<NiBSAnimationNode>("Blizzard01 Emitter");
            Assert.NotNull(emitterNode);

            var particles = nif.FindBlockByName<NiAutoNormalParticles>("Blizzard");
            Assert.NotNull(particles);

            var controller = nif.GetBlock<NiParticleSystemController>(particles.Controller);
            Assert.NotNull(controller);
            Assert.Equal(GetBlockIndex(nif, emitterNode), controller.Emitter.Index);

            // The particle modifiers form a linked list
            var gravity = nif.GetBlock<NiGravity>(controller.ParticleModifier);
            Assert.NotNull(gravity);
            Assert.Equal(GetBlockIndex(nif, controller), gravity.Controller.Index);

            var growFade = nif.GetBlock<NiParticleGrowFade>(gravity.NextModifier);
            Assert.NotNull(growFade);
            Assert.True(growFade.NextModifier.IsEmpty());

            var particleData = nif.GetBlock<NiAutoNormalParticlesData>(particles.DataRef);
            Assert.NotNull(particleData);
            Assert.Equal(particleData.NumVertices, particleData.NumParticles);
        }

        [Fact(DisplayName = "Write multiple root references (MW)")]
        public void WriteMultipleRoots_MW()
        {
            var nif = new NifFile();
            Assert.Equal(0, nif.Load($"{MorrowindDirectory}/Static.nif"));

            var collisionNode = nif.Blocks.OfType<RootCollisionNode>().FirstOrDefault();
            Assert.NotNull(collisionNode);

            List<int> rootIds = [0, GetBlockIndex(nif, collisionNode)];
            nif.Header.SetRootBlockIds(rootIds);

            var saveOptions = new NifFileSaveOptions
            {
                RemoveUnreferencedBlocks = false,
                SortBlocks = false,
                UpdateBounds = false
            };

            using var output = new MemoryStream();
            Assert.Equal(0, nif.Save(output, saveOptions));

            var loaded = new NifFile();
            using var input = new MemoryStream(output.ToArray(), false);
            Assert.Equal(0, loaded.Load(input));
            Assert.Equal(rootIds, loaded.Header.RootBlockIds);
        }
    }
}
