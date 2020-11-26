using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SharpGame
{
    public class CStringList : DisposeBase, IEnumerable
    {
        private Vector<IntPtr> nativeStrs = new Vector<IntPtr>();

        public IntPtr Data => nativeStrs.Data;
        public uint Count => nativeStrs.Count;

        public CStringList()
        {
        }

        public CStringList(CStringList other)
        {
            for(int i = 0; i < other.nativeStrs.Count; i++)
            {
                Add(other.nativeStrs[i]);
            }
        }

        public void Add(string str)
        {
            var ptr = Marshal.StringToHGlobalAnsi(str);
            nativeStrs.Add(ptr);
        }

        unsafe void Add(IntPtr str)
        {
            int len = 0;
            byte* p = (byte*)str;
            while (*p++ != 0)
                len++;
            var ptr = Marshal.AllocHGlobal(len + 1);
            Utilities.CopyMemory(ptr, str, len);
            ((byte*)ptr)[len] = 0;
            nativeStrs.Add(ptr);
        }

        protected override void Destroy(bool disposing)
        {
            foreach(var ptr in nativeStrs)
            {
                Marshal.FreeHGlobal(ptr);
            }

            nativeStrs.Clear();
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)nativeStrs).GetEnumerator();
        }
    }

}
