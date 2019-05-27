using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    public class File : NativeStreamWrapper
    {
        public StringID Name { get; set; }

        public bool IsEof
        {
            get { return stream.Position >= stream.Length; }
        }

        public File(Stream stream, int bufferSize = 4096) : base(stream, bufferSize)
        {
        }

        public int Seek(int offset)
        {
            return (int)stream.Seek(offset, SeekOrigin.Begin);
        }

        public void Skip(int bytes)
        {
            stream.Seek(bytes, SeekOrigin.Current);
        }

        public byte[] ReadAllBytes()
        {
            return ReadArray<byte>((int)stream.Length);
        }

        public string ReadAllText()
        {
            return Encoding.Default.GetString(ReadArray<byte>((int)stream.Length));
        }

        public unsafe T Read<T>() where T : struct
        {
            var temporaryBuffer = nativeStreamBuffer;
            if (temporaryBuffer == null)
                temporaryBuffer = nativeStreamBuffer = new byte[NativeStreamBufferSize];

            int sizeOfT = Unsafe.SizeOf<T>();
            int currentReadSize = stream.Read(nativeStreamBuffer, 0, sizeOfT);
            if(currentReadSize != sizeOfT)
                throw new InvalidOperationException("Reached end of stream.");

            fixed (byte* temporaryBufferStart = nativeStreamBuffer)
            {
                return Unsafe.AsRef<T>(temporaryBufferStart);
            }
        }

        public string ReadCString()
        {
            var temporaryBuffer = nativeStreamBuffer;
            if (temporaryBuffer == null)
                temporaryBuffer = nativeStreamBuffer = new byte[NativeStreamBufferSize];

            int count = 0;
            while(!IsEof)
            {
                int c = stream.ReadByte();// nativeStreamBuffer, count, 1);
                if(c == 0)
                    break;
                nativeStreamBuffer[count++] = (byte)c;
            }

            return Encoding.Default.GetString(nativeStreamBuffer, 0, count);
        }

        public unsafe string ReadFileID()
        {
            byte* bytes = stackalloc byte[5];
            if(4 != Read((IntPtr)bytes, 4))
            {
                throw new InvalidOperationException("Reached end of stream.");
            }

            bytes[4] = 0;                        
            return Marshal.PtrToStringAnsi((IntPtr)bytes);
        }

        public unsafe T[] ReadArray<T>(int count) where T : struct
        {
            var result = new T[count];

            IntPtr dest = Utilities.AsPointer(ref result[0]);            
            int byteCount = count * Unsafe.SizeOf<T>();            
            int currentReadSize = Read(dest, byteCount);
            if(currentReadSize != byteCount)
                throw new InvalidOperationException("Reached end of stream.");
            
            return result;
        }
        
        public unsafe void Write<T>(ref T v) where T : struct
        {
            var temporaryBuffer = nativeStreamBuffer;
            if (temporaryBuffer == null)
                temporaryBuffer = nativeStreamBuffer = new byte[NativeStreamBufferSize];

            fixed (byte* temporaryBufferStart = nativeStreamBuffer)
                Utilities.Write((IntPtr)temporaryBufferStart, ref v);

            stream.Write(nativeStreamBuffer, 0, Unsafe.SizeOf<T>());
        }

        public unsafe void WriteArray<T>(T[] val) where T : struct
        {
            IntPtr dest = Utilities.AsPointer(ref val[0]);            
            int byteCount = val.Length * Unsafe.SizeOf<T>();
            Write(dest, byteCount);   
        }

    }
}
