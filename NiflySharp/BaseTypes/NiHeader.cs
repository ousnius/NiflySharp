using NiflySharp.Stream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NiflySharp
{
    public partial class NiHeader : NiObject
    {
        /// <summary>
        /// Endianess used when reading/writing the file. Usually little endian.
        /// </summary>
        public NiEndian Endian { get; set; } = NiEndian.Little;

        /// <summary>
        /// Creator of the file (used in Bethesda files)
        /// </summary>
        public NiString Creator { get; set; }

        /// <summary>
        /// Export information string 1 (used in Bethesda files)
        /// </summary>
        public NiString ExportInfo1 { get; set; }

        /// <summary>
        /// Export information string 2 (used in Bethesda files)
        /// </summary>
        public NiString ExportInfo2 { get; set; }

        /// <summary>
        /// Export information string 3 (used in Bethesda files)
        /// </summary>
        public NiString ExportInfo3 { get; set; }

        /// <summary>
        /// Copyright line 1
        /// </summary>
        public string Copyright1 { get; set; }

        /// <summary>
        /// Copyright line 2
        /// </summary>
        public string Copyright2 { get; set; }

        /// <summary>
        /// Copyright line 3
        /// </summary>
        public string Copyright3 { get; set; }

        private uint unkInt1;

        /// <summary>
        /// Embedded binary data
        /// </summary>
        private List<byte> embedData;

        /// <summary>
        /// List of block type names
        /// </summary>
        private List<NiString> blockTypes;

        /// <summary>
        /// List of block type indices of all blocks
        /// </summary>
        private List<ushort> blockTypeIndices;

        /// <summary>
        /// List of block size for each block
        /// </summary>
        private List<int> blockSizes;

        /// <summary>
        /// Maximum header string length. Updated before writing the file.
        /// </summary>
        private uint maxStringLen;

        /// <summary>
        /// List of all strings in the header
        /// </summary>
        private List<NiString> strings;

        /// <summary>
        /// List of group sizes
        /// </summary>
        private List<int> groupSizes;

        /// <summary>
        /// All version information of the file
        /// </summary>
        public NiVersion Version { get; set; }

        /// <summary>
        /// Amount of blocks currently in the file
        /// </summary>
        public int BlockCount { get; protected set; }

        /// <summary>
        /// Valid header was loaded
        /// </summary>
        public bool Valid { get; protected set; }

        public NiHeader() { }

        public NiHeader(NiVersion version)
        {
            Version = version;

            if (Version.IsBethesda())
            {
                Creator = new NiString();

                ExportInfo1 = new NiString();
                ExportInfo2 = new NiString();

                if (Version.StreamVersion >= 130)
                {
                    ExportInfo3 = new NiString();
                }
            }
            else if (Version.FileVersion >= NiFileVersion.V30_0_0_2)
            {
                embedData = [];
            }

            if (Version.FileVersion >= NiFileVersion.V5_0_0_1)
            {
                blockTypes = [];
                blockTypeIndices = [];
            }

            if (Version.FileVersion >= NiFileVersion.V20_2_0_5)
            {
                blockSizes = [];
            }

            if (Version.FileVersion >= NiFileVersion.V20_1_0_1)
            {
                strings = [];
            }

            if (Version.FileVersion >= NiVersion.ToFile(5, 0, 0, 6))
            {
                groupSizes = [];
            }
        }

        /// <summary>
        /// Clear all header information
        /// </summary>
        public void Clear()
        {
            blockTypes?.Clear();
            blockTypeIndices?.Clear();
            blockSizes?.Clear();
            strings?.Clear();
        }

        /// <summary>
        /// Read header from stream <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Stream</param>
        public void Read(NiStreamReader stream)
        {
            Version = new NiVersion();

            string ver = stream.GetLine(128);

            bool isNetImmerse = ver.Contains(NiVersion.NIF_NETIMMERSE);
            bool isGamebryo = ver.Contains(NiVersion.NIF_GAMEBRYO);
            bool isNDS = ver.Contains(NiVersion.NIF_NDS);

            if (!isNetImmerse && !isGamebryo && !isNDS)
                return;

            NiFileVersion vfile = NiFileVersion.Unknown;

            int verStrIndex = ver.IndexOf(", Version ");
            if (verStrIndex != -1)
            {
                string verStr = ver[(verStrIndex + 10)..];

                var reg = VersionNumberRegex();
                var matches = reg.Matches(verStr);

                var v = new byte[4];
                byte m = 0;

                foreach (Match match in matches)
                {
                    if (m == 4)
                        break;

                    v[m] = Convert.ToByte(match.Value);
                    m++;
                }

                vfile = NiVersion.ToFile(v[0], v[1], v[2], v[3]);
            }

            if (vfile > NiFileVersion.V3_1 && !isNDS)
            {
                vfile = (NiFileVersion)stream.Reader.ReadUInt32();
            }
            else if (isNDS)
            {
                Version.NDSVersion = stream.Reader.ReadUInt32();
            }
            else
            {
                Copyright1 = stream.GetLine(128);
                Copyright2 = stream.GetLine(128);
                Copyright3 = stream.GetLine(128);
            }

            Version.FileVersion = vfile;

            if (vfile >= NiVersion.ToFile(20, 0, 0, 3))
                Endian = (NiEndian)stream.Reader.ReadByte();
            else
                Endian = NiEndian.Little;

            if (vfile >= NiVersion.ToFile(10, 0, 1, 8))
            {
                Version.UserVersion = stream.Reader.ReadUInt32();
            }

            BlockCount = stream.Reader.ReadInt32();

            if (Version.IsBethesda())
            {
                Version.StreamVersion = stream.Reader.ReadUInt32();

                Creator = new NiString();
                Creator.Read(stream, 1);

                if (Version.StreamVersion > 130)
                    unkInt1 = stream.Reader.ReadUInt32();

                ExportInfo1 = new NiString();
                ExportInfo1.Read(stream, 1);

                ExportInfo2 = new NiString();
                ExportInfo2.Read(stream, 1);

                if (Version.StreamVersion == 130)
                {
                    ExportInfo3 = new NiString();
                    ExportInfo3.Read(stream, 1);
                }
            }
            else if (vfile >= NiFileVersion.V30_0_0_2)
            {
                embedData = new List<byte>(stream.Reader.ReadInt32());

                for (int i = 0; i < embedData.Capacity; i++)
                    embedData.Add(stream.Reader.ReadByte());
            }

            if (vfile >= NiFileVersion.V5_0_0_1)
            {
                blockTypes = new List<NiString>(stream.Reader.ReadUInt16());

                for (int i = 0; i < blockTypes.Capacity; i++)
                {
                    var blockType = new NiString();
                    blockType.Read(stream, 4);
                    blockTypes.Add(blockType);
                }

                blockTypeIndices = new List<ushort>(BlockCount);

                for (int i = 0; i < blockTypeIndices.Capacity; i++)
                    blockTypeIndices.Add(stream.Reader.ReadUInt16());
            }

            if (vfile >= NiFileVersion.V20_2_0_5)
            {
                blockSizes = new List<int>(BlockCount);

                for (int i = 0; i < BlockCount; i++)
                    blockSizes.Add(stream.Reader.ReadInt32());
            }

            if (vfile >= NiFileVersion.V20_1_0_1)
            {
                int numStrings = (int)stream.Reader.ReadUInt32();
                strings = new List<NiString>(numStrings);
                maxStringLen = stream.Reader.ReadUInt32();

                for (int i = 0; i < numStrings; i++)
                {
                    var str = new NiString();
                    str.Read(stream, 4);
                    strings.Add(str);
                }
            }

            if (vfile >= NiVersion.ToFile(5, 0, 0, 6))
            {
                groupSizes = new List<int>(stream.Reader.ReadInt32());

                for (int i = 0; i < groupSizes.Capacity; i++)
                    groupSizes.Add(stream.Reader.ReadInt32());
            }

            Valid = true;
        }

        /// <summary>
        /// Wrte header to stream <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Stream</param>
        public void Write(NiStreamWriter stream)
        {
            stream.Writer.Write(Version.VersionString.ToCharArray());
            stream.Writer.Write((byte)0x0A); // Newline to end header string

            bool isNDS = Version.NDSVersion != 0;
            if (Version.FileVersion > NiFileVersion.V3_1 && !isNDS)
            {
                stream.Writer.Write((uint)Version.FileVersion);
            }
            else if (isNDS)
            {
                stream.Writer.Write(Version.NDSVersion);
            }
            else
            {
                stream.Writer.Write(Copyright1.PadRight(128, '\0').ToCharArray());
                stream.Writer.Write(Copyright2.PadRight(128, '\0').ToCharArray());
                stream.Writer.Write(Copyright3.PadRight(128, '\0').ToCharArray());
            }

            if (Version.FileVersion >= NiVersion.ToFile(20, 0, 0, 3))
                stream.Writer.Write((byte)Endian);

            if (Version.FileVersion >= NiVersion.ToFile(10, 0, 1, 8))
                stream.Writer.Write(Version.UserVersion);

            stream.Writer.Write(BlockCount);

            if (Version.IsBethesda())
            {
                stream.Writer.Write(Version.StreamVersion);

                Creator.NullOutput = true;
                Creator.Write(stream, 1);

                if (Version.StreamVersion > 130)
                    stream.Writer.Write(unkInt1);

                ExportInfo1.NullOutput = true;
                ExportInfo1.Write(stream, 1);

                ExportInfo2.NullOutput = true;
                ExportInfo2.Write(stream, 1);

                if (Version.StreamVersion == 130)
                {
                    ExportInfo3.NullOutput = true;
                    ExportInfo3.Write(stream, 1);
                }
            }
            else if (Version.FileVersion >= NiFileVersion.V30_0_0_2)
            {
                stream.Writer.Write(embedData.Count);
                embedData.ForEach(ed => stream.Writer.Write(ed));
            }

            if (Version.FileVersion >= NiFileVersion.V5_0_0_1)
            {
                stream.Writer.Write((ushort)blockTypes.Count);
                blockTypes.ForEach(bt => bt.Write(stream, 4));
                blockTypeIndices.ForEach(bti => stream.Writer.Write(bti));
            }

            if (Version.FileVersion >= NiFileVersion.V20_2_0_5)
            {
                stream.BlockSizePos = stream.Writer.BaseStream.Position;
                blockSizes.ForEach(bs => stream.Writer.Write(bs));
            }

            if (Version.FileVersion >= NiFileVersion.V20_1_0_1)
            {
                stream.Writer.Write(strings.Count);
                stream.Writer.Write(maxStringLen);
                strings.ForEach(s => s.Write(stream, 4));
            }

            if (Version.FileVersion >= NiVersion.ToFile(5, 0, 0, 6))
            {
                stream.Writer.Write(groupSizes.Count);
                groupSizes.ForEach(gs => stream.Writer.Write(gs));
            }
        }

        /// <summary>
        /// Get block type index of block with index <paramref name="blockId"/>
        /// </summary>
        /// <param name="blockId">Block index</param>
        /// <param name="blockTypeIndex">Found block type index value</param>
        /// <returns>Block type found</returns>
        public bool GetBlockTypeIndex(int blockId, out ushort blockTypeIndex)
        {
            if (blockId == NiRef.NPOS || blockId >= BlockCount)
            {
                blockTypeIndex = 0;
                return false;
            }

            blockTypeIndex = blockTypeIndices[blockId];
            return true;
        }

        /// <summary>
        /// Get block type index of block type with index <paramref name="blockTypeName"/>
        /// </summary>
        /// <param name="blockTypeName">Block type name</param>
        /// <param name="blockTypeIndex">Found block type index value</param>
        /// <returns>Block type found</returns>
        public bool GetBlockTypeIndex(string blockTypeName, out ushort blockTypeIndex)
        {
            if (string.IsNullOrWhiteSpace(blockTypeName))
            {
                blockTypeIndex = 0;
                return false;
            }

            int index = blockTypes.FindIndex(bt => bt.Content == blockTypeName);
            if (index < 0)
            {
                blockTypeIndex = 0;
                return false;
            }

            blockTypeIndex = (ushort)index;
            return true;
        }

        /// <summary>
        /// Add or find an existing block type index with block type name <paramref name="blockTypeName"/>
        /// </summary>
        /// <param name="blockTypeName">Block type name to add or find</param>
        /// <returns>Found or added block type index</returns>
        public ushort AddOrFindBlockTypeIndex(string blockTypeName)
        {
            ushort blockTypesCount = (ushort)blockTypes.Count;
            for (ushort i = 0; i < blockTypesCount; i++)
            {
                if (blockTypes[i].Content == blockTypeName)
                    return i;
            }

            // Block type name not found, add it
            var niStr = new NiString(blockTypeName);
            blockTypes.Add(niStr);
            return blockTypesCount;
        }

        /// <summary>
        /// Get the name of the type of block <paramref name="blockId"/>.
        /// </summary>
        /// <param name="blockId">Block index</param>
        /// <returns>Block type name or <see cref="string.Empty"/></returns>
        public string GetBlockTypeNameById(int blockId)
        {
            if (blockTypes == null || blockTypeIndices == null)
                return null;

            if (blockId < 0 || blockId >= BlockCount)
                return null;

            ushort typeIndex = blockTypeIndices[blockId];
            if (typeIndex < 0 || typeIndex >= blockTypes.Count)
                return null;

            return blockTypes[typeIndex].Content;
        }

        /// <summary>
        /// Adds the block type index entry and block size entry for the new block <paramref name="newBlock"/>
        /// </summary>
        /// <param name="newBlock">New block</param>
        public void AddBlockInfo(NiObject newBlock)
        {
            string blockTypeName = newBlock.GetType().Name;
            ushort blockTypeIndex = AddOrFindBlockTypeIndex(blockTypeName);
            blockTypeIndices.Add(blockTypeIndex);

            if (Version.FileVersion >= NiFileVersion.V20_2_0_5)
                blockSizes.Add(0);

            BlockCount++;
        }

        /// <summary>
        /// Replaces block info at id <paramref name="oldBlockId"/> with new block <paramref name="newBlock"/> info.
        /// </summary>
        /// <param name="oldBlockId">Old block id to replace</param>
        /// <param name="newBlock">New block</param>
        /// <returns>Successfully replaced</returns>
        public bool ReplaceBlockInfo(int oldBlockId, INiObject newBlock)
        {
            if (oldBlockId == NiRef.NPOS)
                return false;

            if (BlockCount <= oldBlockId)
                return false;

            if (!GetBlockTypeIndex(oldBlockId, out ushort blockTypeId))
                return false;

            if (blockTypeIndices.Count(b => b == blockTypeId) < 2)
            {
                blockTypes.RemoveAt(blockTypeId);

                for (int i = 0; i < blockTypeIndices.Count; i++)
                {
                    if (blockTypeIndices[i] > blockTypeId)
                        blockTypeIndices[i]--;
                }
            }

            string blockTypeName = newBlock.GetType().Name;
            ushort blockTypeIndex = AddOrFindBlockTypeIndex(blockTypeName);
            blockTypeIndices[oldBlockId] = blockTypeIndex;

            if (Version.FileVersion >= NiFileVersion.V20_2_0_5)
                blockSizes[oldBlockId] = 0;

            return true;
        }

        /// <summary>
        /// Removes the block type index entry and block size entry for block index <paramref name="blockId"/>
        /// </summary>
        /// <param name="blockId">Block index</param>
        public bool RemoveBlockInfo(int blockId)
        {
            if (!GetBlockTypeIndex(blockId, out ushort blockTypeId))
                return false;

            if (blockTypeIndices.Count(b => b == blockTypeId) == 1)
            {
                blockTypes.RemoveAt(blockTypeId);

                for (int i = 0; i < blockTypeIndices.Count; i++)
                {
                    if (blockTypeIndices[i] > blockTypeId)
                        blockTypeIndices[i]--;
                }
            }

            blockTypeIndices.RemoveAt(blockId);

            if (Version.FileVersion >= NiFileVersion.V20_2_0_5)
                blockSizes.RemoveAt(blockId);

            BlockCount--;
            return true;
        }

        /// <summary>
        /// Get the stored block size of block <paramref name="blockId"/>.
        /// </summary>
        /// <param name="blockId">Block index</param>
        /// <returns>Block size or <see cref="NiRef.NPOS"/></returns>
        public int GetBlockSize(int blockId)
        {
            if (blockId >= 0 && blockId < BlockCount)
                return blockSizes[blockId];

            return NiRef.NPOS;
        }

        /// <summary>
        /// Get the string <paramref name="stringId"/>.
        /// </summary>
        /// <param name="stringId">String index</param>
        /// <returns>String or <see cref="string.Empty"/></returns>
        public string GetString(int stringId)
        {
            if (stringId >= 0 && stringId < strings.Count)
                return strings[stringId].Content;

            return string.Empty;
        }

        /// <summary>
        /// Add a new or find an existing string index that matches <paramref name="str"/>.
        /// </summary>
        /// <param name="str">String to add or find</param>
        /// <param name="addEmpty">Add empty string as new index or not</param>
        /// <returns>Added or found string index</returns>
        public int AddOrFindStringId(string str, bool addEmpty)
        {
            for (int i = 0; i < strings.Count; i++)
                if (strings[i].Content == str)
                    return i;

            if (!addEmpty && string.IsNullOrEmpty(str))
                return NiRef.NPOS;

            if (strings.Count >= int.MaxValue)
                return NiRef.NPOS;

            var niStr = new NiString(str);
            strings.Add(niStr);

            return strings.Count - 1;
        }

        /// <summary>
        /// Fills all strings in string refs in <paramref name="blocks"/> from header strings.
        /// </summary>
        /// <param name="blocks">List of blocks</param>
        public void FillStringRefs(List<INiObject> blocks)
        {
            if (Version.FileVersion < NiFileVersion.V20_1_0_1)
                return;

            foreach (var block in blocks)
            {
                foreach (var stringRef in block.StringRefs)
                {
                    int stringId = stringRef.Index;

                    // Check if string index is overflowing
                    if (stringId != NiRef.NPOS && stringId >= strings.Count)
                    {
                        stringId -= strings.Count;
                        stringRef.Index = stringId;
                    }

                    stringRef.String = GetString(stringId);
                }
            }
        }

        /// <summary>
        /// Updates the maximum header string length.
        /// </summary>
        public void UpdateMaxStringLength()
        {
            maxStringLen = 0;

            foreach (var s in strings.Where(s => !string.IsNullOrEmpty(s.Content)))
            {
                uint len = (uint)s.Content.Length;
                if (maxStringLen < len)
                    maxStringLen = len;
            }
        }

        /// <summary>
        /// Update all header strings from string refs in the file's blocks.
        /// </summary>
        /// <param name="blocks">List of blocks</param>
        /// <param name="hasUnknown">Does the file have unknown blocks?</param>
        public void UpdateHeaderStrings(List<INiObject> blocks, bool hasUnknown)
        {
            if (!hasUnknown)
                strings?.Clear();

            if (Version.FileVersion < NiFileVersion.V20_1_0_1)
                return;

            foreach (var block in blocks)
            {
                foreach (var stringRef in block.StringRefs)
                {
                    bool addEmpty = stringRef.Index != NiRef.NPOS;
                    int stringId = AddOrFindStringId(stringRef.String, addEmpty);
                    stringRef.Index = stringId;
                }
            }

            UpdateMaxStringLength();
        }

        /// <summary>
        /// Sets the order of all blocks.
        /// </summary>
        /// <param name="blocks">List of all blocks to reorder</param>
        /// <param name="newOrder">List of block indices in new order</param>
        public void SetBlockOrder(List<INiObject> blocks, List<int> newOrder)
        {
            if (newOrder.Count != BlockCount)
                return;

            var newBlocks = new List<INiObject>(blocks);

            for (int i = 0; i < BlockCount; i++)
                newBlocks[newOrder[i]] = blocks[i];

            blocks.Clear();
            blocks.AddRange(newBlocks);

            if (Version.FileVersion >= NiFileVersion.V5_0_0_1)
            {
                var newBlockTypeIndices = new List<ushort>(blockTypeIndices);

                for (int i = 0; i < BlockCount; i++)
                    newBlockTypeIndices[newOrder[i]] = blockTypeIndices[i];

                blockTypeIndices = newBlockTypeIndices;
            }

            if (Version.FileVersion >= NiFileVersion.V20_2_0_5)
            {
                var newBlockSizes = new List<int>(blockSizes);
                for (int i = 0; i < BlockCount; i++)
                {
                    newBlockSizes[newOrder[i]] = blockSizes[i];
                }

                blockSizes = newBlockSizes;
            }

            foreach (var block in blocks)
            {
                foreach (var r in block.References.Where(r => r != null && !r.IsEmpty()))
                    r.Index = newOrder[r.Index];

                foreach (var p in block.Pointers.Where(p => p != null && !p.IsEmpty()))
                    p.Index = newOrder[p.Index];
            }
        }

        [GeneratedRegex("25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9]?[0-9]")]
        private static partial Regex VersionNumberRegex();
    }
}
