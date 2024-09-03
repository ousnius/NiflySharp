using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NiflySharp.Stream
{
    public class NiStreamReversible
    {
        public enum Mode
        {
            Read,
            Write
        }

        public NiStreamReader In { get; }

        public NiStreamWriter Out { get; }

        public Mode CurrentMode { get; }

        public object Argument { get; set; }

        public NifFile File
        {
            get
            {
                if (CurrentMode == Mode.Read)
                    return In.File;
                else
                    return Out.File;
            }
        }

        public NiVersion Version
        {
            get
            {
                if (CurrentMode == Mode.Read)
                    return In.File.Header.Version;
                else
                    return Out.File.Header.Version;
            }
        }

        public NiStreamReversible(NiStreamReader reader)
        {
            In = reader;
            CurrentMode = Mode.Read;
        }

        public NiStreamReversible(NiStreamWriter writer)
        {
            Out = writer;
            CurrentMode = Mode.Write;
        }

        public void Sync(ref bool b)
        {
            if (CurrentMode == Mode.Read)
            {
                byte bt;
                if (Version.FileVersion <= NiFileVersion.V4_0_0_2)
                    bt = (byte)In.Reader.ReadUInt32();
                else
                    bt = In.Reader.ReadByte();

                b = bt switch
                {
                    0 => false,
                    1 => true,
                    _ => throw new Exception("Byte value for boolean is > 1!")
                };
            }
            else
            {
                byte bt = b ? (byte)1 : (byte)0;
                if (Version.FileVersion <= NiFileVersion.V4_0_0_2)
                    Out.Writer.Write((uint)bt);
                else
                    Out.Writer.Write(bt);
            }
        }

        public void Sync(ref bool? b)
        {
            if (CurrentMode == Mode.Read)
            {
                byte bt;
                if (Version.FileVersion <= NiFileVersion.V4_0_0_2)
                    bt = (byte)In.Reader.ReadUInt32();
                else
                    bt = In.Reader.ReadByte();

                b = bt switch
                {
                    0 => false,
                    1 => true,
                    2 => null,
                    _ => throw new Exception("Byte value for boolean is > 2!")
                };
            }
            else
            {
                byte bt = 0;
                if (!b.HasValue)
                    bt = 2;
                else if (b.Value)
                    bt = 1;

                if (Version.FileVersion <= NiFileVersion.V4_0_0_2)
                    Out.Writer.Write((uint)bt);
                else
                    Out.Writer.Write(bt);
            }
        }

        public void Sync(ref byte b)
        {
            if (CurrentMode == Mode.Read)
                b = In.Reader.ReadByte();
            else
                Out.Writer.Write(b);
        }

        public void Sync(ref byte[] b)
        {
            if (CurrentMode == Mode.Read)
                b = In.Reader.ReadBytes(b.Length);
            else
                Out.Writer.Write(b);
        }

        public void Sync(ref decimal d)
        {
            if (CurrentMode == Mode.Read)
                d = In.Reader.ReadDecimal();
            else
                Out.Writer.Write(d);
        }

        public void Sync(ref double d)
        {
            if (CurrentMode == Mode.Read)
                d = In.Reader.ReadDouble();
            else
                Out.Writer.Write(d);
        }

        public void Sync(ref sbyte b)
        {
            if (CurrentMode == Mode.Read)
                b = In.Reader.ReadSByte();
            else
                Out.Writer.Write(b);
        }

        public void Sync(ref float f)
        {
            if (CurrentMode == Mode.Read)
                f = In.Reader.ReadSingle();
            else
                Out.Writer.Write(f);
        }

        public void Sync(ref Half h)
        {
            if (CurrentMode == Mode.Read)
                h = In.Reader.ReadHalf();
            else
                Out.Writer.Write(h);
        }

        public void Sync(ref short n)
        {
            if (CurrentMode == Mode.Read)
                n = In.Reader.ReadInt16();
            else
                Out.Writer.Write(n);
        }

        public void Sync(ref int n)
        {
            if (CurrentMode == Mode.Read)
                n = In.Reader.ReadInt32();
            else
                Out.Writer.Write(n);
        }

        public void Sync(ref long n)
        {
            if (CurrentMode == Mode.Read)
                n = In.Reader.ReadInt64();
            else
                Out.Writer.Write(n);
        }

        public void Sync(ref ushort n)
        {
            if (CurrentMode == Mode.Read)
                n = In.Reader.ReadUInt16();
            else
                Out.Writer.Write(n);
        }

        public void Sync(ref uint n)
        {
            if (CurrentMode == Mode.Read)
                n = In.Reader.ReadUInt32();
            else
                Out.Writer.Write(n);
        }

        public void Sync(ref ulong n)
        {
            if (CurrentMode == Mode.Read)
                n = In.Reader.ReadUInt64();
            else
                Out.Writer.Write(n);
        }

        public void Sync(ref Vector2 vec)
        {
            if (CurrentMode == Mode.Read)
            {
                vec.X = In.Reader.ReadSingle();
                vec.Y = In.Reader.ReadSingle();
            }
            else
            {
                Out.Writer.Write(vec.X);
                Out.Writer.Write(vec.Y);
            }
        }

        public void Sync(ref Vector3 vec)
        {
            if (CurrentMode == Mode.Read)
            {
                vec.X = In.Reader.ReadSingle();
                vec.Y = In.Reader.ReadSingle();
                vec.Z = In.Reader.ReadSingle();
            }
            else
            {
                Out.Writer.Write(vec.X);
                Out.Writer.Write(vec.Y);
                Out.Writer.Write(vec.Z);
            }
        }

        public void Sync(ref Vector4 vec)
        {
            if (CurrentMode == Mode.Read)
            {
                vec.X = In.Reader.ReadSingle();
                vec.Y = In.Reader.ReadSingle();
                vec.Z = In.Reader.ReadSingle();
                vec.W = In.Reader.ReadSingle();
            }
            else
            {
                Out.Writer.Write(vec.X);
                Out.Writer.Write(vec.Y);
                Out.Writer.Write(vec.Z);
                Out.Writer.Write(vec.W);
            }
        }

        public void Sync(ref Quaternion quat)
        {
            if (CurrentMode == Mode.Read)
            {
                quat.X = In.Reader.ReadSingle();
                quat.Y = In.Reader.ReadSingle();
                quat.Z = In.Reader.ReadSingle();
                quat.W = In.Reader.ReadSingle();
            }
            else
            {
                Out.Writer.Write(quat.X);
                Out.Writer.Write(quat.Y);
                Out.Writer.Write(quat.Z);
                Out.Writer.Write(quat.W);
            }
        }

        public void Sync<T>(ref T streamable) where T : INiStreamable, new()
        {
            streamable ??= new T();

            streamable.Sync(this);
        }

        public static void ResizeListDefaults<T>(ref List<T> list, int size)
        {
            list ??= [];

            if (list.Count == size)
                return;

            list.Clear();
            list.Capacity = size;

            for (int i = 0; i < size; i++)
                list.Add(default);
        }

        public static void ResizeArrayDefaults<T>(ref T[] array, int size)
        {
            array ??= new T[size];

            if (array.Length == size)
                return;

            Array.Resize(ref array, size);

            for (int i = 0; i < size; i++)
                array[i] = default;
        }

        public void SetListSize<T>(ref List<T> list, int size)
        {
            if (CurrentMode == Mode.Write)
            {
                if (list == null)
                {
                    ResizeListDefaults(ref list, size);
                }
                else
                {
                    int cur = list.Count;
                    if (size < cur)
                    {
                        list.RemoveRange(size, cur - size);
                    }
                    else if (size > cur)
                    {
                        if (size > list.Capacity)
                            list.Capacity = size;

                        for (int i = 0; i < size; i++)
                            list.Add(default);
                    }
                }
            }
            else
            {
                ResizeListDefaults(ref list, size);
            }
        }

        public void SetListSize<T>(ref T array, int size) where T : NiRefArray, new()
        {
            array ??= new T();
            array.SetListSize(this, size);
        }

        public void SyncList<SizeT, ValueT>(ref List<ValueT> list)
        {
            var sizeTypeCode = Type.GetTypeCode(typeof(SizeT));
            var valueTypeCode = Type.GetTypeCode(typeof(ValueT));

            list ??= [];

            if (CurrentMode == Mode.Read)
            {
                int size = sizeTypeCode switch
                {
                    TypeCode.SByte or TypeCode.Byte => Convert.ToInt32(In.Reader.ReadByte()),
                    TypeCode.Int16 or TypeCode.UInt16 => Convert.ToInt32(In.Reader.ReadUInt16()),
                    TypeCode.Int32 or TypeCode.UInt32 => Convert.ToInt32(In.Reader.ReadUInt32()),
                    TypeCode.Int64 or TypeCode.UInt64 => Convert.ToInt32(In.Reader.ReadUInt64()),
                    _ => throw new Exception("Size type not supported!"),
                };

                if (size < 0)
                    throw new Exception("Read list size is < 0!");

                list.Clear();
                list.Capacity = size;
            }
            else
            {
                int size = list.Count;

                switch (sizeTypeCode)
                {
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                        var b = Convert.ToByte(size);
                        Out.Writer.Write(b);
                        break;
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                        var us = Convert.ToUInt16(size);
                        Out.Writer.Write(us);
                        break;
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        var ui = Convert.ToUInt32(size);
                        Out.Writer.Write(ui);
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        var ul = Convert.ToUInt64(size);
                        Out.Writer.Write(ul);
                        break;
                    default:
                        throw new Exception("Size type not supported!");
                }
            }

            var span = CollectionsMarshal.AsSpan(list);

            bool isStreamable = typeof(ValueT).IsAssignableTo(typeof(INiStreamable));
            bool isHalf = typeof(ValueT).Equals(typeof(Half));

            for (int i = 0; i < span.Length; i++)
            {
                ref var t = ref span[i];
                if (CurrentMode == Mode.Read)
                    t ??= default;

                switch (valueTypeCode)
                {
                    case TypeCode.SByte:
                        {
                            ref sbyte v = ref Unsafe.As<ValueT, sbyte>(ref t);
                            Sync(ref v);
                        }
                        break;
                    case TypeCode.Byte:
                        {
                            ref byte v = ref Unsafe.As<ValueT, byte>(ref t);
                            Sync(ref v);
                        }
                        break;
                    case TypeCode.Int16:
                        {
                            ref short v = ref Unsafe.As<ValueT, short>(ref t);
                            Sync(ref v);
                        }
                        break;
                    case TypeCode.UInt16:
                        {
                            ref ushort v = ref Unsafe.As<ValueT, ushort>(ref t);
                            Sync(ref v);
                        }
                        break;
                    case TypeCode.Int32:
                        {
                            ref int v = ref Unsafe.As<ValueT, int>(ref t);
                            Sync(ref v);
                        }
                        break;
                    case TypeCode.UInt32:
                        {
                            ref uint v = ref Unsafe.As<ValueT, uint>(ref t);
                            Sync(ref v);
                        }
                        break;
                    case TypeCode.Int64:
                        {
                            ref long v = ref Unsafe.As<ValueT, long>(ref t);
                            Sync(ref v);
                        }
                        break;
                    case TypeCode.UInt64:
                        {
                            ref ulong v = ref Unsafe.As<ValueT, ulong>(ref t);
                            Sync(ref v);
                        }
                        break;
                    case TypeCode.Boolean:
                        {
                            ref bool v = ref Unsafe.As<ValueT, bool>(ref t);
                            Sync(ref v);
                        }
                        break;
                    case TypeCode.Single:
                        {
                            ref float v = ref Unsafe.As<ValueT, float>(ref t);
                            Sync(ref v);
                        }
                        break;
                    case TypeCode.Double:
                        {
                            ref double v = ref Unsafe.As<ValueT, double>(ref t);
                            Sync(ref v);
                        }
                        break;
                    case TypeCode.Decimal:
                        {
                            ref decimal v = ref Unsafe.As<ValueT, decimal>(ref t);
                            Sync(ref v);
                        }
                        break;
                    case TypeCode.Object:
                        if (isStreamable)
                        {
                            t ??= (ValueT)Activator.CreateInstance(typeof(ValueT));
                            ((INiStreamable)t).Sync(this);
                        }
                        else if (isHalf)
                        {
                            ref Half half = ref Unsafe.As<ValueT, Half>(ref t);
                            Sync(ref half);
                        }
                        break;
                }
            }
        }

        public class TypeSyncInfo
        {
            public TypeCode TypeCode;
            public bool IsStreamable;
            public bool IsHalf;
            public bool IsVector2;
            public bool IsVector3;
            public bool IsVector4;
            public bool IsQuaternion;
        }

        private Dictionary<Type, TypeSyncInfo> cachedTypeSyncInfo = [];

        public TypeSyncInfo GetTypeSyncInfo(Type type)
        {
            if (cachedTypeSyncInfo.TryGetValue(type, out TypeSyncInfo typeSyncInfo))
                return typeSyncInfo;

            typeSyncInfo = new()
            {
                TypeCode = Type.GetTypeCode(type),
                IsStreamable = type.IsAssignableTo(typeof(INiStreamable)),
                IsHalf = type.Equals(typeof(Half)),
                IsVector2 = type.Equals(typeof(Vector2)),
                IsVector3 = type.Equals(typeof(Vector3)),
                IsVector4 = type.Equals(typeof(Vector4)),
                IsQuaternion = type.Equals(typeof(Quaternion))
            };

            cachedTypeSyncInfo.Add(type, typeSyncInfo);
            return typeSyncInfo;
        }

        public void SyncListContent<ValueT>(List<ValueT> list)
        {
            var typeSyncInfo = GetTypeSyncInfo(typeof(ValueT));

            var span = CollectionsMarshal.AsSpan(list);

            for (int i = 0; i < span.Length; i++)
            {
                ref ValueT v = ref span[i];
                SyncGeneric(ref v, typeSyncInfo);
            }
        }

        public void SyncListContent(List<Vector2> list)
        {
            var span = CollectionsMarshal.AsSpan(list);

            for (int i = 0; i < span.Length; i++)
                Sync(ref span[i]);
        }

        public void SyncListContent(List<Vector3> list)
        {
            var span = CollectionsMarshal.AsSpan(list);

            for (int i = 0; i < span.Length; i++)
                Sync(ref span[i]);
        }

        public void SyncListContent(List<Vector4> list)
        {
            var span = CollectionsMarshal.AsSpan(list);

            for (int i = 0; i < span.Length; i++)
                Sync(ref span[i]);
        }

        public void SyncListContent(List<Quaternion> list)
        {
            var span = CollectionsMarshal.AsSpan(list);

            for (int i = 0; i < span.Length; i++)
                Sync(ref span[i]);
        }

        public void SyncListContentEnum<T>(List<T> list) where T : Enum
        {
            var span = CollectionsMarshal.AsSpan(list);

            for (int i = 0; i < span.Length; i++)
            {
                ref T value = ref span[i];
                SyncEnum(ref value);
            }
        }

        public void SyncListContentStreamable<T>(List<T> list) where T : INiStreamable, new()
        {
            var span = CollectionsMarshal.AsSpan(list);

            for (int i = 0; i < span.Length; i++)
            {
                if (CurrentMode == Mode.Read)
                    span[i] ??= new T();

                ref T value = ref span[i];
                value.Sync(this);
            }
        }

        public void InitArraySize<T>(ref T[] array, int size)
        {
            if (CurrentMode == Mode.Write)
            {
                if (array == null)
                {
                    ResizeArrayDefaults(ref array, size);
                }
                else
                {
                    int cur = array.Length;
                    if (size != cur)
                    {
                        Array.Resize(ref array, size);
                        array = new T[size];

                        if (cur < size && cur > 0)
                        {
                            for (int i = cur; i < size; i++)
                                array[i] = default;
                        }
                    }
                }
            }
            else
            {
                ResizeArrayDefaults(ref array, size);
            }
        }

        public void SyncArrayContent<ValueT>(ValueT[] array)
        {
            var typeSyncInfo = GetTypeSyncInfo(typeof(ValueT));

            for (int i = 0; i < array.Length; i++)
            {
                ref ValueT v = ref array[i];
                if (CurrentMode == Mode.Read)
                    v = default;

                SyncGeneric(ref v, typeSyncInfo);
            }
        }

        public void SyncArrayContent(Vector2[] array)
        {
            for (int i = 0; i < array.Length; i++)
                Sync(ref array[i]);
        }

        public void SyncArrayContent(Vector3[] array)
        {
            for (int i = 0; i < array.Length; i++)
                Sync(ref array[i]);
        }

        public void SyncArrayContent(Vector4[] array)
        {
            for (int i = 0; i < array.Length; i++)
                Sync(ref array[i]);
        }

        public void SyncArrayContent(Quaternion[] array)
        {
            for (int i = 0; i < array.Length; i++)
                Sync(ref array[i]);
        }

        public void SyncArrayContentEnum<T>(T[] array) where T : Enum
        {
            for (int i = 0; i < array.Length; i++)
            {
                ref T value = ref array[i];
                SyncEnum(ref value);
            }
        }

        public void SyncArrayContentStreamable<T>(T[] array) where T : INiStreamable, new()
        {
            for (int i = 0; i < array.Length; i++)
            {
                ref T value = ref array[i];

                if (CurrentMode == Mode.Read)
                    value ??= new T();

                value.Sync(this);
            }
        }

        public void SyncEnum<T>(ref T e) where T : Enum
        {
            switch (e.GetTypeCode())
            {
                case TypeCode.SByte:
                    if (CurrentMode == Mode.Read)
                    {
                        var v = In.Reader.ReadSByte();
                        e = (T)Enum.ToObject(e.GetType(), v);
                    }
                    else
                    {
                        var v = (sbyte)Convert.ChangeType(e, e.GetTypeCode());
                        Out.Writer.Write(v);
                    }
                    break;

                case TypeCode.Int16:
                    if (CurrentMode == Mode.Read)
                    {
                        var v = In.Reader.ReadInt16();
                        e = (T)Enum.ToObject(e.GetType(), v);
                    }
                    else
                    {
                        var v = (short)Convert.ChangeType(e, e.GetTypeCode());
                        Out.Writer.Write(v);
                    }
                    break;

                case TypeCode.Int32:
                    if (CurrentMode == Mode.Read)
                    {
                        var v = In.Reader.ReadInt32();
                        e = (T)Enum.ToObject(e.GetType(), v);
                    }
                    else
                    {
                        var v = (int)Convert.ChangeType(e, e.GetTypeCode());
                        Out.Writer.Write(v);
                    }
                    break;

                case TypeCode.Int64:
                    if (CurrentMode == Mode.Read)
                    {
                        var v = In.Reader.ReadInt64();
                        e = (T)Enum.ToObject(e.GetType(), v);
                    }
                    else
                    {
                        var v = (long)Convert.ChangeType(e, e.GetTypeCode());
                        Out.Writer.Write(v);
                    }
                    break;

                case TypeCode.Byte:
                    if (CurrentMode == Mode.Read)
                    {
                        var v = In.Reader.ReadByte();
                        e = (T)Enum.ToObject(e.GetType(), v);
                    }
                    else
                    {
                        var v = (byte)Convert.ChangeType(e, e.GetTypeCode());
                        Out.Writer.Write(v);
                    }
                    break;

                case TypeCode.UInt16:
                    if (CurrentMode == Mode.Read)
                    {
                        var v = In.Reader.ReadUInt16();
                        e = (T)Enum.ToObject(e.GetType(), v);
                    }
                    else
                    {
                        var v = (ushort)Convert.ChangeType(e, e.GetTypeCode());
                        Out.Writer.Write(v);
                    }
                    break;

                case TypeCode.UInt32:
                    if (CurrentMode == Mode.Read)
                    {
                        var v = In.Reader.ReadUInt32();
                        e = (T)Enum.ToObject(e.GetType(), v);
                    }
                    else
                    {
                        var v = (uint)Convert.ChangeType(e, e.GetTypeCode());
                        Out.Writer.Write(v);
                    }
                    break;

                case TypeCode.UInt64:
                    if (CurrentMode == Mode.Read)
                    {
                        var v = In.Reader.ReadUInt64();
                        e = (T)Enum.ToObject(e.GetType(), v);
                    }
                    else
                    {
                        var v = (ulong)Convert.ChangeType(e, e.GetTypeCode());
                        Out.Writer.Write(v);
                    }
                    break;
            }
        }

        public void SyncGeneric<ValueT>(ref ValueT t)
        {
            var typeSyncInfo = GetTypeSyncInfo(typeof(ValueT));
            SyncGeneric(ref t, typeSyncInfo);
        }

        public void SyncGeneric<ValueT>(ref ValueT t, TypeSyncInfo typeSyncInfo)
        {
            if (CurrentMode == Mode.Read)
                t = default;

            switch (typeSyncInfo.TypeCode)
            {
                case TypeCode.SByte:
                    {
                        ref sbyte v = ref Unsafe.As<ValueT, sbyte>(ref t);
                        Sync(ref v);
                    }
                    break;
                case TypeCode.Byte:
                    {
                        ref byte v = ref Unsafe.As<ValueT, byte>(ref t);
                        Sync(ref v);
                    }
                    break;
                case TypeCode.Int16:
                    {
                        ref short v = ref Unsafe.As<ValueT, short>(ref t);
                        Sync(ref v);
                    }
                    break;
                case TypeCode.UInt16:
                    {
                        ref ushort v = ref Unsafe.As<ValueT, ushort>(ref t);
                        Sync(ref v);
                    }
                    break;
                case TypeCode.Int32:
                    {
                        ref int v = ref Unsafe.As<ValueT, int>(ref t);
                        Sync(ref v);
                    }
                    break;
                case TypeCode.UInt32:
                    {
                        ref uint v = ref Unsafe.As<ValueT, uint>(ref t);
                        Sync(ref v);
                    }
                    break;
                case TypeCode.Int64:
                    {
                        ref long v = ref Unsafe.As<ValueT, long>(ref t);
                        Sync(ref v);
                    }
                    break;
                case TypeCode.UInt64:
                    {
                        ref ulong v = ref Unsafe.As<ValueT, ulong>(ref t);
                        Sync(ref v);
                    }
                    break;
                case TypeCode.Boolean:
                    {
                        ref bool v = ref Unsafe.As<ValueT, bool>(ref t);
                        Sync(ref v);
                    }
                    break;
                case TypeCode.Single:
                    {
                        ref float v = ref Unsafe.As<ValueT, float>(ref t);
                        Sync(ref v);
                    }
                    break;
                case TypeCode.Double:
                    {
                        ref double v = ref Unsafe.As<ValueT, double>(ref t);
                        Sync(ref v);
                    }
                    break;
                case TypeCode.Decimal:
                    {
                        ref decimal v = ref Unsafe.As<ValueT, decimal>(ref t);
                        Sync(ref v);
                    }
                    break;
                case TypeCode.Object:
                    if (typeSyncInfo.IsStreamable)
                    {
                        t ??= (ValueT)Activator.CreateInstance(typeof(ValueT));
                        ((INiStreamable)t).Sync(this);
                    }
                    else if (typeSyncInfo.IsHalf)
                    {
                        ref Half half = ref Unsafe.As<ValueT, Half>(ref t);
                        Sync(ref half);
                    }
                    else if (typeSyncInfo.IsVector2)
                    {
                        ref Vector2 vec2 = ref Unsafe.As<ValueT, Vector2>(ref t);
                        Sync(ref vec2);
                    }
                    else if (typeSyncInfo.IsVector3)
                    {
                        ref Vector3 vec3 = ref Unsafe.As<ValueT, Vector3>(ref t);
                        Sync(ref vec3);
                    }
                    else if (typeSyncInfo.IsVector4)
                    {
                        ref Vector4 vec4 = ref Unsafe.As<ValueT, Vector4>(ref t);
                        Sync(ref vec4);
                    }
                    else if (typeSyncInfo.IsQuaternion)
                    {
                        ref Quaternion quat = ref Unsafe.As<ValueT, Quaternion>(ref t);
                        Sync(ref quat);
                    }
                    break;
            }
        }
    }
}
