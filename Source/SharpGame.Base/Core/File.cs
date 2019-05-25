using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    public class File : Object
    {
        protected Stream stream;
        public Stream Stream => stream;

        public StringID Name { get; set; }

        public bool IsEof
        {
            get { return stream.Position >= stream.Length; }
        }

        public int Length => (int)stream.Length;

        // Helper buffer for classes needing it.
        // If null, it should be initialized with NativeStreamBufferSize constant.
        protected byte[] nativeStreamBuffer;
        protected int NativeStreamBufferSize => nativeStreamBuffer.Length;
        
        public File(Stream stream, int bufferSize = 4096)
        {
            this.stream = stream;
            nativeStreamBuffer = new byte[bufferSize];
        }

        protected override void Destroy()
        {
            base.Destroy();

            stream?.Dispose();
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

        public String ReadAllText()
        {
            return Encoding.Default.GetString(ReadArray<byte>((int)stream.Length));
        }

        public unsafe T Read<T>() where T : struct
        {
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

        public unsafe String ReadFileID()
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
        
        /// <summary>
        /// Reads a block of bytes from the stream and writes the data in a given buffer.
        /// </summary>
        /// <param name="buffer">When this method returns, contains the specified buffer with the values between 0 and (count - 1) replaced by the bytes read from the current source. </param>
        /// <param name="count">The maximum number of bytes to read. </param>
        /// <exception cref="ArgumentNullException">array is null. </exception>
        /// <returns>The total number of bytes read into the buffer. This might be less than the number of bytes requested if that number of bytes are not currently available, or zero if the end of the stream is reached.</returns>
        public int Read(IntPtr buffer, int count)
        {
            int readSize = 0;

            for (int offset = 0; offset < count; offset += NativeStreamBufferSize, buffer += NativeStreamBufferSize)
            {
                // Compute missing bytes in this block
                int blockSize = count - offset;
                if (blockSize > NativeStreamBufferSize)
                    blockSize = NativeStreamBufferSize;

                int currentReadSize = stream.Read(nativeStreamBuffer, 0, blockSize);
                readSize += currentReadSize;
                Utilities.Write(buffer, nativeStreamBuffer, 0, currentReadSize);

                // Reached end of stream?
                if (currentReadSize < blockSize)
                    break;
            }

            return readSize;
        }

        public unsafe void Write<T>(ref T v) where T : struct
        {
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

        /// <summary>
        /// Writes a block of bytes to this stream using data from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer containing data to write to the stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream. </param>
        public unsafe void Write(IntPtr buffer, int count)
        {
            for (int offset = 0; offset < count; offset += NativeStreamBufferSize, buffer += NativeStreamBufferSize)
            {
                // Compute missing bytes in this block
                int blockSize = count - offset;
                if (blockSize > NativeStreamBufferSize)
                    blockSize = NativeStreamBufferSize;

                fixed (byte* temporaryBufferStart = nativeStreamBuffer)
                    Utilities.CopyMemory((IntPtr)temporaryBufferStart, buffer, blockSize);

                stream.Write(nativeStreamBuffer, 0, blockSize);
            }
        }
    }
}
