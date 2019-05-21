using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    public static class Extensions
    {
        public static bool Empty(this String str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static bool IsNullOrEmpty<T>(this IList<T> list)
        {
            return list == null || list.Count == 0;
        }

        public static bool Empty<T>(this IList<T> list)
        {
            return list.Count == 0;
        }

        public static void Push<T>(this List<T> list, T item)
        {
            list.Add(item);
        }
        
        public static T Pop<T>(this List<T> list)
        {
            T ret = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return ret;
        }

        public static void FastRemove<T>(this List<T> list, int item)
        {
            if(item < list.Count && list.Count > 0)
            {
                list[item] = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
            }

        }

        public static void Clear<T>(this T[] arr)
        {
            if (arr != null)
            {
                Array.Clear(arr, 0, arr.Length);
            }
        }
        
    }
}
