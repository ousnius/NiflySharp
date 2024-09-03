// Source copied and edited from System.Collections.Specialized.BitVector32

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace NiflySharp.Structs
{
    /// <summary>
    /// Provides a simple light bit vector with easy integer or Boolean access to a 64 bit storage.
    /// </summary>
    public struct BitVector64 : IEquatable<BitVector64>
    {
        private ulong _data;

        /// <devdoc>
        /// <para>Initializes a new instance of the BitVector64 structure with the specified internal data.</para>
        /// </devdoc>
        public BitVector64(ulong data)
        {
            _data = data;
        }

        /// <summary>
        /// Initializes a new instance of the BitVector64 structure with the information in the specified value.
        /// </summary>
        public BitVector64(BitVector64 value)
        {
            _data = value._data;
        }

        /// <summary>
        /// Gets or sets a value indicating whether all the specified bits are set.
        /// </summary>
        public bool this[ulong bit]
        {
            readonly get
            {
                return (_data & bit) == bit;
            }
            set
            {
                unchecked
                {
                    if (value)
                    {
                        _data |= bit;
                    }
                    else
                    {
                        _data &= ~bit;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the value for the specified section.
        /// </summary>
        public long this[Section section]
        {
            readonly get
            {
                unchecked
                {
                    return (long)((_data & (ulong)((long)section.Mask << section.Offset)) >> section.Offset);
                }
            }
            set
            {
                // The code should really have originally validated "(value & section.Mask) == value" with
                // an exception (it instead validated it with a Debug.Assert, which does little good in a
                // public method when in a Release build).  We don't include such a check now as it would
                // likely break things and for little benefit.

                value <<= section.Offset;
                long offsetMask = (0xFFFFFFFF & (long)section.Mask) << section.Offset;
                _data = unchecked((_data & ~(ulong)offsetMask) | ((ulong)value & (ulong)offsetMask));
            }
        }

        /// <summary>
        /// returns the raw data stored in this bit vector...
        /// </summary>
        public readonly long Data
        {
            get
            {
                return unchecked((long)_data);
            }
        }

        /// <summary>
        /// Creates the first mask in a series.
        /// </summary>
        public static long CreateMask()
        {
            return CreateMask(0);
        }

        /// <summary>
        /// Creates the next mask in a series.
        /// </summary>
        public static long CreateMask(long previous)
        {
            if (previous == 0)
            {
                return 1;
            }

            if (previous == unchecked((long)0x8000000000000000))
            {

                throw new InvalidOperationException("Bit vector is full.");
            }

            return previous << 1;
        }

        /// <summary>
        /// Creates the first section in a series, with the specified maximum value.
        /// </summary>
        public static Section CreateSection(int maxValue)
        {
            return CreateSectionHelper(maxValue, 0, 0);
        }

        /// <summary>
        /// Creates the next section in a series, with the specified maximum value.
        /// </summary>
        public static Section CreateSection(int maxValue, Section previous)
        {
            return CreateSectionHelper(maxValue, previous.Mask, previous.Offset);
        }

        private static Section CreateSectionHelper(int maxValue, int priorMask, int priorOffset)
        {
            if (maxValue < 1)
            {
                throw new ArgumentException($"{nameof(maxValue)} too small ({nameof(maxValue)} < 1).");
            }

            int offset = priorOffset + BitOperations.PopCount((ulong)(uint)priorMask);
            if (offset >= 64)
            {
                throw new InvalidOperationException("Bit vector is full.");
            }

            int mask = (int)(BitOperations.RoundUpToPowerOf2((ulong)(uint)maxValue + 1) - 1);
            return new Section(mask, offset);
        }

        public override bool Equals([NotNullWhen(true)] object o) => o is BitVector64 other && Equals(other);

        /// <summary>Indicates whether the current instance is equal to another instance of the same type.</summary>
        /// <param name="other">An instance to compare with this instance.</param>
        /// <returns>true if the current instance is equal to the other instance; otherwise, false.</returns>
        public readonly bool Equals(BitVector64 other) => _data == other._data;

        public override readonly int GetHashCode() => _data.GetHashCode();

        public static string ToString(BitVector64 value)
        {
            return string.Create(/*"BitVector64{".Length*/12 + /*64 bits*/64 + /*"}".Length"*/1, value, (dst, v) =>
            {
                ReadOnlySpan<char> prefix = "BitVector64{";
                prefix.CopyTo(dst);
                dst[^1] = '}';

                ulong locdata = v._data;
                dst = dst.Slice(prefix.Length, 64);
                for (int i = 0; i < dst.Length; i++)
                {
                    dst[i] = (locdata & 0x8000000000000000) != 0 ? '1' : '0';
                    locdata <<= 1;
                }
            });
        }

        public override readonly string ToString()
        {
            return ToString(this);
        }

        /// <summary>
        /// Represents an section of the vector that can contain a integer number.
        /// </summary>
        public readonly struct Section : IEquatable<Section>
        {
            private readonly int _mask;
            private readonly int _offset;

            internal Section(int mask, int offset)
            {
                _mask = mask;
                _offset = offset;
            }

            public int Mask => _mask;

            public int Offset => _offset;

            public override bool Equals([NotNullWhen(true)] object? o) => o is Section other && Equals(other);

            public bool Equals(Section obj)
            {
                return obj._mask == _mask && obj._offset == _offset;
            }

            public static bool operator ==(Section a, Section b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(Section a, Section b)
            {
                return !(a == b);
            }

            public override int GetHashCode() => HashCode.Combine(_mask, _offset);

            public static string ToString(Section value)
            {
                return $"Section{{0x{value.Mask:x}, 0x{value.Offset:x}}}";
            }

            public override string ToString()
            {
                return ToString(this);
            }
        }

        public static bool operator ==(BitVector64 left, BitVector64 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BitVector64 left, BitVector64 right)
        {
            return !(left == right);
        }
    }
}
