using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpGame
{
    public class File : NativeStream
    {
        public string Name { get; set; }

        protected Stream stream;
        public Stream Stream => stream;

        public File(Stream stream, int bufferSize = 4096)
        {
            this.stream = stream;
            NativeStreamBufferSize = bufferSize;
        }

        public bool IsEof
        {
            get { return stream.Position >= stream.Length; }
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
            if (currentReadSize != sizeOfT)
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
            while (!IsEof)
            {
                int c = stream.ReadByte();
                if (c == 0)
                    break;
                nativeStreamBuffer[count++] = (byte)c;
            }

            return Encoding.Default.GetString(nativeStreamBuffer, 0, count);
        }

        public unsafe string ReadFileID()
        {
            byte* bytes = stackalloc byte[5];
            if (4 != Read((IntPtr)bytes, 4))
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
            if (currentReadSize != byteCount)
                throw new InvalidOperationException("Reached end of stream.");

            return result;
        }

        public byte[] ReadBytes(int count)
        {
            return ReadArray<byte>(count);
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

        /// <inheritdoc/>
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return stream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            stream.Flush();
        }

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return stream.FlushAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return stream.Read(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            return stream.ReadByte();
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override void WriteByte(byte value)
        {
            stream.WriteByte(value);
        }

        /// <inheritdoc/>
        public override bool CanRead
        {
            get { return stream.CanRead; }
        }

        /// <inheritdoc/>
        public override bool CanSeek
        {
            get { return stream.CanSeek; }
        }

        /// <inheritdoc/>
        public override bool CanTimeout
        {
            get { return stream.CanTimeout; }
        }

        /// <inheritdoc/>
        public override bool CanWrite
        {
            get { return stream.CanWrite; }
        }

        /// <inheritdoc/>
        public override long Length
        {
            get { return stream.Length; }
        }

        /// <inheritdoc/>
        public override long Position
        {
            get { return stream.Position; }
            set { stream.Position = value; }
        }

        /// <inheritdoc/>
        public override int ReadTimeout
        {
            get { return stream.ReadTimeout; }
            set { stream.ReadTimeout = value; }
        }

        /// <inheritdoc/>
        public override int WriteTimeout
        {
            get { return stream.WriteTimeout; }
            set { stream.WriteTimeout = value; }
        }

        public override void Close()
        {
            try
            {
                stream.Close();
            }
            finally
            {
                base.Dispose(true);
            }
            
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                // Explicitly pick up a potentially methodimpl'ed Dispose
                if (disposing)
                    ((IDisposable)stream).Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
            
        }

    }
}
