using NiflySharp.Blocks;
using System;
using System.Diagnostics;
using System.IO;
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
                fs1.Read(one, 0, BYTES_TO_READ);
                fs2.Read(two, 0, BYTES_TO_READ);

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
    }
}
