// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SharpGame
{
    public class NativeStreamWrapper : NativeStream
    {
        protected Stream stream;
        public Stream Stream => stream;

        public NativeStreamWrapper(Stream stream, int bufferSize = 4096)
        {
            this.stream = stream;
            NativeStreamBufferSize = bufferSize;
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
