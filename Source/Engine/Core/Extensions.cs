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

        public static bool IsEof(this Stream stream)
        {
            return stream.Position >= stream.Length;
        }

        public static byte[] ReadAllBytes(this Stream source)
        {
            long originalPosition = source.Position;
            source.Position = 0;

            const int defaultBufferSize = 4096;
            try
            {
                byte[] readBuffer = new byte[defaultBufferSize];
                int totalBytesRead = 0;
                int bytesRead;
                while ((bytesRead = source.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;
                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = source.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                source.Position = originalPosition;
            }
        }

        public static String ReadAllText(this Stream stream)
        {
            return Encoding.Default.GetString(stream.ReadArray<byte>((int)stream.Length));
        }

        public static unsafe T Read<T>(this Stream stream) where T : struct
        {
            byte[] nativeStreamBuffer = new byte[1024];
            int sizeOfT = Unsafe.SizeOf<T>();
            int currentReadSize = stream.Read(nativeStreamBuffer, 0, sizeOfT);
            if (currentReadSize != sizeOfT)
                throw new InvalidOperationException("Reached end of stream.");

            fixed (byte* temporaryBufferStart = nativeStreamBuffer)
            {
                return Unsafe.AsRef<T>(temporaryBufferStart);
            }
        }

        public unsafe static string ReadCString(this Stream stream)
        {
            byte[] nativeStreamBuffer = new byte[1024];
            int count = 0;
            while (!stream.IsEof())
            {
                int c = stream.ReadByte();// nativeStreamBuffer, count, 1);
                if (c == 0)
                    break;
                nativeStreamBuffer[count++] = (byte)c;
            }

            return Encoding.Default.GetString(nativeStreamBuffer, 0, count);
        }

        public static unsafe String ReadFileID(this Stream stream)
        {
            byte* bytes = stackalloc byte[5];
            if (4 != stream.Read((IntPtr)bytes, 4))
            {
                throw new InvalidOperationException("Reached end of stream.");
            }

            bytes[4] = 0;
            return Marshal.PtrToStringAnsi((IntPtr)bytes);
        }

        public static unsafe T[] ReadArray<T>(this Stream stream, int count) where T : struct
        {
            var result = new T[count];
            var asBytes = Unsafe.As<byte[]>(result);

            fixed (void* dest = asBytes)
            {
                int byteCount = count * Unsafe.SizeOf<T>();
                int currentReadSize = stream.Read((IntPtr)dest, byteCount);
                if (currentReadSize != byteCount)
                    throw new InvalidOperationException("Reached end of stream.");
            }

            return result;
        }

        public unsafe static int Read(this Stream stream, IntPtr buffer, int count)
        {
            int NativeStreamBufferSize = 1024;
            byte[] nativeStreamBuffer = new byte[NativeStreamBufferSize];
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
    }
}
