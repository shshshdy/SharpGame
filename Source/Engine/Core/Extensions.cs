﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpGame
{
    public static class Extensions
    {
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

        public static string ReadString(this Stream source)
        {
            return "";
        }

        public static void WriteString(this Stream stream, string str)
        {

        }
    }
}
