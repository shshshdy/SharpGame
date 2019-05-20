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
        public byte* StrPtr => (byte*)handle.AddrOfPinnedObject().ToPointer();

        public CString(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            byte[] text = Encoding.UTF8.GetBytes(s);
            handle = GCHandle.Alloc(text, GCHandleType.Pinned);
            length = text.Length;
        }

        public void SetText(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            handle.Free();
            byte[] text = Encoding.UTF8.GetBytes(s);
            handle = GCHandle.Alloc(text, GCHandleType.Pinned);
            length = text.Length;
        }

        private string GetString()
        {
            return Encoding.UTF8.GetString(StrPtr, (int)length);
        }

        public void Dispose()
        {
            handle.Free();
        }

        public static implicit operator byte* (CString utf8String) => utf8String.StrPtr;
        public static implicit operator IntPtr (CString utf8String) => new IntPtr(utf8String.StrPtr);
        public static implicit operator CString(string s) => new CString(s);
        public static implicit operator string(CString utf8String) => utf8String.GetString();
    }
}
