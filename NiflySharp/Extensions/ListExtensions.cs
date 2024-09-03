using System;
using System.Collections.Generic;
using System.Linq;

namespace NiflySharp.Extensions
{
    public static class ListExtensions
    {
        public static List<T> Resize<T>(this List<T> list, int size, T element = default)
        {
            list ??= [];

            int count = list.Count;

            if (size < count)
            {
                list.RemoveRange(size, count - size);
            }
            else if (size > count)
            {
                if (size > list.Capacity)   // Optimization
                    list.Capacity = size;

                list.AddRange(Enumerable.Repeat(element, size - count));
            }

            return list;
        }

        public static IEnumerable<List<T>> SplitByFixedSize<T>(this List<T> list, int nSize)
        {
            for (int i = 0; i < list.Count; i += nSize)
            {
                yield return list.GetRange(i, Math.Min(nSize, list.Count - i));
            }
        }

        public static IEnumerable<List<T>> SplitByFlexSize<T, SizeT>(this List<T> list, List<SizeT> sizeList)
        {
            for (int i = 0; i < list.Count;)
            {
                int size = Convert.ToInt32(sizeList[i]);
                var range = list.GetRange(i, Math.Min(size, list.Count - i));
                i += size;
                yield return range;
            }
        }
    }
}
