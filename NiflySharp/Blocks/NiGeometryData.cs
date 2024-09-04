using NiflySharp.Extensions;
using NiflySharp.Structs;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace NiflySharp.Blocks
{
    public partial class AdditionalGeomData : NiObject
    {

    }

    public partial class NiGeometryData
    {
        public BoundingSphere Bounds
        {
            get => new(_boundingSphere.Center, _boundingSphere.Radius);
            set => _boundingSphere = new NiBound() { Center = value.Center, Radius = value.Radius };
        }

        public void UpdateBounds()
        {
            Bounds = new BoundingSphere(_vertices ?? default);
        }

        public ushort NumVertices => _numVertices;

        public bool HasVertices
        {
            get => _hasVertices ?? false;
            set
            {
                _hasVertices = value;
                if (value)
                {
                    _vertices = _vertices.Resize(_numVertices);
                }
                else
                {
                    _vertices?.Clear();
                    _numVertices = 0;

                    HasNormals = false;
                    HasVertexColors = false;
                    HasUVs = false;
                    HasTangents = false;
                }
            }
        }

        public bool HasNormals
        {
            get => _hasNormals ?? false;
            set
            {
                _hasNormals = value;
                if (value)
                    _normals = _normals.Resize(_numVertices);
                else
                    _normals?.Clear();
            }
        }

        public bool HasVertexColors
        {
            get => _hasVertexColors ?? false;
            set
            {
                _hasVertexColors = value;
                if (value)
                    _vertexColors = _vertexColors.Resize(_numVertices, new Color4(1.0f, 1.0f, 1.0f, 1.0f));
                else
                    _vertexColors?.Clear();
            }
        }

        public bool HasUVs
        {
            get =>
                (_bSDataFlags != null && _bSDataFlags.HasUV > 0) ||
                (_dataFlags != null && _dataFlags.NumUVSets > 0);

            set
            {
                if (_dataFlags != null && value && _dataFlags.NumUVSets == 0)
                {
                    _dataFlags.NumUVSets = 1;
                }
                else if (_dataFlags != null && !value)
                {
                    _dataFlags.NumUVSets = 0;
                }
                else
                {
                    _bSDataFlags ??= new Bitfields.BSGeometryDataFlags();
                    _bSDataFlags.HasUV = value ? 1 : 0;
                }

                if (value)
                    _uVSets = _uVSets.Resize(_numVertices);
                else
                    _uVSets?.Clear();
            }
        }

        public bool HasTangents
        {
            get =>
                (_bSDataFlags != null && _bSDataFlags.HasTangents) ||
                (_dataFlags != null && _dataFlags.NBTMethod != Enums.NiNBTMethod.NBT_METHOD_NONE);

            set
            {
                if (_dataFlags != null && !value)
                {
                    // Only remove NBT method for false and don't set it for true
                    _dataFlags.NBTMethod = Enums.NiNBTMethod.NBT_METHOD_NONE;
                }
                else
                {
                    _bSDataFlags ??= new Bitfields.BSGeometryDataFlags();
                    _bSDataFlags.HasTangents = value;
                }

                if (value)
                {
                    _tangents = _tangents.Resize(_numVertices);
                    _bitangents = _bitangents.Resize(_numVertices);
                }
                else
                {
                    _tangents?.Clear();
                    _bitangents?.Clear();
                }
            }
        }

        public void SetTangentsFlag(bool hasTangents)
        {
            if (_dataFlags != null)
                _dataFlags.NBTMethod = hasTangents ? Enums.NiNBTMethod.NBT_METHOD_NDL : Enums.NiNBTMethod.NBT_METHOD_NONE;

            if (_bSDataFlags != null)
                _bSDataFlags.HasTangents = hasTangents;
        }

        public List<Vector3> Vertices
        {
            get => _vertices;
            set
            {
                int count = value.Count;
                if (count > ushort.MaxValue)
                    _vertices = value.Take(ushort.MaxValue).ToList();
                else
                    _vertices = value;

                _numVertices = (ushort)_vertices.Count;
                HasVertices = true;
            }
        }

        public List<Vector3> Normals
        {
            get => _normals;
            set
            {
                int count = value.Count;
                if (count > ushort.MaxValue)
                    _normals = value.Take(ushort.MaxValue).ToList();
                else
                    _normals = value;

                HasNormals = true;
            }
        }

        public List<Vector3> Tangents
        {
            get => _tangents;
            set
            {
                int count = value.Count;
                if (count > ushort.MaxValue)
                    _tangents = value.Take(ushort.MaxValue).ToList();
                else
                    _tangents = value;

                HasTangents = true;
            }
        }

        public List<Vector3> Bitangents
        {
            get => _bitangents;
            set
            {
                int count = value.Count;
                if (count > ushort.MaxValue)
                    _bitangents = value.Take(ushort.MaxValue).ToList();
                else
                    _bitangents = value;

                HasTangents = true;
            }
        }

        public List<Color4> VertexColors
        {
            get => _vertexColors;
            set
            {
                int count = value.Count;
                if (count > ushort.MaxValue)
                    _vertexColors = value.Take(ushort.MaxValue).ToList();
                else
                    _vertexColors = value;

                HasVertexColors = true;
            }
        }

        public List<TexCoord> UVSets
        {
            get => _uVSets;
            set
            {
                // FIXME: If multiple UV sets, check max elements accordingly
                _uVSets = value;
                HasUVs = true;
            }
        }

        public int GroupID
        {
            get => _groupID;
            set => _groupID = value;
        }

        public byte CompressFlags
        {
            get => _compressFlags;
            set => _compressFlags = value;
        }

        public uint MaterialCRC
        {
            get => _materialCRC;
            set => _materialCRC = value;
        }

        public byte KeepFlags
        {
            get => _keepFlags;
            set => _keepFlags = value;
        }

        public bool HasAdditionalData => !AdditionalDataRef?.IsEmpty() ?? false;
        public NiBlockRef<AbstractAdditionalGeometryData> AdditionalDataRef { get => _additionalData; set => _additionalData = value; }

        public virtual int NumTriangles => 0;

        public virtual List<Triangle> Triangles
        {
            get => null;
            set { }
        }

        public NiGeometryData()
        {
            _hasVertexColors = false;
            _hasNormals = false;
            _hasUV = false;
            _hasDIV2Floats = false;
        }

        internal void Create(List<Vector3> vertexPositions, List<TexCoord> uvs, List<Vector3> normals)
        {
            if (vertexPositions == null)
            {
                _numVertices = 0;
                _vertices = _vertices.Resize(0);
                return;
            }

            if (vertexPositions.Count > ushort.MaxValue)
                _numVertices = ushort.MaxValue;
            else
                _numVertices = (ushort)vertexPositions.Count;

            if (_numVertices > 0)
                _hasVertices = true;

            _vertices = _vertices.Resize(_numVertices);
            var verticesSpan = CollectionsMarshal.AsSpan(_vertices);

            for (ushort v = 0; v < verticesSpan.Length; v++)
                verticesSpan[v] = vertexPositions[v];

            Bounds = new BoundingSphere(vertexPositions);

            if (uvs != null)
            {
                if (uvs.Count == _numVertices)
                {
                    HasUVs = true;

                    var uvSetsSpan = CollectionsMarshal.AsSpan(_uVSets);

                    for (int uv = 0; uv < _uVSets.Count; uv++)
                    {
                        uvSetsSpan[uv].U = uvs[uv].U;
                        uvSetsSpan[uv].V = uvs[uv].V;
                    }
                }
                else
                {
                    HasUVs = false;
                }
            }
            else
            {
                HasUVs = false;
            }

            if (normals != null && normals.Count == _numVertices)
            {
                HasNormals = true;

                var normalsSpan = CollectionsMarshal.AsSpan(_normals);
                for (int n = 0; n < normalsSpan.Length; n++)
                {
                    normalsSpan[n].X = normals[n].X;
                    normalsSpan[n].Y = normals[n].Y;
                    normalsSpan[n].Z = normals[n].Z;
                }

                CalcTangentSpace();
            }
            else
            {
                HasNormals = false;
                HasTangents = false;
            }
        }

        public void CalcTangentSpace()
        {
            HasTangents = true;
        }
    }
}