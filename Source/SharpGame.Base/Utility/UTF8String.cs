using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    public unsafe class UTF8String : IDisposable
    {
        private int length;
        public int Length => length;
        public byte* StrPtr;

        public UTF8String()
        {
        }

        public UTF8String(string s)
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
        
        private string GetString()
        {
            return Encoding.UTF8.GetString(StrPtr, length);
        }

        public void Dispose()
        {
            Utilities.Free((IntPtr)StrPtr);
        }

        public static string FromPointer(byte* pointer)
        {
            if (pointer == null) return null;

            // Read until null-terminator.
            byte* walkPtr = pointer;
            while (*walkPtr != 0) walkPtr++;

            // Decode UTF-8 bytes to string.
            return Encoding.UTF8.GetString(pointer, (int)(walkPtr - pointer));
        }

        public static string FromPointer(IntPtr pointer) => FromPointer((byte*)pointer);

        public static void ToPointer(string value, byte* dstPointer, int maxByteCount)
        {
            if (value == null) return;

            int destBytesWritten;
            fixed (char* srcPointer = value)
                destBytesWritten = Encoding.UTF8.GetBytes(srcPointer, value.Length, dstPointer, maxByteCount);
            dstPointer[destBytesWritten] = 0; // Null-terminator.
        }

        public static IntPtr AllocToPointer(string value)
        {
            if (value == null) return IntPtr.Zero;

            // Get max number of bytes the string may need.
            int maxSize = GetMaxByteCount(value);
            // Allocate unmanaged memory.
            IntPtr managedPtr = Utilities.Alloc(maxSize);
            var ptr = (byte*)managedPtr;
            // Encode to utf-8, null-terminate and write to unmanaged memory.
            int actualNumberOfBytesWritten;
            fixed (char* ch = value)
                actualNumberOfBytesWritten = Encoding.UTF8.GetBytes(ch, value.Length, ptr, maxSize);
            ptr[actualNumberOfBytesWritten] = 0;
            // Return pointer to the beginning of unmanaged memory.
            return managedPtr;
        }

        public static int GetMaxByteCount(string value) =>
            value == null ? 0 : Encoding.UTF8.GetMaxByteCount(value.Length + 1);


        public static implicit operator byte* (UTF8String utf8String) => utf8String.StrPtr;
        public static implicit operator IntPtr (UTF8String utf8String) => (IntPtr)(utf8String.StrPtr);
        public static implicit operator UTF8String(string s) => new UTF8String(s);
        public static implicit operator string(UTF8String utf8String) => utf8String.GetString();
    }
}
