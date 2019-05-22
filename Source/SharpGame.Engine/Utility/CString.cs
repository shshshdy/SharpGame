using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    public unsafe class CString : IDisposable
    {
        private GCHandle handle;
        private int length;
        public int Length => length;
        public byte* StrPtr;// => (byte*)handle.AddrOfPinnedObject().ToPointer();

        public CString(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            int len = GetMaxByteCount(s);
            StrPtr = (byte*)Utilities.Alloc(len);
            fixed(char* txt = s)
            {
                length = Encoding.UTF8.GetBytes(txt, s.Length, StrPtr, len);
                StrPtr[length] = 0;
            }
        }

        /*
        public static IntPtr AllocToPointer(string value)
        {
            if (value == null) return IntPtr.Zero;

            // Get max number of bytes the string may need.
            int maxSize = GetMaxByteCount(value);
            // Allocate unmanaged memory.
            IntPtr managedPtr = Alloc(maxSize);
            var ptr = (byte*)managedPtr;
            // Encode to utf-8, null-terminate and write to unmanaged memory.
            int actualNumberOfBytesWritten;
            fixed (char* ch = value)
                actualNumberOfBytesWritten = Encoding.UTF8.GetBytes(ch, value.Length, ptr, maxSize);
            ptr[actualNumberOfBytesWritten] = 0;
            // Return pointer to the beginning of unmanaged memory.
            return managedPtr;
        }*/

        private string GetString()
        {
            return Encoding.UTF8.GetString(StrPtr, length);
        }

        public static int GetMaxByteCount(string value) =>
            value == null ? 0 : Encoding.UTF8.GetMaxByteCount(value.Length + 1);

        public void Dispose()
        {
            Utilities.Free((IntPtr)StrPtr);
        }

        public static implicit operator byte* (CString utf8String) => utf8String.StrPtr;
        public static implicit operator IntPtr (CString utf8String) => (IntPtr)(utf8String.StrPtr);
        public static implicit operator CString(string s) => new CString(s);
        public static implicit operator string(CString utf8String) => utf8String.GetString();
    }
}
