using NiflySharp.Bitfields;
using NiflySharp.Enums;
using NiflySharp.Extensions;
using NiflySharp.Stream;
using NiflySharp.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace NiflySharp.Blocks
{
    public partial class BSTriShape : INiShape
    {
        /// <summary>
        /// No separate geometry data on BSTriShape.
        /// </summary>
        public NiGeometryData GeometryData { get => null; set { } }

        public bool HasData => false;
        public NiBlockRef<NiGeometryData> DataRef { get => null; set { } }

        public bool HasSkinInstance => !SkinInstanceRef?.IsEmpty() ?? false;
        INiRef INiShape.SkinInstanceRef { get => _skin; set => throw new NotSupportedException("Ref can only be set using the explicit block ref type."); }
        public NiBlockRef<NiObject> SkinInstanceRef { get => _skin; set => _skin = value; }

        public bool HasShaderProperty => !ShaderPropertyRef?.IsEmpty() ?? false;
        INiRef INiShape.ShaderPropertyRef { get => _shaderProperty; set => throw new NotSupportedException("Ref can only be set using the explicit block ref type."); }
        public NiBlockRef<BSShaderProperty> ShaderPropertyRef { get => _shaderProperty; set => _shaderProperty = value; }

        public bool HasAlphaProperty => !AlphaPropertyRef?.IsEmpty() ?? false;
        public NiBlockRef<NiAlphaProperty> AlphaPropertyRef { get => _alphaProperty; set => _alphaProperty = value; }

        public BoundingSphere Bounds
        {
            get => new(_boundingSphere.Center, _boundingSphere.Radius);
            set => _boundingSphere = new NiBound() { Center = value.Center, Radius = value.Radius };
        }

        internal List<Vector3> rawVertexPositions;  // temporary copy filled by UpdateRawVertices function
        internal List<Vector3> rawNormals;          // temporary copy filled by UpdateRawNormals function
        internal List<Vector3> rawUVs;              // temporary copy filled by UpdateRawTangents function
        internal List<Vector3> rawBitangents;       // temporary copy filled by UpdateRawBitangents function
        internal List<TexCoord> rawUvs;             // temporary copy filled by UpdateRawUvs function
        internal List<Color4> rawVertexColors;      // temporary copy filled by UpdateRawColors function
        internal List<float> rawEyeData;            // temporary copy filled by UpdateRawEyeData function

        public BSTriShape()
        {
            _boundMinMax = new float[6];
            _vertexDesc = new BSVertexDesc();
            _vertexDesc.VertexAttributes |=
                VertexAttribute.Vertex |
                VertexAttribute.UVs |
                VertexAttribute.Normals |
                VertexAttribute.Tangents |
                VertexAttribute.Skinned;
        }

        public BSTriShape(NiVersion version, List<Vector3> vertices, List<Triangle> triangles, List<TexCoord> uvSets, List<Vector3> normals) : this()
        {
            Create(version, vertices, triangles, uvSets, normals);
        }

        public new void BeforeSync(NiStreamReversible stream)
        {
            if (stream.CurrentMode == NiStreamReversible.Mode.Write)
            {
                uint numTris = _numTriangles_ui > 0 ? _numTriangles_ui : _numTriangles_us;

                if (stream.Version.IsSSE() && IsSkinned)
                {
                    // Triangle and vertex data is in partition instead
                    _numVertices = 0;
                    _numTriangles_ui = 0;
                    _numTriangles_us = 0;
                    _dataSize = 0;
                }
                else
                {
                    _dataSize = (_vertexDesc.VertexDataSize * _numVertices * 4) + (numTris * 6);
                }

                if (stream.Version.IsSSE())
                {
                    if (_particleDataSize > 0)
                        _particleDataSize = (uint)((_numVertices * 6) + (numTris * 3));

                    if (this is BSDynamicTriShape bsdts)
                    {
                        _numVertices = (ushort)(bsdts.Vertices?.Count ?? 0);
                    }
                }
            }
        }

        public void UpdateBounds()
        {
            // FIXME: For skinned meshes, use vertex data from NiSkinPartition

            List<Vector3> vertices = null;
            if (_vertexData_List_BSVDSSE != null)
            {
                vertices = _vertexData_List_BSVDSSE.Select(v => v.Vertex).ToList();
            }
            else if (_vertexData_List_BSVD != null)
            {
                if (_vertexDesc.VertexAttributes.HasFlag(VertexAttribute.Full_Precision))
                    vertices = _vertexData_List_BSVD.Select(v => v.Vertex_V3).ToList();
                else
                    vertices = _vertexData_List_BSVD.Select(v => new Vector3((float)v.Vertex_HV3.X, (float)v.Vertex_HV3.Y, (float)v.Vertex_HV3.Z)).ToList();
            }

            if (vertices != null)
                Bounds = new BoundingSphere(vertices);
        }

        public bool HasVertices
        {
            get
            {
                return _vertexDesc.VertexAttributes.HasFlag(VertexAttribute.Vertex);
            }
            set
            {
                if (value)
                {
                    _vertexDesc.VertexAttributes |= VertexAttribute.Vertex;

                    if (_vertexData_List_BSVDSSE != null)
                        _vertexData_List_BSVDSSE.Resize(_numVertices);
                    else
                        _vertexData_List_BSVD?.Resize(_numVertices);
                }
                else
                {
                    _vertexDesc.VertexAttributes &= ~VertexAttribute.Vertex;
                    _numVertices = 0;

                    if (_vertexData_List_BSVDSSE != null)
                        _vertexData_List_BSVDSSE.Clear();
                    else
                        _vertexData_List_BSVD?.Clear();

                    HasUVs = false;
                    HasNormals = false;
                    HasTangents = false;
                    HasVertexColors = false;
                    IsSkinned = false;
                }
            }
        }

        public bool HasUVs
        {
            get
            {
                return _vertexDesc.VertexAttributes.HasFlag(VertexAttribute.UVs);
            }
            set
            {
                if (value)
                    _vertexDesc.VertexAttributes |= VertexAttribute.UVs;
                else
                    _vertexDesc.VertexAttributes &= ~VertexAttribute.UVs;
            }
        }

        public bool HasSecondUVs
        {
            get
            {
                return _vertexDesc.VertexAttributes.HasFlag(VertexAttribute.UVs_2);
            }
            set
            {
                if (value)
                    _vertexDesc.VertexAttributes |= VertexAttribute.UVs_2;
                else
                    _vertexDesc.VertexAttributes &= ~VertexAttribute.UVs_2;
            }
        }

        public bool HasNormals
        {
            get
            {
                return _vertexDesc.VertexAttributes.HasFlag(VertexAttribute.Normals);
            }
            set
            {
                if (value)
                    _vertexDesc.VertexAttributes |= VertexAttribute.Normals;
                else
                    _vertexDesc.VertexAttributes &= ~VertexAttribute.Normals;
            }
        }

        public bool HasTangents
        {
            get
            {
                return _vertexDesc.VertexAttributes.HasFlag(VertexAttribute.Tangents);
            }
            set
            {
                if (value)
                    _vertexDesc.VertexAttributes |= VertexAttribute.Tangents;
                else
                    _vertexDesc.VertexAttributes &= ~VertexAttribute.Tangents;
            }
        }

        public bool HasVertexColors
        {
            get
            {
                return _vertexDesc.VertexAttributes.HasFlag(VertexAttribute.Vertex_Colors);
            }
            set
            {
                if (value)
                {
                    if (_vertexDesc.VertexAttributes.HasFlag(VertexAttribute.Vertex_Colors))
                    {
                        // Make all color values white
                        if (_vertexData_List_BSVDSSE != null)
                        {
                            var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVDSSE);
                            foreach (ref var v in vertexDataSpan)
                            {
                                v._vertexColors._r = 255;
                                v._vertexColors._g = 255;
                                v._vertexColors._b = 255;
                                v._vertexColors._a = 255;
                            }
                        }
                        else if (_vertexData_List_BSVD != null)
                        {
                            var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVD);
                            foreach (ref var v in vertexDataSpan)
                            {
                                v._vertexColors._r = 255;
                                v._vertexColors._g = 255;
                                v._vertexColors._b = 255;
                                v._vertexColors._a = 255;
                            }
                        }
                    }

                    _vertexDesc.VertexAttributes |= VertexAttribute.Vertex_Colors;
                }
                else
                    _vertexDesc.VertexAttributes &= ~VertexAttribute.Vertex_Colors;
            }
        }

        public bool IsSkinned
        {
            get
            {
                return _vertexDesc.VertexAttributes.HasFlag(VertexAttribute.Skinned);
            }
            set
            {
                if (value)
                    _vertexDesc.VertexAttributes |= VertexAttribute.Skinned;
                else
                    _vertexDesc.VertexAttributes &= ~VertexAttribute.Skinned;
            }
        }

        public bool HasEyeData
        {
            get
            {
                return _vertexDesc.VertexAttributes.HasFlag(VertexAttribute.Eye_Data);
            }
            set
            {
                if (value)
                    _vertexDesc.VertexAttributes |= VertexAttribute.Eye_Data;
                else
                    _vertexDesc.VertexAttributes &= ~VertexAttribute.Eye_Data;
            }
        }

        public bool CanChangePrecision
        {
            get
            {
                return HasVertices;
            }
        }

        public bool IsFullPrecision
        {
            get
            {
                return _vertexDesc.VertexAttributes.HasFlag(VertexAttribute.Full_Precision);
            }
            set
            {
                if (!CanChangePrecision)
                    return;

                if (value)
                    _vertexDesc.VertexAttributes |= VertexAttribute.Full_Precision;
                else
                    _vertexDesc.VertexAttributes &= ~VertexAttribute.Full_Precision;
            }
        }

        public ushort VertexCount
        {
            get
            {
                if (_vertexData_List_BSVDSSE != null)
                    return (ushort)_vertexData_List_BSVDSSE.Count;
                else if (_vertexData_List_BSVD != null)
                    return (ushort)_vertexData_List_BSVD.Count;
                else
                    return 0;
            }
        }

        public int TriangleCount => _triangles?.Count ?? 0;

        public List<Triangle> Triangles => _triangles;

        public void SetTriangles(NiVersion version, List<Triangle> triangles)
        {
            if (version.IsSSE())
            {
                if (triangles.Count > ushort.MaxValue)
                    return;

                _numTriangles_us = (ushort)triangles.Count;
            }
            else
            {
                if (triangles.Count > int.MaxValue) // Really uint
                    return;

                _numTriangles_ui = (uint)triangles.Count;
            }

            _triangles = _triangles.Resize(triangles.Count);
            triangles.CopyTo(CollectionsMarshal.AsSpan(_triangles));
        }

        public BSVertexDesc VertexDesc { get => _vertexDesc; private set => _vertexDesc = value; }
        public List<BSVertexData> VertexData { get => _vertexData_List_BSVD; }
        public List<BSVertexDataSSE> VertexDataSSE { get => _vertexData_List_BSVDSSE; }

        public void SetVertexData(List<BSVertexData> vertexData)
        {
            if (vertexData.Count > ushort.MaxValue)
                return;

            _numVertices = (ushort)vertexData.Count;
            _vertexData_List_BSVD = _vertexData_List_BSVD.Resize(_numVertices);
            vertexData.CopyTo(CollectionsMarshal.AsSpan(_vertexData_List_BSVD));
        }

        public void SetVertexDataSSE(List<BSVertexDataSSE> vertexData)
        {
            if (vertexData.Count > ushort.MaxValue)
                return;

            _numVertices = (ushort)vertexData.Count;
            _vertexData_List_BSVDSSE = _vertexData_List_BSVDSSE.Resize(_numVertices);
            vertexData.CopyTo(CollectionsMarshal.AsSpan(_vertexData_List_BSVDSSE));
        }

        /// <summary>
        /// Not in file
        /// </summary>
        public uint VertexSize { get; private set; }

        public uint DataSize { get => _dataSize; private set => _dataSize = value; }

        public uint ParticleDataSize { get => _particleDataSize; set => _particleDataSize = value; }
        public List<HalfVector3> ParticleVertices { get => _particleVertices; set => _particleVertices = value; }
        public List<HalfVector3> ParticleNormals { get => _particleNormals; set => _particleNormals = value; }
        public List<Triangle> ParticleTriangles { get => _particleTriangles; set => _particleTriangles = value; }

        public void Create(NiVersion version, List<Vector3> vertices, List<Triangle> triangles, List<TexCoord> uvSets, List<Vector3> normals)
        {
            const ushort maxVertIndex = ushort.MaxValue;

            int vertCount = vertices?.Count ?? 0;
            if (vertCount > maxVertIndex)
                _numVertices = maxVertIndex;
            else
                _numVertices = (ushort)vertCount;

            bool isSSE = version.UserVersion >= 12 && version.StreamVersion < 130;
            uint maxTriIndex = isSSE ? ushort.MaxValue : uint.MaxValue;

            int triCount = triangles?.Count ?? 0;
            if (_numVertices == 0)
                triCount = 0;
            else if (triCount > maxTriIndex)
                triCount = (int)maxTriIndex;

            if (isSSE)
            {
                _numTriangles_us = (ushort)triCount;
                _numTriangles_ui = 0;
            }
            else
            {
                _numTriangles_us = 0;
                _numTriangles_ui = (uint)triCount;
            }

            if (uvSets != null && uvSets.Count != _numVertices)
                HasUVs = false;

            if (isSSE)
            {
                _vertexData_List_BSVDSSE = _vertexData_List_BSVDSSE.Resize(_numVertices);

                var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVDSSE);

                for (int i = 0; i < _numVertices; i++)
                {
                    ref var vertex = ref vertexDataSpan[i];
                    vertex._vertex = vertices[i];
                    vertex._normal._x = 0;
                    vertex._normal._y = 0;
                    vertex._normal._z = 0;
                    vertex._vertexColors._r = 255;
                    vertex._vertexColors._g = 255;
                    vertex._vertexColors._b = 255;
                    vertex._vertexColors._a = 255;
                    vertex._boneWeights ??= new Half[4];
                    vertex._boneIndices ??= new byte[4];

                    if (uvSets != null && uvSets.Count == _numVertices)
                    {
                        vertex._uV._u = (Half)uvSets[i].U;
                        vertex._uV._v = (Half)uvSets[i].V;
                    }

                    //Array.Clear(vertex._boneWeights);
                    //Array.Clear(vertex._boneIndices);
                }
            }
            else
            {
                _vertexData_List_BSVD = _vertexData_List_BSVD.Resize(_numVertices);

                var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVD);

                for (int i = 0; i < _numVertices; i++)
                {
                    ref var vertex = ref vertexDataSpan[i];

                    if (IsFullPrecision)
                    {
                        vertex._vertex_V3 = vertices[i];
                        vertex._bitangentX_fl = 0.0f;
                    }
                    else
                    {
                        vertex._vertex_HV3._x = (Half)vertices[i].X;
                        vertex._vertex_HV3._y = (Half)vertices[i].Y;
                        vertex._vertex_HV3._z = (Half)vertices[i].Z;
                        vertex._bitangentX_SH = Half.Zero;
                    }

                    vertex._normal._x = 0;
                    vertex._normal._y = 0;
                    vertex._normal._z = 0;
                    vertex._vertexColors._r = 255;
                    vertex._vertexColors._g = 255;
                    vertex._vertexColors._b = 255;
                    vertex._vertexColors._a = 255;
                    vertex._boneWeights ??= new Half[4];
                    vertex._boneIndices ??= new byte[4];

                    if (uvSets != null && uvSets.Count == _numVertices)
                    {
                        vertex._uV._u = (Half)uvSets[i].U;
                        vertex._uV._v = (Half)uvSets[i].V;
                    }

                    //Array.Clear(vertex._boneWeights);
                    //Array.Clear(vertex._boneIndices);
                }
            }

            _triangles = triangles.Take(triCount).ToList();

            UpdateRawVertexPositions();

            var bounds = new BoundingSphere(rawVertexPositions);
            _boundingSphere = new NiBound()
            {
                Center = bounds.Center,
                Radius = bounds.Radius
            };

            if (normals != null && normals.Count == _numVertices)
            {
                SetNormals(normals);
                CalcTangentSpace();
            }
            else
            {
                HasNormals = false;
                HasTangents = false;
            }
        }

        /// <summary>
        /// Sets mesh vertex positions and enables the vertices flag.
        /// </summary>
        /// <param name="vertices">Positions for all vertices</param>
        public void SetVertexPositions(List<Vector3> vertices)
        {
            if (vertices.Count != _numVertices)
                return;

            HasVertices = true;

            rawVertexPositions = rawVertexPositions.Resize(_numVertices);
            var rawVerticesSpan = CollectionsMarshal.AsSpan(rawVertexPositions);

            if (_vertexData_List_BSVDSSE != null)
            {
                var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVDSSE);

                for (int i = 0; i < _numVertices; i++)
                {
                    rawVerticesSpan[i] = vertices[i];
                    vertexDataSpan[i]._vertex.X = vertices[i].X;
                    vertexDataSpan[i]._vertex.Y = vertices[i].Y;
                    vertexDataSpan[i]._vertex.Z = vertices[i].Z;
                }
            }
            else if (_vertexData_List_BSVD != null)
            {
                var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVD);

                for (int i = 0; i < _numVertices; i++)
                {
                    rawVerticesSpan[i] = vertices[i];
                    if (IsFullPrecision)
                    {
                        vertexDataSpan[i]._vertex_V3.X = vertices[i].X;
                        vertexDataSpan[i]._vertex_V3.Y = vertices[i].Y;
                        vertexDataSpan[i]._vertex_V3.Z = vertices[i].Z;
                    }
                    else
                    {
                        vertexDataSpan[i]._vertex_HV3._x = (Half)vertices[i].X;
                        vertexDataSpan[i]._vertex_HV3._y = (Half)vertices[i].Y;
                        vertexDataSpan[i]._vertex_HV3._z = (Half)vertices[i].Z;
                    }
                }
            }
        }

        /// <summary>
        /// Sets mesh normals and enables the normals flag.
        /// </summary>
        /// <param name="normals">Normals for all vertices</param>
        public void SetNormals(List<Vector3> normals)
        {
            if (normals.Count != _numVertices)
                return;

            HasNormals = true;

            rawNormals = rawNormals.Resize(_numVertices);
            var rawNormalsSpan = CollectionsMarshal.AsSpan(rawNormals);

            if (_vertexData_List_BSVDSSE != null)
            {
                var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVDSSE);

                for (int i = 0; i < _numVertices; i++)
                {
                    rawNormalsSpan[i] = normals[i];
                    vertexDataSpan[i]._normal._x = (sbyte)Math.Round((normals[i].X + 1.0f) / 2.0f * 255.0f);
                    vertexDataSpan[i]._normal._y = (sbyte)Math.Round((normals[i].Y + 1.0f) / 2.0f * 255.0f);
                    vertexDataSpan[i]._normal._z = (sbyte)Math.Round((normals[i].Z + 1.0f) / 2.0f * 255.0f);
                }
            }
            else if (_vertexData_List_BSVD != null)
            {
                var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVD);

                for (int i = 0; i < _numVertices; i++)
                {
                    rawNormalsSpan[i] = normals[i];
                    vertexDataSpan[i]._normal._x = (sbyte)Math.Round((normals[i].X + 1.0f) / 2.0f * 255.0f);
                    vertexDataSpan[i]._normal._y = (sbyte)Math.Round((normals[i].Y + 1.0f) / 2.0f * 255.0f);
                    vertexDataSpan[i]._normal._z = (sbyte)Math.Round((normals[i].Z + 1.0f) / 2.0f * 255.0f);
                }
            }
        }

        /// <summary>
        /// Sets mesh tangents and enables the tangents flag.
        /// </summary>
        /// <param name="tangents">Tangents for all vertices</param>
        public void SetTangents(List<Vector3> tangents)
        {
            if (tangents.Count != _numVertices)
                return;

            HasTangents = true;

            rawUVs = rawUVs.Resize(_numVertices);
            var rawTangentsSpan = CollectionsMarshal.AsSpan(rawUVs);

            if (_vertexData_List_BSVDSSE != null)
            {
                var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVDSSE);

                for (int i = 0; i < _numVertices; i++)
                {
                    rawTangentsSpan[i] = tangents[i];
                    vertexDataSpan[i]._tangent._x = (sbyte)Math.Round((tangents[i].X + 1.0f) / 2.0f * 255.0f);
                    vertexDataSpan[i]._tangent._y = (sbyte)Math.Round((tangents[i].Y + 1.0f) / 2.0f * 255.0f);
                    vertexDataSpan[i]._tangent._z = (sbyte)Math.Round((tangents[i].Z + 1.0f) / 2.0f * 255.0f);
                }
            }
            else if (_vertexData_List_BSVD != null)
            {
                var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVD);

                for (int i = 0; i < _numVertices; i++)
                {
                    rawTangentsSpan[i] = tangents[i];
                    vertexDataSpan[i]._tangent._x = (sbyte)Math.Round((tangents[i].X + 1.0f) / 2.0f * 255.0f);
                    vertexDataSpan[i]._tangent._y = (sbyte)Math.Round((tangents[i].Y + 1.0f) / 2.0f * 255.0f);
                    vertexDataSpan[i]._tangent._z = (sbyte)Math.Round((tangents[i].Z + 1.0f) / 2.0f * 255.0f);
                }
            }
        }

        /// <summary>
        /// Sets mesh bitangents and enables the tangents flag.
        /// </summary>
        /// <param name="bitangents">Bitangents for all vertices</param>
        public void SetBitangents(List<Vector3> bitangents)
        {
            if (bitangents.Count != _numVertices)
                return;

            HasTangents = true;

            rawBitangents = rawBitangents.Resize(_numVertices);
            var rawBitangentsSpan = CollectionsMarshal.AsSpan(rawBitangents);

            if (_vertexData_List_BSVDSSE != null)
            {
                var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVDSSE);

                for (int i = 0; i < _numVertices; i++)
                {
                    rawBitangentsSpan[i] = bitangents[i];
                    vertexDataSpan[i]._bitangentX = bitangents[i].X;
                    vertexDataSpan[i]._bitangentY = (sbyte)Math.Round((bitangents[i].Y + 1.0f) / 2.0f * 255.0f);
                    vertexDataSpan[i]._bitangentZ = (sbyte)Math.Round((bitangents[i].Z + 1.0f) / 2.0f * 255.0f);
                }
            }
            else if (_vertexData_List_BSVD != null)
            {
                var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVD);

                for (int i = 0; i < _numVertices; i++)
                {
                    rawBitangentsSpan[i] = bitangents[i];
                    if (IsFullPrecision)
                        vertexDataSpan[i]._bitangentX_fl = bitangents[i].X;
                    else
                        vertexDataSpan[i]._bitangentX_SH = (Half)bitangents[i].X;
                    vertexDataSpan[i]._bitangentY = (sbyte)Math.Round((bitangents[i].Y + 1.0f) / 2.0f * 255.0f);
                    vertexDataSpan[i]._bitangentZ = (sbyte)Math.Round((bitangents[i].Z + 1.0f) / 2.0f * 255.0f);
                }
            }
        }

        /// <summary>
        /// Sets mesh UVs (texture coordinates) and enables the UV flag.
        /// </summary>
        /// <param name="uvs">UVs for all vertices</param>
        public void SetUVs(List<Vector3> uvs)
        {
            if (uvs.Count != _numVertices)
                return;

            HasUVs = true;

            rawUVs = rawUVs.Resize(_numVertices);
            var rawUVsSpan = CollectionsMarshal.AsSpan(rawUVs);

            if (_vertexData_List_BSVDSSE != null)
            {
                var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVDSSE);

                for (int i = 0; i < _numVertices; i++)
                {
                    rawUVsSpan[i] = uvs[i];
                    vertexDataSpan[i]._uV._u = (Half)uvs[i].X;
                    vertexDataSpan[i]._uV._v = (Half)uvs[i].Y;
                }
            }
            else if (_vertexData_List_BSVD != null)
            {
                var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVD);

                for (int i = 0; i < _numVertices; i++)
                {
                    rawUVsSpan[i] = uvs[i];
                    vertexDataSpan[i]._uV._u = (Half)uvs[i].X;
                    vertexDataSpan[i]._uV._v = (Half)uvs[i].Y;
                }
            }
        }

        /// <summary>
        /// Sets mesh vertex colors and enables the vertex colors flag.
        /// </summary>
        /// <param name="colors">Colors for all vertices</param>
        public void SetVertexColors(List<Color4> colors)
        {
            if (colors.Count != _numVertices)
                return;

            HasVertexColors = true;

            rawVertexColors = rawVertexColors.Resize(_numVertices);
            var rawVertexColorsSpan = CollectionsMarshal.AsSpan(rawVertexColors);

            if (_vertexData_List_BSVDSSE != null)
            {
                var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVDSSE);

                for (int i = 0; i < _numVertices; i++)
                {
                    rawVertexColorsSpan[i] = colors[i];

                    float value = Math.Max(0.0f, Math.Min(1.0f, colors[i]._r));
                    vertexDataSpan[i]._vertexColors._r = (byte)Math.Floor(value == 1.0f ? 255.0f : value * 256.0f);

                    value = Math.Max(0.0f, Math.Min(1.0f, colors[i]._g));
                    vertexDataSpan[i]._vertexColors._g = (byte)Math.Floor(value == 1.0f ? 255.0f : value * 256.0f);

                    value = Math.Max(0.0f, Math.Min(1.0f, colors[i]._b));
                    vertexDataSpan[i]._vertexColors._b = (byte)Math.Floor(value == 1.0f ? 255.0f : value * 256.0f);

                    value = Math.Max(0.0f, Math.Min(1.0f, colors[i]._a));
                    vertexDataSpan[i]._vertexColors._a = (byte)Math.Floor(value == 1.0f ? 255.0f : value * 256.0f);
                }
            }
            else if (_vertexData_List_BSVD != null)
            {
                var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVD);

                for (int i = 0; i < _numVertices; i++)
                {
                    rawVertexColorsSpan[i] = colors[i];

                    float value = Math.Max(0.0f, Math.Min(1.0f, colors[i]._r));
                    vertexDataSpan[i]._vertexColors._r = (byte)Math.Floor(value == 1.0f ? 255.0f : value * 256.0f);

                    value = Math.Max(0.0f, Math.Min(1.0f, colors[i]._g));
                    vertexDataSpan[i]._vertexColors._g = (byte)Math.Floor(value == 1.0f ? 255.0f : value * 256.0f);

                    value = Math.Max(0.0f, Math.Min(1.0f, colors[i]._b));
                    vertexDataSpan[i]._vertexColors._b = (byte)Math.Floor(value == 1.0f ? 255.0f : value * 256.0f);

                    value = Math.Max(0.0f, Math.Min(1.0f, colors[i]._a));
                    vertexDataSpan[i]._vertexColors._a = (byte)Math.Floor(value == 1.0f ? 255.0f : value * 256.0f);
                }
            }
        }

        /// <summary>
        /// Sets mesh eye data and enables the eye data flag.
        /// </summary>
        /// <param name="eyeData">Eye data for all vertices</param>
        public void SetEyeData(List<float> eyeData)
        {
            if (eyeData.Count != _numVertices)
                return;

            HasEyeData = true;

            rawEyeData = rawEyeData.Resize(_numVertices);
            var rawEyeDataSpan = CollectionsMarshal.AsSpan(rawEyeData);

            if (_vertexData_List_BSVDSSE != null)
            {
                var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVDSSE);

                for (int i = 0; i < _numVertices; i++)
                {
                    rawEyeDataSpan[i] = eyeData[i];
                    vertexDataSpan[i]._eyeData = eyeData[i];
                }
            }
            else if (_vertexData_List_BSVD != null)
            {
                var vertexDataSpan = CollectionsMarshal.AsSpan(_vertexData_List_BSVD);

                for (int i = 0; i < _numVertices; i++)
                {
                    rawEyeDataSpan[i] = eyeData[i];
                    vertexDataSpan[i]._eyeData = eyeData[i];
                }
            }
        }

        public void CalcTangentSpace()
        {
            if (!HasNormals || !HasUVs)
                return;

            if (_vertexData_List_BSVDSSE == null && _vertexData_List_BSVD == null)
                return;

            UpdateRawNormals();
            HasTangents = true;

            var tan1 = new List<Vector3>();
            var tan2 = new List<Vector3>();
            tan1.Resize(_numVertices);
            tan2.Resize(_numVertices);

            foreach (var triangle in _triangles)
            {
                int i1 = triangle._v1;
                int i2 = triangle._v2;
                int i3 = triangle._v3;

                if (i1 >= _numVertices || i2 >= _numVertices || i3 >= _numVertices)
                    continue;

                Vector3 v1;
                Vector3 v2;
                Vector3 v3;

                Vector2 w1;
                Vector2 w2;
                Vector2 w3;

                if (_vertexData_List_BSVDSSE != null)
                {
                    v1 = _vertexData_List_BSVDSSE[i1]._vertex;
                    v2 = _vertexData_List_BSVDSSE[i2]._vertex;
                    v3 = _vertexData_List_BSVDSSE[i3]._vertex;

                    w1 = new Vector2((float)_vertexData_List_BSVDSSE[i1]._uV._u, (float)_vertexData_List_BSVDSSE[i1]._uV._v);
                    w2 = new Vector2((float)_vertexData_List_BSVDSSE[i2]._uV._u, (float)_vertexData_List_BSVDSSE[i2]._uV._v);
                    w3 = new Vector2((float)_vertexData_List_BSVDSSE[i3]._uV._u, (float)_vertexData_List_BSVDSSE[i3]._uV._v);
                }
                else if (_vertexData_List_BSVD != null)
                {
                    if (IsFullPrecision)
                    {
                        v1 = _vertexData_List_BSVD[i1]._vertex_V3;
                        v2 = _vertexData_List_BSVD[i2]._vertex_V3;
                        v3 = _vertexData_List_BSVD[i3]._vertex_V3;
                    }
                    else
                    {
                        v1 = new Vector3((float)_vertexData_List_BSVD[i1]._vertex_HV3._x,
                                         (float)_vertexData_List_BSVD[i1]._vertex_HV3._y,
                                         (float)_vertexData_List_BSVD[i1]._vertex_HV3._z);
                        v2 = new Vector3((float)_vertexData_List_BSVD[i2]._vertex_HV3._x,
                                         (float)_vertexData_List_BSVD[i2]._vertex_HV3._y,
                                         (float)_vertexData_List_BSVD[i2]._vertex_HV3._z);
                        v3 = new Vector3((float)_vertexData_List_BSVD[i3]._vertex_HV3._x,
                                         (float)_vertexData_List_BSVD[i3]._vertex_HV3._y,
                                         (float)_vertexData_List_BSVD[i3]._vertex_HV3._z);
                    }

                    w1 = new Vector2((float)_vertexData_List_BSVD[i1]._uV._u, (float)_vertexData_List_BSVD[i1]._uV._v);
                    w2 = new Vector2((float)_vertexData_List_BSVD[i2]._uV._u, (float)_vertexData_List_BSVD[i2]._uV._v);
                    w3 = new Vector2((float)_vertexData_List_BSVD[i3]._uV._u, (float)_vertexData_List_BSVD[i3]._uV._v);
                }
                else
                    return;

                float x1 = v2.X - v1.X;
                float x2 = v3.X - v1.X;
                float y1 = v2.Y - v1.Y;
                float y2 = v3.Y - v1.Y;
                float z1 = v2.Z - v1.Z;
                float z2 = v3.Z - v1.Z;

                float s1 = w2.X - w1.X;
                float s2 = w3.X - w1.X;
                float t1 = w2.Y - w1.Y;
                float t2 = w3.Y - w1.Y;

                float r = s1 * t2 - s2 * t1;
                r = r >= 0.0f ? +1.0f : -1.0f;

                var sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                var tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                sdir = Vector3.Normalize(sdir);
                tdir = Vector3.Normalize(tdir);

                tan1[i1] += tdir;
                tan1[i2] += tdir;
                tan1[i3] += tdir;

                tan2[i1] += sdir;
                tan2[i2] += sdir;
                tan2[i3] += sdir;
            }

            rawBitangents = rawBitangents.Resize(_numVertices);
            rawUVs = rawUVs.Resize(_numVertices);

            var rawNormalsSpan = CollectionsMarshal.AsSpan(rawNormals);
            var rawBitangentsSpan = CollectionsMarshal.AsSpan(rawBitangents);
            var rawTangentsSpan = CollectionsMarshal.AsSpan(rawUVs);

            for (ushort i = 0; i < _numVertices; i++)
            {
                rawTangentsSpan[i] = tan1[i];
                rawBitangentsSpan[i] = tan2[i];

                if (rawTangentsSpan[i] == Vector3.Zero || rawBitangentsSpan[i] == Vector3.Zero)
                {
                    rawTangentsSpan[i].X = rawNormalsSpan[i].Y;
                    rawTangentsSpan[i].Y = rawNormalsSpan[i].Z;
                    rawTangentsSpan[i].Z = rawNormalsSpan[i].X;
                    rawBitangentsSpan[i] = Vector3.Cross(rawNormalsSpan[i], rawTangentsSpan[i]);
                }
                else
                {
                    rawTangentsSpan[i] = Vector3.Normalize(rawTangentsSpan[i]);
                    rawTangentsSpan[i] = rawTangentsSpan[i] - rawNormalsSpan[i] * Vector3.Dot(rawNormalsSpan[i], rawTangentsSpan[i]);
                    rawTangentsSpan[i] = Vector3.Normalize(rawTangentsSpan[i]);

                    rawBitangentsSpan[i] = Vector3.Normalize(rawBitangentsSpan[i]);

                    rawBitangentsSpan[i] = rawBitangentsSpan[i] - rawNormalsSpan[i] * Vector3.Dot(rawNormalsSpan[i], rawBitangentsSpan[i]);
                    rawBitangentsSpan[i] = rawBitangentsSpan[i] - rawTangentsSpan[i] * Vector3.Dot(rawTangentsSpan[i], rawBitangentsSpan[i]);

                    rawBitangentsSpan[i] = Vector3.Normalize(rawBitangentsSpan[i]);
                }

                byte tX = (byte)Math.Round((rawTangentsSpan[i].X + 1.0f) / 2.0f * 255.0f);
                byte tY = (byte)Math.Round((rawTangentsSpan[i].Y + 1.0f) / 2.0f * 255.0f);
                byte tZ = (byte)Math.Round((rawTangentsSpan[i].Z + 1.0f) / 2.0f * 255.0f);

                float btX = rawBitangentsSpan[i].X;
                byte btY = (byte)Math.Round((rawBitangentsSpan[i].Y + 1.0f) / 2.0f * 255.0f);
                byte btZ = (byte)Math.Round((rawBitangentsSpan[i].Z + 1.0f) / 2.0f * 255.0f);

                if (_vertexData_List_BSVDSSE != null)
                {
                    var spanVertexData = CollectionsMarshal.AsSpan(_vertexData_List_BSVDSSE);

                    spanVertexData[i]._tangent._x = (sbyte)tX;
                    spanVertexData[i]._tangent._y = (sbyte)tY;
                    spanVertexData[i]._tangent._z = (sbyte)tZ;

                    spanVertexData[i]._bitangentX = btX;
                    spanVertexData[i]._bitangentY = (sbyte)btY;
                    spanVertexData[i]._bitangentZ = (sbyte)btZ;
                }
                else if (_vertexData_List_BSVD != null)
                {
                    var spanVertexData = CollectionsMarshal.AsSpan(_vertexData_List_BSVD);

                    spanVertexData[i]._tangent._x = (sbyte)tX;
                    spanVertexData[i]._tangent._y = (sbyte)tY;
                    spanVertexData[i]._tangent._z = (sbyte)tZ;

                    if (IsFullPrecision)
                        spanVertexData[i]._bitangentX_fl = btX;
                    else
                        spanVertexData[i]._bitangentX_SH = (Half)btX;

                    spanVertexData[i]._bitangentY = (sbyte)btY;
                    spanVertexData[i]._bitangentZ = (sbyte)btZ;
                }
            }
        }

        public void CalcDataSizes(NiVersion version)
        {
            _vertexDesc ??= new();
            _vertexDesc.ClearAttributeOffsets();

            uint attrOffset = 0;

            if (HasVertices) // "dynamic" meshes are false here
            {
                if (IsFullPrecision || version.StreamVersion == 100)
                    attrOffset += 4; // 4 floats
                else
                    attrOffset += 2;  // 4 half floats
            }

            if (HasUVs)
            {
                _vertexDesc.UV1Offset = attrOffset;
                attrOffset += 1; // 2 half floats
            }

            if (HasSecondUVs)
            {
                _vertexDesc.UV2Offset = attrOffset;
                attrOffset += 1; // 2 half floats
            }

            if (HasNormals)
            {
                _vertexDesc.NormalOffset = attrOffset;
                attrOffset += 1; // 4 bytes

                if (HasTangents)
                {
                    _vertexDesc.TangentOffset = attrOffset;
                    attrOffset += 1; // 4 bytes
                }
            }

            if (HasVertexColors)
            {
                _vertexDesc.ColorOffset = attrOffset;
                attrOffset += 1; // 4 bytes
            }

            if (IsSkinned)
            {
                _vertexDesc.SkinningDataOffset = attrOffset;
                attrOffset += 3;
            }

            if (HasEyeData)
            {
                _vertexDesc.EyeDataOffset = attrOffset;
                attrOffset += 1;
            }

            uint attrPackingCount = attrOffset;

            _vertexDesc.VertexDataSize = attrPackingCount;
            VertexSize = attrPackingCount * 4; // Packing count -> Byte count

            if (this is BSDynamicTriShape)
                _vertexDesc.DynamicVertexSize = 4; // 4 floats

            _dataSize = (VertexSize * _numVertices) + (6 * _numTriangles_ui > 0 ? _numTriangles_ui : _numTriangles_us);
        }

        /// <summary>
        /// Get raw vertex positions of the mesh.
        /// This is a copy and not the original mesh data. Use <see cref="SetVertexPositions(List{Vector3})"/> to set.
        /// </summary>
        public List<Vector3> VertexPositions => UpdateRawVertexPositions();

        /// <summary>
        /// Get raw normals of the mesh.
        /// This is a copy and not the original mesh data. Use <see cref="SetNormals(List{Vector3})"/> to set.
        /// </summary>
        public List<Vector3> Normals => UpdateRawNormals();

        /// <summary>
        /// Get raw tangents of the mesh.
        /// This is a copy and not the original mesh data. Use <see cref="SetTangents(List{Vector3})"/> to set.
        /// </summary>
        public List<Vector3> Tangents => UpdateRawTangents();

        /// <summary>
        /// Get raw bitangents of the mesh.
        /// This is a copy and not the original mesh data. Use <see cref="SetBitangents(List{Vector3})"/> to set.
        /// </summary>
        public List<Vector3> Bitangents => UpdateRawBitangents();

        /// <summary>
        /// Get raw UVs of the mesh.
        /// This is a copy and not the original mesh data. Use <see cref="SetUVs"/> to set.
        /// </summary>
        public List<TexCoord> UVs => UpdateRawUVs();

        /// <summary>
        /// Get raw vertex colors of the mesh.
        /// This is a copy and not the original mesh data. Use <see cref="SetVertexColors(List{Color4})"/> to set.
        /// </summary>
        public List<Color4> VertexColors => UpdateRawVertexColors();

        /// <summary>
        /// Get raw eye data of the mesh.
        /// This is a copy and not the original mesh data. Use <see cref="SetEyeData(List{float})"/> to set.
        /// </summary>
        public List<float> EyeData => UpdateRawEyeData();

        internal List<Vector3> UpdateRawVertexPositions()
        {
            rawVertexPositions = rawVertexPositions.Resize(_numVertices);

            var spanRawVertices = CollectionsMarshal.AsSpan(rawVertexPositions);

            if (_vertexData_List_BSVDSSE != null)
            {
                for (ushort i = 0; i < _numVertices; i++)
                    spanRawVertices[i] = _vertexData_List_BSVDSSE[i]._vertex;
            }
            else if (_vertexData_List_BSVD != null)
            {
                if (IsFullPrecision)
                {
                    for (ushort i = 0; i < _numVertices; i++)
                        spanRawVertices[i] = _vertexData_List_BSVD[i]._vertex_V3;
                }
                else
                {
                    for (ushort i = 0; i < _numVertices; i++)
                    {
                        spanRawVertices[i].X = (float)_vertexData_List_BSVD[i]._vertex_HV3._x;
                        spanRawVertices[i].Y = (float)_vertexData_List_BSVD[i]._vertex_HV3._y;
                        spanRawVertices[i].Z = (float)_vertexData_List_BSVD[i]._vertex_HV3._z;
                    }
                }
            }

            return rawVertexPositions;
        }

        internal List<Vector3> UpdateRawNormals()
        {
            if (!HasNormals)
            {
                rawNormals = rawNormals.Resize(0);
                return rawNormals;
            }

            rawNormals = rawNormals.Resize(_numVertices);

            var spanRawNormals = CollectionsMarshal.AsSpan(rawNormals);

            if (_vertexData_List_BSVDSSE != null)
            {
                for (ushort i = 0; i < _numVertices; i++)
                {
                    spanRawNormals[i].X = (byte)_vertexData_List_BSVDSSE[i]._normal._x / 255.0f * 2.0f - 1.0f;
                    spanRawNormals[i].Y = (byte)_vertexData_List_BSVDSSE[i]._normal._y / 255.0f * 2.0f - 1.0f;
                    spanRawNormals[i].Z = (byte)_vertexData_List_BSVDSSE[i]._normal._z / 255.0f * 2.0f - 1.0f;
                }
            }
            else if (_vertexData_List_BSVD != null)
            {
                for (ushort i = 0; i < _numVertices; i++)
                {
                    spanRawNormals[i].X = (byte)_vertexData_List_BSVD[i]._normal._x / 255.0f * 2.0f - 1.0f;
                    spanRawNormals[i].Y = (byte)_vertexData_List_BSVD[i]._normal._y / 255.0f * 2.0f - 1.0f;
                    spanRawNormals[i].Z = (byte)_vertexData_List_BSVD[i]._normal._z / 255.0f * 2.0f - 1.0f;
                }
            }

            return rawNormals;
        }

        internal List<Vector3> UpdateRawTangents()
        {
            if (!HasTangents)
            {
                rawUVs = rawUVs.Resize(0);
                return rawUVs;
            }

            rawUVs = rawUVs.Resize(_numVertices);

            var spanRawTangents = CollectionsMarshal.AsSpan(rawUVs);

            if (_vertexData_List_BSVDSSE != null)
            {
                for (ushort i = 0; i < _numVertices; i++)
                {
                    spanRawTangents[i].X = (byte)_vertexData_List_BSVDSSE[i]._tangent._x / 255.0f * 2.0f - 1.0f;
                    spanRawTangents[i].Y = (byte)_vertexData_List_BSVDSSE[i]._tangent._y / 255.0f * 2.0f - 1.0f;
                    spanRawTangents[i].Z = (byte)_vertexData_List_BSVDSSE[i]._tangent._z / 255.0f * 2.0f - 1.0f;
                }
            }
            else if (_vertexData_List_BSVD != null)
            {
                for (ushort i = 0; i < _numVertices; i++)
                {
                    spanRawTangents[i].X = (byte)_vertexData_List_BSVD[i]._tangent._x / 255.0f * 2.0f - 1.0f;
                    spanRawTangents[i].Y = (byte)_vertexData_List_BSVD[i]._tangent._y / 255.0f * 2.0f - 1.0f;
                    spanRawTangents[i].Z = (byte)_vertexData_List_BSVD[i]._tangent._z / 255.0f * 2.0f - 1.0f;
                }
            }

            return rawUVs;
        }

        internal List<Vector3> UpdateRawBitangents()
        {
            if (!HasTangents)
            {
                rawBitangents = rawBitangents.Resize(0);
                return rawBitangents;
            }

            rawBitangents = rawBitangents.Resize(_numVertices);

            var spanRawBiTangents = CollectionsMarshal.AsSpan(rawBitangents);

            if (_vertexData_List_BSVDSSE != null)
            {
                for (ushort i = 0; i < _numVertices; i++)
                {
                    spanRawBiTangents[i].X = (byte)_vertexData_List_BSVDSSE[i]._bitangentX;
                    spanRawBiTangents[i].Y = (byte)_vertexData_List_BSVDSSE[i]._bitangentY / 255.0f * 2.0f - 1.0f;
                    spanRawBiTangents[i].Z = (byte)_vertexData_List_BSVDSSE[i]._bitangentZ / 255.0f * 2.0f - 1.0f;
                }
            }
            else if (_vertexData_List_BSVD != null)
            {
                if (IsFullPrecision)
                {
                    for (ushort i = 0; i < _numVertices; i++)
                    {
                        spanRawBiTangents[i].X = (byte)_vertexData_List_BSVD[i]._bitangentX_fl;
                        spanRawBiTangents[i].Y = (byte)_vertexData_List_BSVD[i]._bitangentY / 255.0f * 2.0f - 1.0f;
                        spanRawBiTangents[i].Z = (byte)_vertexData_List_BSVD[i]._bitangentZ / 255.0f * 2.0f - 1.0f;
                    }
                }
                else
                {
                    for (ushort i = 0; i < _numVertices; i++)
                    {
                        spanRawBiTangents[i].X = (float)_vertexData_List_BSVD[i]._bitangentX_SH;
                        spanRawBiTangents[i].Y = (byte)_vertexData_List_BSVD[i]._bitangentY / 255.0f * 2.0f - 1.0f;
                        spanRawBiTangents[i].Z = (byte)_vertexData_List_BSVD[i]._bitangentZ / 255.0f * 2.0f - 1.0f;
                    }
                }
            }

            return rawBitangents;
        }

        internal List<TexCoord> UpdateRawUVs()
        {
            if (!HasUVs)
            {
                rawUvs = rawUvs.Resize(0);
                return rawUvs;
            }

            rawUvs = rawUvs.Resize(_numVertices);

            var spanRawUVs = CollectionsMarshal.AsSpan(rawUvs);

            if (_vertexData_List_BSVDSSE != null)
            {
                for (ushort i = 0; i < _numVertices; i++)
                {
                    spanRawUVs[i]._u = (float)_vertexData_List_BSVDSSE[i]._uV._u;
                    spanRawUVs[i]._v = (float)_vertexData_List_BSVDSSE[i]._uV._v;
                }
            }
            else if (_vertexData_List_BSVD != null)
            {
                for (ushort i = 0; i < _numVertices; i++)
                {
                    spanRawUVs[i]._u = (float)_vertexData_List_BSVD[i]._uV._u;
                    spanRawUVs[i]._v = (float)_vertexData_List_BSVD[i]._uV._v;
                }
            }

            return rawUvs;
        }

        internal List<Color4> UpdateRawVertexColors()
        {
            if (!HasVertexColors)
            {
                rawVertexColors = rawVertexColors.Resize(0);
                return rawVertexColors;
            }

            rawVertexColors = rawVertexColors.Resize(_numVertices);

            var spanRawColors = CollectionsMarshal.AsSpan(rawVertexColors);

            if (_vertexData_List_BSVDSSE != null)
            {
                for (ushort i = 0; i < _numVertices; i++)
                {
                    spanRawColors[i]._r = _vertexData_List_BSVDSSE[i]._vertexColors._r / 255.0f;
                    spanRawColors[i]._g = _vertexData_List_BSVDSSE[i]._vertexColors._g / 255.0f;
                    spanRawColors[i]._b = _vertexData_List_BSVDSSE[i]._vertexColors._b / 255.0f;
                    spanRawColors[i]._a = _vertexData_List_BSVDSSE[i]._vertexColors._a / 255.0f;
                }
            }
            else if (_vertexData_List_BSVD != null)
            {
                for (ushort i = 0; i < _numVertices; i++)
                {
                    spanRawColors[i]._r = _vertexData_List_BSVD[i]._vertexColors._r / 255.0f;
                    spanRawColors[i]._g = _vertexData_List_BSVD[i]._vertexColors._g / 255.0f;
                    spanRawColors[i]._b = _vertexData_List_BSVD[i]._vertexColors._b / 255.0f;
                    spanRawColors[i]._a = _vertexData_List_BSVD[i]._vertexColors._a / 255.0f;
                }
            }

            return rawVertexColors;
        }

        internal List<float> UpdateRawEyeData()
        {
            if (!HasEyeData)
            {
                rawEyeData = rawEyeData.Resize(0);
                return rawEyeData;
            }

            rawEyeData = rawEyeData.Resize(_numVertices);

            var spanRawEyeData = CollectionsMarshal.AsSpan(rawEyeData);

            if (_vertexData_List_BSVDSSE != null)
            {
                for (ushort i = 0; i < _numVertices; i++)
                    spanRawEyeData[i] = _vertexData_List_BSVDSSE[i]._eyeData;
            }
            else if (_vertexData_List_BSVD != null)
            {
                for (ushort i = 0; i < _numVertices; i++)
                    spanRawEyeData[i] = _vertexData_List_BSVD[i]._eyeData;
            }

            return rawEyeData;
        }
    }
}
