using NiflySharp.Blocks;
using NiflySharp.Enums;
using NiflySharp.Extensions;
using NiflySharp.Interfaces;
using NiflySharp.Stream;
using NiflySharp.Structs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NiflySharp
{
    /// <summary>
    /// Main class for NIF files
    /// </summary>
    public class NifFile
    {
        /// <summary>
        /// Header of the file
        /// </summary>
        public NiHeader Header { get; protected set; }

        /// <summary>
        /// List of all blocks currently in the file
        /// </summary>
        public List<INiObject> Blocks { get; protected set; }

        /// <summary>
        /// Valid file has been loaded or created
        /// </summary>
        public bool Valid { get; protected set; }

        /// <summary>
        /// File was loaded with unknown blocks
        /// </summary>
        public bool HasUnknownBlocks { get; protected set; }

        /// <summary>
        /// File is a terrain file type
        /// </summary>
        public bool IsTerrainFile { get; protected set; }

        /// <summary>
        /// Default constructor for completely empty file.
        /// Header values and contents need to be created.
        /// </summary>
        public NifFile() { }

        /// <summary>
        /// Create empty file using the version of <paramref name="file"/>, <paramref name="user"/> and <paramref name="stream"/>.
        /// </summary>
        /// <param name="file">File version enum</param>
        /// <param name="user">User version</param>
        /// <param name="stream">Stream version</param>
        /// <param name="withRootNode">Add a root node</param>
        public NifFile(NiFileVersion file, uint user, uint stream, bool withRootNode = false)
        {
            var version = new NiVersion(file, user, stream);
            Create(version, withRootNode);
        }

        /// <summary>
        /// Create empty file using the version <paramref name="version"/>.
        /// </summary>
        /// <param name="version">Version</param>
        /// <param name="withRootNode">Add a root node</param>
        public NifFile(NiVersion version, bool withRootNode = false)
        {
            Create(version, withRootNode);
        }

        /// <summary>
        /// Load a NIF file from <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="options">Load options (optional)</param>
        public NifFile(System.IO.Stream stream, NifFileLoadOptions options = null)
        {
            Load(stream, options);
        }

        /// <summary>
        /// Creates an empty NIF file using the version <paramref name="version"/>
        /// </summary>
        /// <param name="version">Version</param>
        /// <param name="withRootNode">Add a root node</param>
        public void Create(NiVersion version, bool withRootNode = false)
        {
            Clear();

            Header = new NiHeader(version);
            Blocks = [];

            if (withRootNode)
            {
                var rootNode = new NiNode()
                {
                    Name = new NiStringRef("Scene Root")
                };
                AddBlock(rootNode);
            }

            Valid = true;
        }

        /// <summary>
        /// Clears all contents of the file including the header.
        /// </summary>
        public void Clear()
        {
            Valid = false;
            HasUnknownBlocks = false;
            IsTerrainFile = false;

            Blocks?.Clear();
            Header?.Clear();
        }

        /// <summary>
        /// Load a NIF file from file <paramref name="fileName"/>.
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="options">Load options (optional)</param>
        /// <returns>
        /// Result code:
        ///  0 = success
        /// >0 = error
        /// </returns>
        public int Load(string fileName, NifFileLoadOptions options = null)
        {
            FileStream file;
            try
            {
                file = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            }
            catch
            {
                return 1;
            }

            return Load(file, options);
        }

        /// <summary>
        /// Load a NIF file from <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="options">Load options (optional)</param>
        /// <returns>
        /// Result code:
        ///  0 = success
        /// >0 = error
        /// </returns>
        public int Load(System.IO.Stream stream, NifFileLoadOptions options = null)
        {
            Clear();

            if (stream == null || !stream.CanRead)
                return 1;

            options ??= new();
            IsTerrainFile = options.IsTerrainFile;

            Header = new NiHeader();

            var streamReader = new NiStreamReader(stream, this);
            Header.Read(streamReader);

            if (!Header.Valid)
            {
                // No valid header was read
                Clear();
                return 1;
            }
            
            /*
            if (!(Header.Version.FileVersion >= NiVersion.ToFile(20, 2, 0, 7) && (Header.Version.UserVersion == 11 || Header.Version.UserVersion == 12)))
            {
                // Only load Bethesda files right now
                Clear();
                return 2;
            }
            */

            var streamReversible = new NiStreamReversible(streamReader);

            Blocks = new List<INiObject>(Header.BlockCount);

            for (int i = 0; i < Header.BlockCount; i++)
            {
                // Get block type name for block id
                string blockTypeStr = Header.GetBlockTypeNameById(i);
                if (blockTypeStr == null)
                {
                    // Read block type string directly from stream
                    var nistr = new NiString4();
                    nistr.Sync(streamReversible);
                    blockTypeStr = nistr.Content;
                }

                NiObject block = null;
                INiStreamable blockStreamable = null;

                try
                {
                    // Create a new default instance of the block type
                    var blockType = Type.GetType("NiflySharp.Blocks." + blockTypeStr);
                    blockStreamable = Activator.CreateInstance(blockType) as INiStreamable;
                }
                catch
                {
                    // Block type is unknown
                    HasUnknownBlocks = true;
                    block = new NiUnknown(streamReversible, Header.GetBlockSize(i));
                }

                if (blockStreamable != null)
                {
                    // Read the block
                    //streamReversible.Argument = null;
                    blockStreamable.Sync(streamReversible);
                    block = blockStreamable as NiObject;
                }

                if (block != null)
                    Blocks.Add(block);
            }

            PrepareData();
            Valid = true;
            return 0;
        }

        /// <summary>
        /// Save a NIF file to file <paramref name="fileName"/>.
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="options">Save options (optional)</param>
        /// <returns>
        /// Result code:
        ///  0 = success
        /// >0 = error
        /// </returns>
        public int Save(string fileName, NifFileSaveOptions options = null)
        {
            FileStream file;
            try
            {
                file = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
            }
            catch
            {
                return 1;
            }

            int result = Save(file, options);

            file.Close();
            file.Dispose();

            return result;
        }

        /// <summary>
        /// Save a NIF file to stream <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="options">Save options (optional)</param>
        /// <returns>
        /// Result code:
        ///  0 = success
        /// >0 = error
        /// </returns>
        public int Save(System.IO.Stream stream, NifFileSaveOptions options = null)
        {
            if (stream == null || !stream.CanWrite || !stream.CanSeek)
                return 1;

            options ??= new();

            var streamWriter = new NiStreamWriter(stream, this);

            FinalizeData();

            if (options.RemoveUnreferencedBlocks)
                RemoveUnreferencedBlocks();

            if (options.SortBlocks)
                PrettySortBlocks();

            if (options.UpdateBounds)
            {
                foreach (var shape in GetShapes())
                {
                    shape.UpdateBounds();
                }
            }

            Header.Write(streamWriter);

            var streamReversible = new NiStreamReversible(streamWriter);
            long blockStartPos = streamWriter.Writer.BaseStream.Position;

            // Retrieve block sizes from stream after writing each block
            var blockSizes = new List<long>(Blocks.Count);
            foreach (var block in Blocks.OfType<INiStreamable>())
            {
                if (Header.Version.FileVersion < NiFileVersion.V5_0_0_1)
                {
                    // Write block type name
                    var nistr = new NiString4(block.GetType().Name);
                    nistr.Sync(streamReversible);
                }

                // Write block
                block.Sync(streamReversible);

                // Calculate new block size
                long blockEndPos = streamWriter.Writer.BaseStream.Position;
                blockSizes.Add(blockEndPos - blockStartPos);
                blockStartPos = blockEndPos;
            }

            // End padding
            streamWriter.Writer.Write(1);
            streamWriter.Writer.Write(0);

            // Get previous stream pos of block size array and overwrite
            if (streamWriter.BlockSizePos != 0)
            {
                streamWriter.Writer.BaseStream.Seek(streamWriter.BlockSizePos, SeekOrigin.Begin);

                blockSizes.ForEach(bs => streamWriter.Writer.Write((int)bs));
                streamWriter.BlockSizePos = 0;
            }

            return 0;
        }

        public void LinkGeometryData()
        {
            foreach (var geom in Blocks.OfType<NiGeometry>())
            {
                geom.GeometryData = GetBlock(geom.DataRef);
            }
        }

        /// <summary>
        /// Prepare internal data after loading.
        /// </summary>
        public void PrepareData()
        {
            Header.FillStringRefs(Blocks);
            LinkGeometryData();

            // FIXME: Make this an option?
            //TrimTexturePaths();

            foreach (var shape in GetShapes().ToList())
            {
                if (Header.Version.IsSSE())
                {
                    if (shape is not BSTriShape bsTriShape)
                        continue;

                    var skinInst = GetBlock<NiSkinInstance>(bsTriShape.SkinInstanceRef);
                    if (skinInst == null)
                        continue;

                    var skinPart = GetBlock(skinInst.SkinPartition);
                    if (skinPart == null)
                        continue;

                    bsTriShape.SetVertexDataSSE(skinPart.VertexData);

                    var tris = new List<Triangle>();

                    for (int pi = 0; pi < skinPart.Partitions.Count; ++pi)
                    {
                        foreach (var tri in skinPart.Partitions[pi].TrianglesCopy)
                        {
                            tris.Add(tri);
                            skinPart.triParts.Add(pi);
                        }
                    }

                    bsTriShape.SetTriangles(Header.Version, tris);

                    if (shape is BSDynamicTriShape bsDynamicTriShape)
                    {
                        if (bsDynamicTriShape.VertexDataSSE != null)
                        {
                            if (bsDynamicTriShape.VertexDataSSE.Count != bsDynamicTriShape.Vertices.Count)
                                return;

                            var vertexDataSpan = CollectionsMarshal.AsSpan(bsDynamicTriShape.VertexDataSSE);

                            for (int i = 0; i < bsDynamicTriShape.VertexCount; i++)
                            {
                                vertexDataSpan[i].Vertex.X = bsDynamicTriShape.Vertices[i].X;
                                vertexDataSpan[i].Vertex.Y = bsDynamicTriShape.Vertices[i].Y;
                                vertexDataSpan[i].Vertex.Z = bsDynamicTriShape.Vertices[i].Z;
                                vertexDataSpan[i].BitangentX = bsDynamicTriShape.Vertices[i].W;
                            }
                        }
                        else if (bsDynamicTriShape.VertexData != null)
                        {
                            if (bsDynamicTriShape.VertexData.Count != bsDynamicTriShape.Vertices.Count)
                                return;

                            var vertexDataSpan = CollectionsMarshal.AsSpan(bsDynamicTriShape.VertexData);

                            for (int i = 0; i < bsDynamicTriShape.VertexCount; i++)
                            {
                                if (bsDynamicTriShape.IsFullPrecision)
                                {
                                    vertexDataSpan[i].Vertex.X = bsDynamicTriShape.Vertices[i].X;
                                    vertexDataSpan[i].Vertex.Y = bsDynamicTriShape.Vertices[i].Y;
                                    vertexDataSpan[i].Vertex.Z = bsDynamicTriShape.Vertices[i].Z;
                                    vertexDataSpan[i].BitangentX = bsDynamicTriShape.Vertices[i].W;
                                }
                                else
                                {
                                    vertexDataSpan[i].VertexHalf.X = (Half)bsDynamicTriShape.Vertices[i].X;
                                    vertexDataSpan[i].VertexHalf.Y = (Half)bsDynamicTriShape.Vertices[i].Y;
                                    vertexDataSpan[i].VertexHalf.Z = (Half)bsDynamicTriShape.Vertices[i].Z;
                                    vertexDataSpan[i].BitangentXHalf = (Half)bsDynamicTriShape.Vertices[i].W;
                                }
                            }
                        }
                    }
                }

                if (Header.Version.IsOB() && shape.GeometryData != null)
                {
                    // Move tangents and bitangents from binary extra data to shape
                    if (GetBinaryTangentData(shape, out var tangents, out var bitangents) != null)
                    {
                        shape.GeometryData.Tangents = tangents;
                        shape.GeometryData.Bitangents = bitangents;

                        // Remove tangents flag again but keep data stored
                        shape.GeometryData.SetTangentsFlag(false);
                    }
                }
            }

            // FIXME: Make this an option?
            //RemoveInvalidTris();
        }

        /// <summary>
        /// Finalize internal data before saving.
        /// </summary>
        public void FinalizeData()
        {
            foreach (var block in Blocks)
            {
                foreach (var refArray in block.ReferenceArrays)
                {
                    refArray.CleanInvalidRefs();
                }
            }

            foreach (var bsDynTriShape in GetShapes().OfType<BSDynamicTriShape>())
            {
                bsDynTriShape.CalcDynamicData();
            }

            foreach (var shape in GetShapes().ToList())
            {
                if (shape is BSTriShape bsTriShape)
                {
                    bsTriShape.CalcDataSizes(Header.Version);

                    if (Header.Version.IsSSE())
                    {
                        // Move triangle and vertex data from shape to partition
                        var skinInst = GetBlock<NiSkinInstance>(bsTriShape.SkinInstanceRef);
                        if (skinInst != null)
                        {
                            var skinPart = GetBlock(skinInst.SkinPartition);
                            if (skinPart != null)
                            {
                                skinPart.DataSize = bsTriShape.DataSize;
                                skinPart.VertexSize = bsTriShape.VertexSize;
                                skinPart.SetVertexData(bsTriShape.VertexDataSSE);
                                skinPart.VertexDesc = new(bsTriShape.VertexDesc.Value);

                                var partitionsSpan = CollectionsMarshal.AsSpan(skinPart.Partitions);

                                foreach (ref var part in partitionsSpan)
                                {
                                    part.VertexDesc = new(bsTriShape.VertexDesc.Value);
                                }
                            }
                        }
                    }
                }

                if (Header.Version.IsOB() && shape.GeometryData != null)
                {
                    // Move tangents and bitangents from shape back to binary extra data
                    if (shape.GeometryData.Tangents?.Count > 0 && shape.GeometryData.Bitangents?.Count > 0)
                        SetBinaryTangentData(shape, shape.GeometryData.Tangents, shape.GeometryData.Bitangents);
                    else
                        DeleteBinaryTangentData(shape);
                }
            }

            Header.UpdateHeaderStrings(Blocks, HasUnknownBlocks);
        }

        /// <summary>
        /// Gets a block reference using the block's index.
        /// </summary>
        /// <typeparam name="T">Type of the block</typeparam>
        /// <param name="blockId">Block index</param>
        /// <returns>Block reference of supplied type or null</returns>
        public T GetBlock<T>(int blockId) where T : class
        {
            if (blockId == NiRef.NPOS || blockId >= Header.BlockCount)
                return null;

            return Blocks[blockId] as T;
        }

        /// <summary>
        /// Gets a block reference using a NiRef object.
        /// </summary>
        /// <typeparam name="T">Type of the block</typeparam>
        /// <param name="niRef">NiRef object</param>
        /// <returns>Block reference of supplied type or null</returns>
        public T GetBlock<T>(INiRef niRef) where T : class
        {
            if (niRef == null)
                return null;

            return GetBlock<T>(niRef.Index);
        }

        /// <summary>
        /// Gets a block reference using a NiBLockRef object.
        /// </summary>
        /// <typeparam name="T">Type of the block</typeparam>
        /// <param name="niBlockRef">NiBLockRef object with type</param>
        /// <returns>Block reference of supplied type or null</returns>
        public T GetBlock<T>(NiBlockRef<T> niBlockRef) where T : class
        {
            if (niBlockRef == null)
                return null;

            return GetBlock<T>(niBlockRef.Index);
        }

        /// <summary>
        /// Find a block by its name.
        /// </summary>
        /// <typeparam name="T">Type of the block</typeparam>
        /// <param name="name">Name of the block</param>
        /// <returns>Block reference of supplied type or null</returns>
        public T FindBlockByName<T>(string name) where T : NiObject, INiNamed
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var block = Blocks.OfType<T>().FirstOrDefault(b => b.Name?.String == name);
            return block;
        }

        /// <summary>
        /// Returns true and the block index if found.
        /// </summary>
        /// <param name="block">Block</param>
        /// <param name="index">Block index (output)</param>
        /// <returns>Block found</returns>
        public bool GetBlockIndex(INiObject block, out int index)
        {
            index = Blocks.IndexOf(block);
            return index != -1;
        }

        /// <summary>
        /// Gets the main root NiNode block
        /// </summary>
        /// <returns>NiNode or null</returns>
        public NiNode GetRootNode()
        {
            // Check if block at index 0 is a node
            var root = GetBlock<NiNode>(0);

            // Not a node, look for first node block
            root ??= Blocks.OfType<NiNode>().FirstOrDefault();

            return root;
        }

        /// <summary>
        /// Gets all root NiNode blocks
        /// </summary>
        /// <returns>List of root nodes</returns>
        public List<NiNode> GetRootNodes()
        {
            var nodes = new List<NiNode>();

            foreach (var node in Blocks.OfType<NiNode>())
            {
                if (GetParentBlock(node) == null)
                    nodes.Add(node);
            }

            return nodes;
        }

        /// <summary>
        /// Gets the parent NiNode block of <paramref name="child"/>
        /// This is the first node referencing block <paramref name="child"/>.
        /// </summary>
        /// <param name="child">Child block</param>
        /// <returns>NiNode or null</returns>
        public NiNode GetParentNode(INiObject child)
        {
            if (!GetBlockIndex(child, out int childIndex))
                return null;

            var nodes = Blocks.OfType<NiNode>().Where(n => n != child);
            return nodes.FirstOrDefault(n => n.Children.Indices.Contains(childIndex));
        }

        /// <summary>
        /// Gets the parent block of <paramref name="child"/>.
        /// This is the first block referencing block <paramref name="child"/>.
        /// </summary>
        /// <param name="child">Child block</param>
        /// <returns>Parent block or null</returns>
        public INiObject GetParentBlock(INiObject child)
        {
            if (!GetBlockIndex(child, out int childIndex))
                return null;

            var blocks = Blocks.Where(n => n != child);
            return blocks.FirstOrDefault(b => b.References.Any(r => r.Index == childIndex));
        }

        /// <summary>
        /// Enumerate all shape blocks.
        /// </summary>
        /// <returns>Enumeration of INiShape blocks</returns>
        public IEnumerable<INiShape> GetShapes()
        {
            return Blocks.OfType<INiShape>();
        }

        /// <summary>
        /// Returns the shader block assigned to <paramref name="shape"/>.
        /// </summary>
        /// <param name="shape">Shape</param>
        /// <returns>Shader block or null</returns>
        public INiShader GetShader(INiShape shape)
        {
            if (shape == null)
                return null;

            var shader = GetBlock<INiShader>(shape.ShaderPropertyRef);
            if (shader != null)
                return shader;
            
            if (shape.Properties != null)
            {
                foreach (var propRef in shape.Properties.References)
                {
                    var shaderProp = GetBlock<INiShader>(propRef);
                    if (shaderProp != null)
                    {
                        // Prefer other shader block types to NiMaterialProperty
                        if (shaderProp is not NiMaterialProperty)
                            return shaderProp;

                        shader = shaderProp;
                    }
                }
            }

            return shader;
        }

        /// <summary>
        /// Returns the first property block of type <typeparamref name="T"/> assigned to <paramref name="shape"/>.
        /// </summary>
        /// <param name="shape">Shape</param>
        /// <returns><typeparamref name="T"/> block or null</returns>
        public T GetPropertyOfType<T>(INiShape shape) where T : NiProperty
        {
            if (shape == null)
                return null;

            foreach (var propRef in shape.Properties.References)
            {
                var prop = GetBlock<T>(propRef);
                if (prop != null)
                    return prop;
            }

            return null;
        }

        /// <summary>
        /// Check if block with supplied index is referenced in any block.
        /// </summary>
        /// <param name="index">Block index</param>
        /// <param name="includePtrs">Include block pointers in reference check</param>
        public bool IsBlockReferenced(int index, bool includePtrs = true)
        {
            foreach (var b in Blocks)
            {
                if (b.References.Any(r => r != null && r.Index == index))
                    return true;

                if (includePtrs)
                {
                    if (b.Pointers.Any(p => p != null && p.Index == index))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if block with supplied index is referenced in any block.
        /// </summary>
        /// <param name="block">Block</param>
        /// <param name="includePtrs">Include block pointers in reference check</param>
        public bool IsBlockReferenced(NiObject block, bool includePtrs = true)
        {
            if (!GetBlockIndex(block, out int index))
                return false;

            return IsBlockReferenced(index, includePtrs);
        }

        /// <summary>
        /// Add a new block <paramref name="newBlock"/> to the file.
        /// </summary>
        /// <param name="newBlock">New block</param>
        /// <returns>New block index</returns>
        public int AddBlock(NiObject newBlock)
        {
            Header.AddBlockInfo(newBlock);
            Blocks.Add(newBlock);
            return Header.BlockCount - 1;
        }

        /// <summary>
        /// Replaces block at id <paramref name="oldBlockId"/> with a new block <paramref name="newBlock"/>.
        /// </summary>
        /// <param name="oldBlockId">Old block id to replace</param>
        /// <param name="newBlock">New block</param>
        /// <returns>Successfully replaced</returns>
        public bool ReplaceBlock(int oldBlockId, INiObject newBlock)
        {
            if (!Header.ReplaceBlockInfo(oldBlockId, newBlock))
                return false;

            Blocks[oldBlockId] = newBlock;
            return true;
        }

        /// <summary>
        /// Replaces block <paramref name="oldBlock"/> with a new block <paramref name="newBlock"/>.
        /// </summary>
        /// <param name="oldBlock">Old block to replace</param>
        /// <param name="newBlock">New block</param>
        /// <returns>Successfully replaced</returns>
        public bool ReplaceBlock(INiObject oldBlock, INiObject newBlock)
        {
            if (!GetBlockIndex(oldBlock, out int oldBlockId))
                return false;

            if (!Header.ReplaceBlockInfo(oldBlockId, newBlock))
                return false;

            Blocks[oldBlockId] = newBlock;
            return true;
        }

        /// <summary>
        /// Removes block at <paramref name="index"/>
        /// </summary>
        /// <param name="index">Index of block to remove</param>
        public bool RemoveBlock(int index)
        {
            if (!Header.RemoveBlockInfo(index))
                return false;

            try
            {
                Blocks.RemoveAt(index);
            }
            catch
            {
                return false;
            }

            // Remove/adjust block references and pointers
            foreach (var b in Blocks)
            {
                foreach (var r in b.References.Where(r => r != null && !r.IsEmpty()))
                {
                    if (r.Index == index)
                    {
                        r.List?.Remove(r);
                        r.Clear();
                    }
                    else if (r.Index > index)
                        r.Index--;
                }

                foreach (var p in b.Pointers.Where(p => p != null && !p.IsEmpty()))
                {
                    if (p.Index == index)
                    {
                        p.List?.Remove(p);
                        p.Clear();
                    }
                    else if (p.Index > index)
                        p.Index--;
                }
            }

            return true;
        }

        /// <summary>
        /// Removes block <paramref name="block"/>
        /// </summary>
        /// <param name="block">Block to remove</param>
        public bool RemoveBlock(NiObject block)
        {
            if (!GetBlockIndex(block, out int index))
                return false;

            return RemoveBlock(index);
        }

        /// <summary>
        /// Removes all blocks of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">Block type</typeparam>
        /// <param name="orphanedOnly">Remove orphaned blocks only</param>
        public void RemoveBlocksOfType<T>(bool orphanedOnly = false)
        {
            var blockType = typeof(T);
            if (blockType.Namespace != "NiflySharp.Blocks")
                return;

            string blockTypeName = blockType.Name;
            if (!Header.GetBlockTypeIndex(blockTypeName, out ushort blockTypeIndex))
                return;

            var blockIds = new List<int>();
            for (int blockId = 0; blockId < Header.BlockCount; blockId++)
            {
                if (Header.GetBlockTypeIndex(blockId, out ushort btIndex))
                {
                    if (btIndex == blockTypeIndex)
                    {
                        blockIds.Add(blockId);
                    }
                }
            }

            for (int i = blockIds.Count - 1; i != -1; i--)
            {
                int blockId = blockIds[i];
                if (!orphanedOnly || !IsBlockReferenced(blockId))
                {
                    RemoveBlock(blockId);
                }
            }
        }

        /// <summary>
        /// Removes all unreferenced (loose) blocks of the given type starting at the root.
        /// </summary>
        /// <returns>Removal count</returns>
        public int RemoveUnreferencedBlocks()
        {
            return RemoveUnreferencedBlocks<NiObject>();
        }

        /// <summary>
        /// Removes all unreferenced (loose) blocks of the given type starting at the root.
        /// Use template type "NiObject" for all block types.
        /// </summary>
        /// <typeparam name="T">Block type to remove unreferenced blocks for</typeparam>
        /// <returns>Removal count</returns>
        public int RemoveUnreferencedBlocks<T>() where T : class
        {
            if (HasUnknownBlocks)
                return 0;

            var rootNode = GetRootNode();
            if (rootNode == null)
                return 0;

            if (!GetBlockIndex(rootNode, out int rootIndex))
                return 0;

            int removalCount = 0;
            RemoveUnreferencedBlocks<T>(rootIndex, ref removalCount);
            return removalCount;
        }

        /// <summary>
        /// Removes all unreferenced (loose) blocks of the given type starting at the specified root.
        /// Use template type "NiObject" for all block types.
        /// Adds the amount of removed blocks to "removalCount".
        /// </summary>
        /// <typeparam name="T">Block type to remove unreferenced blocks for</typeparam>
        /// <param name="rootId">Root index for removal</param>
        /// <param name="removalCount">Removal count</param>
        public void RemoveUnreferencedBlocks<T>(int rootId, ref int removalCount) where T : class
        {
            if (rootId == NiRef.NPOS)
                return;

            for (int i = 0; i < Header.BlockCount; i++)
            {
                if (i != rootId)
                {
                    // Only check blocks of provided template type
                    if (GetBlock<T>(i) is NiObject block && !IsBlockReferenced(block))
                    {
                        RemoveBlock(block);

                        removalCount++;

                        // Removing a block can cause others to become unreferenced
                        RemoveUnreferencedBlocks<T>(rootId > i ? rootId - 1 : rootId, ref removalCount);
                        return;
                    }
                }
            }
        }

        public class SortState
        {
            public HashSet<int> VisitedIndices = [];
            public List<int> NewIndices;
            public int NewIndex = 0;
            public List<int> RootShapeOrder = [];

            public SortState(int numBlocks)
            {
                NewIndices = new List<int>(numBlocks);
                for (int i = 0; i < numBlocks; i++)
                    NewIndices.Add(i);
            }
        }

        /// <summary>
        /// Sorts the blocks in a pretty manner based on their block type
        /// </summary>
        public void PrettySortBlocks()
        {
            if (HasUnknownBlocks || Blocks.Count == 0)
                return;

            int blockCount = Blocks.Count;
            if (blockCount == 0)
                return;

            var sortState = new SortState(Blocks.Count);

            var nodes = Blocks.OfType<NiNode>();
            foreach (var node in nodes)
            {
                var parentNode = GetParentNode(node);
                if (parentNode == null)
                {
                    // No parent, node is at the root level
                    if (GetBlockIndex(node, out int nodeIndex))
                    {
                        SetSortIndices(nodeIndex, sortState);
                    }
                }
            }

            for (int i = 0; i < blockCount; i++)
            {
                if (!sortState.VisitedIndices.Contains(i))
                {
                    sortState.NewIndices[i] = sortState.NewIndex++;
                    sortState.VisitedIndices.Add(i);
                }
            }

            Header.SetBlockOrder(Blocks, sortState.NewIndices);
        }

        /// <summary>
        /// Sets the indices in <paramref name="sortState"/> for the given <paramref name="niRef"/> block
        /// </summary>
        /// <param name="niRef">Reference</param>
        /// <param name="sortState">Sort state</param>
        public void SetSortIndices(INiRef niRef, SortState sortState)
        {
            if (niRef == null)
                return;

            SetSortIndices(niRef.Index, sortState);
        }

        /// <summary>
        /// Sets the indices in <paramref name="sortState"/> for the given <paramref name="refIndex"/> block
        /// </summary>
        /// <param name="refIndex">Index of reference block</param>
        /// <param name="sortState">Sort state</param>
        public void SetSortIndices(int refIndex, SortState sortState)
        {
            var obj = GetBlock<NiObject>(refIndex);
            if (obj == null)
                return;

            bool fullySorted = sortState.VisitedIndices.Contains(refIndex);
            if (!fullySorted)
            {
                if (obj is NiCollisionObject collision)
                {
                    SortCollision(collision, refIndex, sortState);
                    fullySorted = true;
                }
                else
                {
                    // Assign new sort index
                    sortState.NewIndices[refIndex] = sortState.NewIndex++;
                    sortState.VisitedIndices.Add(refIndex);
                }
            }

            if (!fullySorted)
            {
                if (obj is NiNode node)
                {
                    SortGraph(node, sortState);
                    fullySorted = true;
                }
            }

            if (!fullySorted)
            {
                if (obj is INiShape shape)
                {
                    SortShape(shape, sortState);
                    fullySorted = true;
                }
            }

            if (!fullySorted)
            {
                if (obj is NiTimeController controller)
                {
                    SortController(controller, sortState);
                    fullySorted = true;
                }
            }

            if (!fullySorted)
            {
                if (obj is INiShader shader)
                {
                    SortNiObjectNET(shader as NiObjectNET, sortState);
                    SetSortIndices(shader.TextureSetRef, sortState);
                    fullySorted = true;
                }
            }

            if (!fullySorted)
            {
                // Default child sorting
                var childIndices = obj.References.Where(r => r != null).Select(r => r.Index);
                foreach (int childIndex in childIndices)
                    SetSortIndices(childIndex, sortState);

                fullySorted = true;
            }
        }

        public void SortCollision(NiObject parent, int parentIndex, SortState sortState)
        {
            if (parent is bhkConstraint constraint)
            {
                foreach (var entityRef in constraint.ConstraintInfo.Pointers)
                {
                    var entity = GetBlock<NiObject>(entityRef);
                    if (entity != null && !sortState.VisitedIndices.Contains(entityRef.Index))
                        SortCollision(entity, entityRef.Index, sortState);
                }
            }

            if (parent is bhkBallSocketConstraintChain constraintChain)
            {
                foreach (var entityRef in constraintChain.ConstraintChainInfo.ChainedEntities.References)
                {
                    var entity = GetBlock<NiObject>(entityRef);
                    if (entity != null && !sortState.VisitedIndices.Contains(entityRef.Index))
                        SortCollision(entity, entityRef.Index, sortState);
                }

                foreach (var entityRef in constraintChain.ConstraintChainInfo.ConstraintInfo.Pointers)
                {
                    var entity = GetBlock<NiObject>(entityRef);
                    if (entity != null && !sortState.VisitedIndices.Contains(entityRef.Index))
                        SortCollision(entity, entityRef.Index, sortState);
                }
            }

            var childIndices = parent.References.Where(r => r != null).Select(r => r.Index);
            foreach (int childIndex in childIndices)
            {
                var child = GetBlock<NiObject>(childIndex);
                if (child != null && !sortState.VisitedIndices.Contains(childIndex))
                {
                    bool childBeforeParent =
                        child.GetType().IsAssignableTo(typeof(bhkRefObject)) &&
                        !child.GetType().IsAssignableTo(typeof(bhkConstraint)) &&
                        !child.GetType().IsAssignableTo(typeof(bhkBallSocketConstraintChain));

                    if (childBeforeParent)
                        SortCollision(child, childIndex, sortState);
                }
            }

            // Assign new sort index
            if (!sortState.VisitedIndices.Contains(parentIndex))
            {
                sortState.NewIndices[parentIndex] = sortState.NewIndex++;
                sortState.VisitedIndices.Add(parentIndex);
            }

            foreach (int childIndex in childIndices)
            {
                var child = GetBlock<NiObject>(childIndex);
                if (child != null && !sortState.VisitedIndices.Contains(childIndex))
                {
                    bool childBeforeParent =
                        child.GetType().IsAssignableTo(typeof(bhkRefObject)) &&
                        !child.GetType().IsAssignableTo(typeof(bhkConstraint)) &&
                        !child.GetType().IsAssignableTo(typeof(bhkBallSocketConstraintChain));

                    if (!childBeforeParent)
                        SortCollision(child, childIndex, sortState);
                }
            }
        }

        public void SortController(NiTimeController controller, SortState sortState)
        {
            var childIndices = controller.References.Where(r => r != null).Select(r => r.Index);
            foreach (var childIndex in childIndices)
            {
                SetSortIndices(childIndex, sortState);

                var controllerSequence = GetBlock<NiControllerSequence>(childIndex);
                if (controllerSequence != null)
                {
                    foreach (var cb in controllerSequence.ControlledBlocks)
                    {
                        var interp = GetBlock<NiInterpolator>(cb.Interpolator);
                        if (interp != null)
                            SetSortIndices(cb.Interpolator, sortState);

                        var subController = GetBlock<NiTimeController>(cb.Controller);
                        if (subController != null)
                            SetSortIndices(cb.Controller, sortState);
                    }

                    SetSortIndices(controllerSequence.TextKeys, sortState);

                    var animNotes = GetBlock<BSAnimNotes>(controllerSequence.AnimNotes);
                    if (animNotes != null)
                    {
                        SetSortIndices(controllerSequence.AnimNotes, sortState);

                        if (animNotes.AnimNotes != null)
                        {
                            foreach (var an in animNotes.AnimNotes.References)
                                SetSortIndices(an, sortState);
                        }
                    }

                    if (controllerSequence.AnimNoteArrays != null)
                    {
                        foreach (var ar in controllerSequence.AnimNoteArrays.References)
                        {
                            animNotes = GetBlock<BSAnimNotes>(ar);
                            if (animNotes != null)
                            {
                                SetSortIndices(ar, sortState);

                                if (animNotes.AnimNotes != null)
                                {
                                    foreach (var an in animNotes.AnimNotes.References)
                                        SetSortIndices(an, sortState);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SortNiObjectNET(NiObjectNET objnet, SortState sortState)
        {
            if (objnet == null)
                return;

            if (objnet.ExtraDataList != null)
                foreach (var r in objnet.ExtraDataList.References)
                    SetSortIndices(r, sortState);

            SetSortIndices(objnet.Controller, sortState);

            var controller = GetBlock<NiTimeController>(objnet.Controller);
            if (controller != null)
                SortController(controller, sortState);
        }

        public void SortAVObject(NiAVObject avobj, SortState sortState)
        {
            if (avobj == null)
                return;

            SortNiObjectNET(avobj, sortState);

            if (avobj.Properties != null)
                foreach (var r in avobj.Properties.References)
                    SetSortIndices(r, sortState);

            var col = GetBlock<NiCollisionObject>(avobj.CollisionObject);
            if (col != null)
                SortCollision(col, avobj.CollisionObject.Index, sortState);
        }

        public void SortShape(INiShape shape, SortState sortState)
        {
            SortAVObject(shape as NiAVObject, sortState);

            SetSortIndices(shape.DataRef, sortState);
            SetSortIndices(shape.SkinInstanceRef, sortState);

            var niSkinInst = GetBlock<NiSkinInstance>(shape.SkinInstanceRef);
            if (niSkinInst != null)
            {
                SetSortIndices(niSkinInst.Data, sortState);
                SetSortIndices(niSkinInst.SkinPartition, sortState);
            }

            var bsSkinInst = GetBlock<BSSkin_Instance>(shape.SkinInstanceRef);
            if (bsSkinInst != null)
                SetSortIndices(bsSkinInst.Data, sortState);

            SetSortIndices(shape.ShaderPropertyRef, sortState);
            SetSortIndices(shape.AlphaPropertyRef, sortState);

            var remainingChildIndices = shape.References.Where(r => r != null).Select(r => r.Index);

            // Sort remaining children
            foreach (int childIndex in remainingChildIndices)
                SetSortIndices(childIndex, sortState);
        }

        public void SortGraph(NiNode root, SortState sortState)
        {
            bool isRootNode = GetBlockIndex(root, out int rootIndex) && rootIndex == 0;
            SortAVObject(root, sortState);

            if (root.Children == null)
                return;

            var childIndices = root.Children.Indices.ToList();
            if (childIndices.Count == 0)
                return;

            bool reorderChildRefs = !root.GetType().IsAssignableTo(typeof(BSOrderedNode));
            if (reorderChildRefs)
            {
                var newChildIndices = new List<int>(childIndices.Count);
                var newChildRefs = new NiBlockRefArray<NiAVObject>();

                if (Header.Version.IsOB() || Header.Version.IsFO3())
                {
                    // Order for OB/FO3:
                    // 1. Nodes with children
                    // 2. Shapes
                    // 3. other

                    // Add nodes with children
                    foreach (var index in childIndices)
                    {
                        var node = GetBlock<NiNode>(index);
                        if (node != null && node.Children.Count > 0)
                        {
                            newChildIndices.Add(index);
                            newChildRefs.AddBlockRef(index);
                        }
                    }

                    // Add shapes
                    var shapeIndices = new List<int>();
                    foreach (var index in childIndices)
                    {
                        var shape = GetBlock<INiShape>(index);
                        if (shape != null)
                            shapeIndices.Add(index);
                    }

                    if (isRootNode)
                    {
                        // Reorder shapes on root node if order is provided
                        if (sortState.RootShapeOrder.Count == shapeIndices.Count)
                        {
                            var newShapeIndices = new List<int>(shapeIndices.Count);
                            for (int si = 0; si < sortState.RootShapeOrder.Count; si++)
                            {
                                int it = shapeIndices.FindIndex(i => i == sortState.RootShapeOrder[si]);
                                if (it != -1)
                                    newShapeIndices[si] = shapeIndices[it];
                            }
                            shapeIndices = newShapeIndices;
                        }
                    }

                    foreach (var index in shapeIndices)
                    {
                        newChildIndices.Add(index);
                        newChildRefs.AddBlockRef(index);
                    }
                }
                else
                {
                    // Order:
                    // 1. Nodes
                    // 2. Shapes
                    // 3. other

                    // Add nodes
                    foreach (var index in childIndices)
                    {
                        var node = GetBlock<NiNode>(index);
                        if (node != null)
                        {
                            newChildIndices.Add(index);
                            newChildRefs.AddBlockRef(index);
                        }
                    }

                    // Add shapes
                    var shapeIndices = new List<int>();
                    foreach (var index in childIndices)
                    {
                        var shape = GetBlock<INiShape>(index);
                        if (shape != null)
                            shapeIndices.Add(index);
                    }

                    if (isRootNode)
                    {
                        // Reorder shapes on root node if order is provided
                        if (sortState.RootShapeOrder.Count == shapeIndices.Count)
                        {
                            var newShapeIndices = new List<int>(shapeIndices.Count);
                            for (int si = 0; si < sortState.RootShapeOrder.Count; si++)
                            {
                                int it = shapeIndices.FindIndex(i => i == sortState.RootShapeOrder[si]);
                                if (it != -1)
                                    newShapeIndices[si] = shapeIndices[it];
                            }
                            shapeIndices = newShapeIndices;
                        }
                    }

                    foreach (var index in shapeIndices)
                    {
                        newChildIndices.Add(index);
                        newChildRefs.AddBlockRef(index);
                    }
                }

                // Add missing others
                foreach (var index in childIndices)
                {
                    if (!newChildIndices.Contains(index))
                    {
                        var obj = GetBlock<NiObject>(index);
                        if (obj != null)
                        {
                            newChildIndices.Add(index);
                            newChildRefs.AddBlockRef(index);
                        }
                    }
                }

                // Add empty refs
                foreach (var index in childIndices)
                {
                    if (index == NiRef.NPOS)
                    {
                        newChildIndices.Add(index);
                        newChildRefs.AddBlockRef(index);
                    }
                }

                // Assign child ref array with new order
                root.Children = newChildRefs;
            }

            var remainingChildIndices = root.References.Where(r => r != null).Select(r => r.Index);

            // Sort remaining children
            foreach (var childIndex in remainingChildIndices)
                SetSortIndices(childIndex, sortState);
        }

        /// <summary>
        /// Renames all shapes that share the same names with a suffix.
        /// </summary>
        /// <param name="parentNode">Parent node or null (root)</param>
        /// <returns>Shapes were renamed</returns>
        public bool RenameDuplicateShapes(NiNode parentNode = null)
        {
            int countDuplicateNames(NiNode parent, string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return 0;

                var names = new List<string>();

                foreach (var group in parent.Children.References.GroupBy(r => r.Index))
                {
                    int childIndex = group.Key;

                    var obj = GetBlock<NiAVObject>(childIndex);
                    if (obj != null)
                    {
                        if (!string.IsNullOrWhiteSpace(obj.Name?.String))
                        {
                            names.Add(obj.Name.String);
                        }
                    }
                }

                return names.Count(n => n == name);
            }

            bool shapesWereRenamed = false;

            List<NiNode> nodes;
            if (parentNode != null)
                nodes = parentNode.Children.GetBlocks(this).OfType<NiNode>().ToList();
            else
                nodes = GetRootNodes();

            foreach (var node in nodes)
            {
                int dupCount = 0;

                foreach (var shape in node.Children.GetBlocks(this).OfType<INiShape>())
                {
                    // Skip first child
                    if (dupCount == 0)
                    {
                        dupCount++;
                        continue;
                    }

                    var shapeName = shape.Name.String;

                    bool duped = countDuplicateNames(node, shapeName) > 1;
                    if (duped)
                    {
                        string dup = $"_{dupCount}";

                        while (countDuplicateNames(node, shapeName + dup) > 1)
                        {
                            dupCount++;
                            dup = $"_{dupCount}";
                        }

                        shape.Name.String = shapeName + dup;
                        dupCount++;
                        shapesWereRenamed = true;
                    }
                }

                // Recursion for child nodes
                if (RenameDuplicateShapes(node))
                    shapesWereRenamed = true;
            }

            return shapesWereRenamed;
        }

        public NifFileOptimizeResult OptimizeFor(NifFileOptimizeOptions options)
        {
            var result = new NifFileOptimizeResult();

            bool toSSE = options.TargetVersion.IsSSE() && Header.Version.IsSK();
            bool toLE = options.TargetVersion.IsSK() && Header.Version.IsSSE();

            if (!toSSE && !toLE)
            {
                result.VersionMismatch = true;
                return result;
            }

            if (!IsTerrainFile)
            {
                result.DuplicatesRenamed = RenameDuplicateShapes();
            }

            Header.Version = options.TargetVersion;

            var shapes = GetShapes().ToList();
            
            if (toSSE)
            {
                foreach (var shape in shapes)
                {
                    var geomData = GetBlock(shape.DataRef);
                    if (geomData == null)
                        continue;

                    bool withoutVertexColors = true;
                    if (!options.RemoveParallax)
                        withoutVertexColors = false;

                    if (withoutVertexColors && geomData.HasVertexColors)
                    {
                        // Remove vertex colors if all elements are white/neutral
                        var white = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
                        withoutVertexColors = geomData.VertexColors.Any(c => c != white);
                    }

                    bool isHeadPartEyes = false;
                    bool withoutNormals = false;
                    bool removedParallax = false;

                    var shader = GetShader(shape);
                    if (shader != null)
                    {
                        if (shader is BSLightingShaderProperty bslsp)
                        {
                            // Remember eyes flag for later
                            if (bslsp.HasEyeEnvironmentMapping)
                                isHeadPartEyes = true;

                            // No normals and tangents with model space maps
                            if (bslsp.ModelSpace)
                                withoutNormals = true;

                            // Check tree anim flag
                            if (bslsp.HasTreeAnim)
                                withoutVertexColors = false;

                            // Unset flags without vertex colors
                            if (withoutVertexColors)
                            {
                                bslsp.HasVertexColors = false;
                                bslsp.HasVertexAlpha = false;
                            }

                            if (options.RemoveParallax)
                            {
                                if (bslsp.IsTypeParallax)
                                {
                                    // Change type from parallax to default
                                    bslsp.ShaderType_SK_FO4 = BSLightingShaderType.Default;

                                    // Remove parallax flag
                                    bslsp.Parallax = false;

                                    // Remove parallax texture from set
                                    var textureSet = GetBlock(bslsp.TextureSetRef);
                                    if (textureSet != null && textureSet.NumTextures >= 4)
                                        textureSet.Textures[3].Content = null;

                                    removedParallax = true;
                                }
                            }
                        }
                        else if (shader is BSEffectShaderProperty bsesp)
                        {
                            // Remember eyes flag for later
                            if (bsesp.HasEyeEnvironmentMapping)
                                isHeadPartEyes = true;

                            // Check tree anim flag
                            if (bsesp.HasTreeAnim)
                                withoutVertexColors = false;

                            // Disable flags if vertex colors were removed
                            if (withoutVertexColors) {
						        bsesp.HasVertexColors = false;
						        bsesp.HasVertexAlpha = false;
					        }
                        }
                    }

                    BSTriShape bsOptShape;
                    if (shape is BSSegmentedTriShape bsSegmentShape)
                    {
                        var bsSITS = new BSSubIndexTriShape();
                        bsOptShape = bsSITS;

                        // Move segments to new shape (by reference as original is replaced)
                        bsSITS.Segments = bsSegmentShape.Segments;
                    }
                    else
                    {
                        bsSegmentShape = null;

                        if (options.HeadPartsOnly)
                            bsOptShape = new BSDynamicTriShape();
                        else
                            bsOptShape = new BSTriShape();
                    }

                    bsOptShape.Name = new NiStringRef(shape.Name.String);
                    bsOptShape.Controller = new NiBlockRef<NiTimeController>(shape.Controller);

                    if (shape.HasSkinInstance)
                        bsOptShape.SkinInstanceRef = shape.SkinInstanceRef.CloneRefAs<NiObject>();

                    if (shape.HasShaderProperty)
                        bsOptShape.ShaderPropertyRef = shape.ShaderPropertyRef.CloneRefAs<BSShaderProperty>();

                    if (shape.HasAlphaProperty)
                        bsOptShape.AlphaPropertyRef = shape.AlphaPropertyRef.Clone();

                    bsOptShape.CollisionObject = shape.CollisionObject?.Clone();
                    bsOptShape.Properties = shape.Properties?.Clone();
                    bsOptShape.ExtraDataList = shape.ExtraDataList?.Clone();

                    bsOptShape.Translation = shape.Translation;
                    bsOptShape.Rotation = shape.Rotation;
                    bsOptShape.Scale = shape.Scale;

                    bsOptShape.Create(Header.Version, geomData.Vertices, geomData.Triangles, geomData.UVSets, geomData.Normals);
                    bsOptShape.Flags_ui = shape.Flags_ui;

                    // Restore old bounds for static meshes or when calc bounds is off
                    if (!shape.IsSkinned || !options.CalculateBounds)
                        bsOptShape.Bounds = geomData.Bounds;

                    var vertexDataSpan = CollectionsMarshal.AsSpan(bsOptShape.VertexDataSSE);

                    ushort vertexCount = bsOptShape.VertexCount;
                    if (vertexCount > 0)
                    {
                        // Copy vertex colors
                        if (!withoutVertexColors && geomData.HasVertexColors)
                        {
                            bsOptShape.HasVertexColors = true;

                            var vertexData = bsOptShape.VertexDataSSE;
                            for (int i = 0; i  < vertexData.Count; i++)
                            {
                                float r = Math.Max(0.0f, Math.Min(1.0f, geomData.VertexColors[i].R));
                                float g = Math.Max(0.0f, Math.Min(1.0f, geomData.VertexColors[i].G));
                                float b = Math.Max(0.0f, Math.Min(1.0f, geomData.VertexColors[i].B));
                                float a = Math.Max(0.0f, Math.Min(1.0f, geomData.VertexColors[i].A));

                                vertexDataSpan[i].VertexColors.R = (byte)Math.Floor(r == 1.0f ? 255.0 : r * 256.0);
                                vertexDataSpan[i].VertexColors.G = (byte)Math.Floor(g == 1.0f ? 255.0 : g * 256.0);
                                vertexDataSpan[i].VertexColors.B = (byte)Math.Floor(b == 1.0f ? 255.0 : b * 256.0);
                                vertexDataSpan[i].VertexColors.A = (byte)Math.Floor(a == 1.0f ? 255.0 : a * 256.0);
                            }
                        }

                        // Find NiOptimizeKeep string
                        foreach (var extraData in bsOptShape.ExtraDataList
                            .GetBlocks(this)
                            .OfType<NiStringExtraData>())
                        {
                            if (extraData.StringData?.String != null &&
                                extraData.StringData.String.Contains("NiOptimizeKeep"))
                            {
                                bsOptShape.ParticleDataSize = (uint)(vertexCount * 6 + bsOptShape.TriangleCount * 3);
                                bsOptShape.ParticleVertices = geomData.Vertices.Select(v =>
                                    new HalfVector3()
                                    {
                                        X = (Half)v.X,
                                        Y = (Half)v.Y,
                                        Z = (Half)v.Z
                                    })
                                    .ToList();

                                if (geomData.HasNormals && geomData.Normals.Count == vertexCount)
                                {
                                    bsOptShape.ParticleNormals = geomData.Normals.Select(n =>
                                        new HalfVector3()
                                        {
                                            X = (Half)n.X,
                                            Y = (Half)n.Y,
                                            Z = (Half)n.Z
                                        })
                                        .ToList();
                                }
                                else
                                {
                                    bsOptShape.ParticleNormals = bsOptShape.ParticleNormals.Resize(vertexCount,
                                        new HalfVector3()
                                        {
                                            X = Half.One
                                        });
                                }

                                bsOptShape.ParticleTriangles = geomData.Triangles;
                            }
                        }

                        // Copy skinning and partitions
                        if (shape.IsSkinned)
                        {
                            bsOptShape.IsSkinned = true;

                            var skinInst = GetBlock<NiSkinInstance>(bsOptShape.SkinInstanceRef);
                            if (skinInst != null)
                            {
                                var skinPart = GetBlock(skinInst.SkinPartition);
                                if (skinPart != null)
                                {
                                    bool triangulated = skinPart.ConvertStripsToTriangles();
                                    if (triangulated)
                                        result.ShapesPartitionsTriangulated.Add(bsOptShape);

                                    var partitionsSpan = CollectionsMarshal.AsSpan(skinPart.Partitions);

                                    for (int partID = 0; partID < partitionsSpan.Length; partID++)
                                    {
                                        ref var part = ref partitionsSpan[partID];

                                        var vertexWeights = part.GetVertexWeights();
                                        var boneIndices = part.GetVertexBoneIndices();

                                        for (ushort i = 0; i < part.NumVertices; i++)
                                        {
                                            ushort vindex = part.HasVertexMap.GetValueOrDefault() ? part.VertexMap[i] : i;

                                            if (bsOptShape.VertexCount > vindex)
                                            {
                                                if (part.HasVertexWeights ?? false && vertexWeights != null)
                                                {
                                                    vertexDataSpan[vindex].BoneWeights = new Half[part.NumWeightsPerVertex];

                                                    var weights = vertexWeights[vindex];
                                                    for (int n = 0; n < part.NumWeightsPerVertex; n++)
                                                    {
                                                        vertexDataSpan[vindex].BoneWeights[n] = (Half)weights[n];
                                                    }
                                                }

                                                if (part.HasBoneIndices ?? false && boneIndices != null)
                                                {
                                                    vertexDataSpan[vindex].BoneIndices = new byte[part.NumWeightsPerVertex];

                                                    var bindices = boneIndices[vindex];
                                                    for (int n = 0; n < part.NumWeightsPerVertex; n++)
                                                    {
                                                        vertexDataSpan[vindex].BoneIndices[n] = bindices[n];
                                                    }
                                                }
                                            }
                                        }

                                        part.GenerateTrueTrianglesFromMappedTriangles();
                                        part.Triangles = [.. part.TrianglesCopy];
                                    }

                                    skinPart.mappedIndices = false;
                                }
                            }
                        }
                        else
                        {
                            bsOptShape.IsSkinned = false;
                        }
                    }
                    else
                    {
                        bsOptShape.HasVertices = false;
                    }

                    // Check if tangents were added
                    if (!geomData.HasTangents && bsOptShape.HasTangents)
                        result.ShapesTangentsAdded.Add(bsOptShape);

                    // Enable eye data flag
                    if (bsSegmentShape == null)
                    {
                        if (options.HeadPartsOnly)
                        {
                            if (isHeadPartEyes)
                                bsOptShape.HasEyeData = true;
                        }
                    }

                    if (withoutVertexColors && geomData.HasVertexColors)
                        result.ShapesVertexColorsRemoved.Add(bsOptShape);

                    if (withoutNormals && geomData.HasNormals)
                        result.ShapesNormalsRemoved.Add(bsOptShape);

                    if (removedParallax)
                        result.ShapesParallaxRemoved.Add(bsOptShape);

                    ReplaceBlock(shape, bsOptShape);
                    UpdateSkinPartitions(bsOptShape);
                }

                RemoveUnreferencedBlocks();

                // For files without a root node, remove the leftover data blocks anyway
                RemoveBlocksOfType<NiTriStripsData>(true);
                RemoveBlocksOfType<NiTriShapeData>(true);
            }
            else if (toLE)
            {
                foreach (var shape in shapes)
                {
                    if (shape is not BSTriShape bsTriShape)
                        continue;

                    var vertexPositions = bsTriShape.VertexPositions;
                    var uvs = bsTriShape.UVs;
                    var normals = bsTriShape.Normals;
                    var vertexColors = bsTriShape.VertexColors;
                    var triangles = bsTriShape.Triangles;

                    bool withoutVertexColors = true;
                    if (!options.RemoveParallax)
                        withoutVertexColors = false;

                    if (withoutVertexColors && bsTriShape.HasVertexColors)
                    {
                        // Remove vertex colors if all elements are white/neutral
                        var white = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
                        withoutVertexColors = vertexColors.Any(c => c != white);
                    }

                    bool withoutNormals = false;

                    var shader = GetShader(shape);
                    if (shader != null)
                    {
                        if (shader is BSLightingShaderProperty bslsp)
                        {
                            // No normals and tangents with model space maps
                            if (bslsp.ModelSpace)
                            {
                                if (normals.Count == 0)
                                    result.ShapesNormalsRemoved.Add(shape);

                                withoutNormals = true;
                            }

                            // Check tree anim flag
                            if (bslsp.ShaderFlags_SSPF2.HasFlag(SkyrimShaderPropertyFlags2.Tree_Anim))
                                withoutVertexColors = false;

                            // Disable flags if vertex colors were removed
                            if (withoutVertexColors)
                            {
                                bslsp.HasVertexColors = false;
                                bslsp.HasVertexAlpha = false;
                            }

                            // This flag breaks LE headparts
                            if (options.HeadPartsOnly)
                            {
                                bslsp.ShaderFlags_SSPF2 &= ~SkyrimShaderPropertyFlags2.Packed_Tangent;
                            }

                            if (options.RemoveParallax)
                            {
                                if (bslsp.ShaderType_SK_FO4 == BSLightingShaderType.Parallax)
                                {
                                    // Change type from parallax to default
                                    bslsp.ShaderType_SK_FO4 = BSLightingShaderType.Default;

                                    // Remove parallax flag
                                    bslsp.ShaderFlags_SSPF1 &= ~SkyrimShaderPropertyFlags1.Parallax;

                                    // Remove parallax texture from set
                                    var textureSet = GetBlock(shader.TextureSetRef);
                                    if (textureSet != null && textureSet.Textures.Count >= 4)
                                        textureSet.Textures[3].Content = string.Empty;

                                    result.ShapesParallaxRemoved.Add(shape);
                                }
                            }
                        }

                        if (shader is BSEffectShaderProperty bsesp)
                        {
                            // Check tree anim flag
                            if (bsesp.ShaderFlags_SSPF2.HasFlag(SkyrimShaderPropertyFlags2.Tree_Anim))
                                withoutVertexColors = false;

                            // Disable flags if vertex colors were removed
                            if (withoutVertexColors)
                            {
                                bsesp.HasVertexColors = false;
                                bsesp.HasVertexAlpha = false;
                            }
                        }
                    }

                    if (withoutVertexColors && vertexColors.Count > 0)
                        result.ShapesVertexColorsRemoved.Add(shape);

                    NiTriShape bsOptShape = null;
                    var bsOptShapeData = new NiTriShapeData();

                    if (shape is BSSubIndexTriShape bssits)
                    {
                        var bsSegmentShape = new BSSegmentedTriShape();
                        bsOptShape = bsSegmentShape;

                        // Move segments to new shape (by reference as original is replaced)
                        bsSegmentShape.Segments = bssits.Segments;
                    }
                    else
                    {
                        bssits = null;
                        bsOptShape = new NiTriShape();
                    }

                    int dataId = AddBlock(bsOptShapeData);
                    bsOptShape.DataRef = new NiBlockRef<NiGeometryData>(dataId);
                    bsOptShape.GeometryData = bsOptShapeData;

                    bsOptShapeData.Create(vertexPositions, triangles, uvs, !withoutNormals ? normals : null);

                    bsOptShape.Name = new NiStringRef(shape.Name.String);
                    bsOptShape.Controller = new NiBlockRef<NiTimeController>(shape.Controller);

                    if (shape.HasSkinInstance)
                        bsOptShape.SkinInstanceRef = shape.SkinInstanceRef.CloneRefAs<NiSkinInstance>();

                    if (shape.HasShaderProperty)
                        bsOptShape.ShaderPropertyRef = shape.ShaderPropertyRef.CloneRefAs<BSShaderProperty>();

                    if (shape.HasAlphaProperty)
                        bsOptShape.AlphaPropertyRef = shape.AlphaPropertyRef.Clone();

                    bsOptShape.CollisionObject = shape.CollisionObject?.Clone();
                    bsOptShape.Properties = shape.Properties?.Clone();
                    bsOptShape.ExtraDataList = shape.ExtraDataList?.Clone();

                    bsOptShape.Translation = shape.Translation;
                    bsOptShape.Rotation = shape.Rotation;
                    bsOptShape.Scale = shape.Scale;

                    bsOptShape.Flags_ui = shape.Flags_ui;

                    // Restore old bounds for static meshes or when calc bounds is off
                    if (!shape.IsSkinned || !options.CalculateBounds)
                        bsOptShape.Bounds = shape.Bounds;

                    // Vertex Colors
                    if (bsOptShape.VertexCount > 0)
                    {
                        if (!withoutVertexColors && vertexColors.Count > 0)
                        {
                            bsOptShape.HasVertexColors = true;

                            var vertexColorsSpan = CollectionsMarshal.AsSpan(bsOptShapeData.VertexColors);
                            for (ushort i = 0; i < vertexColorsSpan.Length; i++)
                                vertexColorsSpan[i] = vertexColors[i];
                        }

                        // Skinning and partitions
                        if (shape.IsSkinned)
                        {
                            var skinInst = GetBlock<NiSkinInstance>(shape.SkinInstanceRef);
                            if (skinInst != null)
                            {
                                var skinPart = GetBlock(skinInst.SkinPartition);
                                if (skinPart != null)
                                {
                                    bool triangulated = skinPart.ConvertStripsToTriangles();
                                    if (triangulated)
                                        result.ShapesPartitionsTriangulated.Add(shape);

                                    var partitionsSpan = CollectionsMarshal.AsSpan(skinPart.Partitions);

                                    for (int partID = 0; partID < partitionsSpan.Length; partID++)
                                    {
                                        ref var part = ref partitionsSpan[partID];

                                        part.GenerateMappedTrianglesFromTrueTrianglesAndVertexMap();
                                    }
                                    skinPart.mappedIndices = true;
                                }
                            }
                        }
                    }
                    else
                        bsOptShape.HasVertices = false;

                    // Check if tangents were added
                    if (!shape.HasTangents && bsOptShape.HasTangents)
                        result.ShapesTangentsAdded.Add(shape);

                    ReplaceBlock(shape, bsOptShape);
                    UpdateSkinPartitions(bsOptShape);
                }

                RemoveUnreferencedBlocks();
                PrettySortBlocks();
            }

            if (options.FixBSXFlags)
                this.FixBSXFlags();

            if (options.FixShaderFlags)
                this.FixShaderFlags();

            return result;
        }

        public void UpdateSkinPartitions(INiShape shape)
        {
            var skinInst = GetBlock<NiSkinInstance>(shape.SkinInstanceRef);
            if (skinInst == null)
                return;

            var skinData = GetBlock(skinInst.Data);
            if (skinData == null)
                return;

            var skinPart = GetBlock(skinInst.SkinPartition);
            if (skinPart == null)
                return;

            var tris = shape.Triangles.ToList();

            var bsTriShape = shape as BSTriShape;
            bsTriShape?.CalcDataSizes(Header.Version);

            // Align triangles for comparisons
            foreach (var tri in tris)
                tri.Rotate();

            // Make maps of vertices to bones and weights
            var vertBoneWeights = new Dictionary<ushort, List<BoneVertData>>();
            ushort boneIndex = 0;

            foreach (var bone in skinData.BoneList)
            {
                foreach (var bw in bone.VertexWeights)
                {
                    var boneVertData = new BoneVertData()
                    {
                        Index = boneIndex,
                        Weight = bw.Weight
                    };

                    if (vertBoneWeights.TryGetValue(bw.Index, out var entry))
                        entry.Add(boneVertData);
                    else
                        vertBoneWeights[bw.Index] = [boneVertData];
                }

                boneIndex++;
            }

            // Sort weights and corresponding bones
            foreach (var bw in vertBoneWeights)
                bw.Value.Sort((lhs, rhs) => lhs.Weight.CompareTo(rhs.Weight));

            // Enforce maximum vertex bone weight count
            const ushort maxBonesPerVertex = 4;

            foreach (var bw in vertBoneWeights)
                if (bw.Value.Count > maxBonesPerVertex)
                    bw.Value.Resize(maxBonesPerVertex);

            skinPart.PrepareTriParts(tris);

            ushort maxBonesPerPartition = ushort.MaxValue;
            if (Header.Version.IsOB() || Header.Version.IsFO3())
                maxBonesPerPartition = 18;
            else if (Header.Version.IsSSE())
                maxBonesPerPartition = 80;

            var bsdSkinInst = skinInst as BSDismemberSkinInstance;

            // Make a list of the bones used by each partition.
            // If any partition has too many bones, split it.
            var partBones = new List<HashSet<int>>(skinPart.Partitions.Count);
            partBones.Resize(skinPart.Partitions.Count);

            for (int triIndex = 0; triIndex < tris.Count; ++triIndex)
            {
                int partInd = skinPart.triParts[triIndex];
                if (partInd < 0)
                    continue;

                partBones[partInd] ??= [];

                var tri = tris[triIndex];

                // Get associated bones for the current tri
                var triBones = new HashSet<int>();
                for (ushort i = 0; i < 3; i++)
                    foreach (var tb in vertBoneWeights[tri[i]])
                        triBones.Add(tb.Index);

                // How many new bones are in the tri's bone list?
                ushort newBoneCount = 0;
                foreach (var tb in triBones)
                    if (!partBones[partInd].Contains(tb))
                        newBoneCount++;

                int partBonesSize = partBones[partInd].Count;
                if (partBonesSize + newBoneCount > maxBonesPerPartition)
                {
                    // Too many bones for this partition, make a new partition starting with this triangle
                    for (int j = 0; j < tris.Count; ++j)
                        if (skinPart.triParts[j] > partInd || (j >= triIndex && skinPart.triParts[j] >= partInd))
                            ++skinPart.triParts[j];

                    partBones.Insert(partInd + 1, []);

                    if (bsdSkinInst != null)
                    {
                        var info = new BodyPartList
                        {
                            PartFlag = BSPartFlag.PF_EDITOR_VISIBLE,
                            BodyPart = bsdSkinInst.Partitions[partInd].BodyPart
                        };
                        bsdSkinInst.Partitions.Insert(partInd + 1, info);
                    }

                    ++partInd;
                }

                partBones[partInd].UnionWith(triBones);
            }

            // Re-create partitions
            skinPart.NumPartitions = (uint)partBones.Count;
            skinPart.Partitions = skinPart.Partitions.Resize(partBones.Count);

            var spanPartitions = CollectionsMarshal.AsSpan(skinPart.Partitions);
            foreach (ref var part in spanPartitions)
            {
                part = new SkinPartition
                {
                    HasBoneIndices = true,
                    HasFaces = true,
                    HasVertexMap = true,
                    HasVertexWeights = true,
                    NumWeightsPerVertex = maxBonesPerVertex,
                    GlobalVB = false
                };
            }

            // Re-create trueTriangles, vertexMap, and triangles for each partition
            skinPart.GenerateTrueTrianglesFromTriParts(tris);
            skinPart.PrepareVertexMapsAndTriangles();

	        for (int partInd = 0; partInd < skinPart.NumPartitions; ++partInd) {
		        ref var part = ref spanPartitions[partInd];

		        // Copy relevant data from shape to partition
		        if (bsTriShape != null)
			        part.VertexDesc = new(bsTriShape.VertexDesc.Value);

		        var boneLookup = new Dictionary<int, byte>();
		        boneLookup.EnsureCapacity(partBones[partInd].Count);

		        part.NumBones = (ushort)partBones[partInd].Count;
                part.Bones = new List<ushort>(part.NumBones);

		        foreach (var b in partBones[partInd])
                {
			        part.Bones.Add((ushort)b);
			        boneLookup[b] = (byte)(part.Bones.Count - 1);
		        }

		        foreach (var v in part.VertexMap)
                {
			        var b = new byte[4];
                    var vw = new float[4];

			        float tot = 0.0f;
			        for (int bi = 0; bi < vertBoneWeights[v].Count; bi++)
                    {
				        if (bi == 4)
					        break;

                        if (boneLookup.TryGetValue(vertBoneWeights[v][bi].Index, out byte lookupValue))
                            b[bi] = lookupValue;
                        else
                            b[bi] = 0;

				        vw[bi] = vertBoneWeights[v][bi].Weight;
				        tot += vw[bi];
			        }

			        if (tot != 0.0f)
				        for (int bi = 0; bi < 4; bi++)
					        vw[bi] /= tot;

                    part.BoneIndices ??= [];
                    part.BoneIndices.AddRange(b);

                    part.VertexWeights ??= [];
                    part.VertexWeights.AddRange(vw);
		        }
	        }

	        if (bsTriShape != null)
            {
		        skinPart.DataSize = bsTriShape.DataSize;
		        skinPart.VertexSize = bsTriShape.VertexSize;
		        skinPart.SetVertexData(bsTriShape.VertexDataSSE);
		        skinPart.VertexDesc = new(bsTriShape.VertexDesc.Value);
	        }

	        UpdatePartitionFlags(shape);
        }

        public void UpdatePartitionFlags(INiShape shape)
        {
            var bsdSkinInst = GetBlock<BSDismemberSkinInstance>(shape.SkinInstanceRef);
            if (bsdSkinInst == null)
                return;

            var skinPart = GetBlock(bsdSkinInst.SkinPartition);
            if (skinPart == null)
                return;

            var partitionsSpan = CollectionsMarshal.AsSpan(bsdSkinInst.Partitions);

            for (int i = 0; i < bsdSkinInst.Partitions.Count; i++)
            {
                BSPartFlag flags = 0;

                if (Header.Version.IsFO3())
                {
                    // Don't make FO3/NV meat caps visible
                    if ((int)partitionsSpan[i].BodyPart < 100 || (int)partitionsSpan[i].BodyPart >= 1000)
                        flags |= BSPartFlag.PF_EDITOR_VISIBLE;
                }
                else
                    flags |= BSPartFlag.PF_EDITOR_VISIBLE;

                if (i != 0)
                {
                    // Start a new set if the previous bones are different
                    if (skinPart.Partitions.Count > i && skinPart.Partitions[i].Bones != skinPart.Partitions[i - 1].Bones)
                        flags |= BSPartFlag.PF_START_NET_BONESET;
                }
                else
                    flags |= BSPartFlag.PF_START_NET_BONESET;

                partitionsSpan[i].PartFlag = flags;
            }
        }

        /// <summary>
        /// Retrieves tangent and bitangent data from a NiBinaryExtraData block linked to shape <paramref name="shape"/>.
        /// Extra data needs to be named "Tangent space (binormal &amp; tangent vectors)".
        /// </summary>
        /// <param name="shape">Shape</param>
        /// <param name="tangents">Retrieved tangents</param>
        /// <param name="bitangents">Retrieved bitangents</param>
        /// <returns>NiBinaryExtraData block or null</returns>
        public NiBinaryExtraData GetBinaryTangentData(INiShape shape, out List<Vector3> tangents, out List<Vector3> bitangents)
        {
            ArgumentNullException.ThrowIfNull(shape);

            tangents = [];
            bitangents = [];

            ushort numVerts = shape.VertexCount;

            foreach (var binaryData in shape.ExtraDataList
                .GetBlocks(this)
                .OfType<NiBinaryExtraData>()
                .Where(b => b.Name.String == "Tangent space (binormal & tangent vectors)"))
            {
                uint dataSize = (uint)(numVerts * 4 * 3 * 2);
                if (binaryData.BinaryData.DataSize == dataSize)
                {
                    var data = binaryData.BinaryData.Data.ToArray();
                    ref var vectorData = ref Unsafe.As<byte[], Vector3[]>(ref data);

                    int dataIndex = 0;

                    tangents.Resize(numVerts);

                    for (ushort i = 0; i < numVerts; i++)
                    {
                        tangents[i] = vectorData[dataIndex];
                        ++dataIndex;
                    }

                    bitangents.Resize(numVerts);

                    for (ushort i = 0; i < numVerts; i++)
                    {
                        bitangents[i] = vectorData[dataIndex];
                        ++dataIndex;
                    }
                }

                return binaryData;
            }

            return null;
        }

        /// <summary>
        /// Copy tangent and bitangent data into a NiBinaryExtraData block linked to shape <paramref name="shape"/>.
        /// Extra data will be named "Tangent space (binormal &amp; tangent vectors)".
        /// </summary>
        /// <param name="shape">Shape</param>
        /// <param name="tangents">Tangents</param>
        /// <param name="bitangents">Bitangents</param>
        public void SetBinaryTangentData(INiShape shape, List<Vector3> tangents, List<Vector3> bitangents)
        {
            ArgumentNullException.ThrowIfNull(shape);
            ArgumentNullException.ThrowIfNull(tangents);
            ArgumentNullException.ThrowIfNull(bitangents);

            ushort numVerts = shape.VertexCount;
            if (tangents.Count != numVerts)
                ArgumentOutOfRangeException.ThrowIfNotEqual(tangents.Count, numVerts, nameof(tangents));
            if (bitangents.Count != numVerts)
                ArgumentOutOfRangeException.ThrowIfNotEqual(bitangents.Count, numVerts, nameof(bitangents));

            // Find existing fitting NiBinaryExtraData block
            var binaryData = shape.ExtraDataList
                .GetBlocks(this)
                .OfType<NiBinaryExtraData>()
                .Where(b => b.Name.String == "Tangent space (binormal & tangent vectors)")
                .FirstOrDefault();

            if (binaryData == null)
            {
                // Add new NiBinaryExtraData block
                binaryData = new NiBinaryExtraData
                {
                    Name = new NiStringRef("Tangent space (binormal & tangent vectors)")
                };

                int binaryDataId = AddBlock(binaryData);
                shape.ExtraDataList ??= new NiBlockRefArray<NiExtraData>();
                shape.ExtraDataList.AddBlockRef(binaryDataId);
            }

            uint dataSize = (uint)(numVerts * 4 * 3 * 2);

            var data = new byte[dataSize];
            ref var vectorData = ref Unsafe.As<byte[], Vector3[]>(ref data);

            int dataIndex = 0;

            for (ushort i = 0; i < numVerts; i++)
            {
                vectorData[dataIndex] = tangents[i];
                ++dataIndex;
            }

            for (ushort i = 0; i < numVerts; i++)
            {
                vectorData[dataIndex] = bitangents[i];
                ++dataIndex;
            }

            binaryData.BinaryData = new ByteArray()
            {
                DataSize = dataSize,
                Data = [.. data]
            };
        }

        /// <summary>
        /// Deletes NiBinaryExtraData block containing tangent and bitangent data linked to shape <paramref name="shape"/>.
        /// Extra data needs to be named "Tangent space (binormal &amp; tangent vectors)".
        /// </summary>
        /// <param name="shape">Shape</param>
        public void DeleteBinaryTangentData(INiShape shape)
        {
            ArgumentNullException.ThrowIfNull(shape);

            foreach (var binaryData in shape.ExtraDataList
                .GetBlocks(this)
                .OfType<NiBinaryExtraData>()
                .Where(b => b.Name.String == "Tangent space (binormal & tangent vectors)")
                .ToArray())
            {
                RemoveBlock(binaryData);
            }
        }
    }
}
