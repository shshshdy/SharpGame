using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    // A ridiculously bad KTX file parser.
    // https://www.khronos.org/opengles/sdk/tools/KTX/file_format_spec
    public class KtxFile
    {
        public KtxHeader Header { get; }
        public KtxKeyValuePair[] KeyValuePairs { get; }
        public ImageData[] Faces { get; }

        public KtxFile(KtxHeader header, KtxKeyValuePair[] keyValuePairs, ImageData[] faces)
        {
            Header = header;
            KeyValuePairs = keyValuePairs;
            Faces = faces;
        }

        public static KtxFile Load(File file, bool readKeyValuePairs)
        {
           
            KtxHeader header = ReadStruct<KtxHeader>(file);

            KtxKeyValuePair[] kvps = null;
            if (readKeyValuePairs)
            {
                int keyValuePairBytesRead = 0;
                List<KtxKeyValuePair> keyValuePairs = new List<KtxKeyValuePair>();
                while (keyValuePairBytesRead < header.BytesOfKeyValueData)
                {
                    int bytesRemaining = (int)(header.BytesOfKeyValueData - keyValuePairBytesRead);
                    KtxKeyValuePair kvp = ReadNextKeyValuePair(file, out int read);
                    keyValuePairBytesRead += read;
                    keyValuePairs.Add(kvp);
                }

                kvps = keyValuePairs.ToArray();
            }
            else
            {
                file.Skip((int)header.BytesOfKeyValueData); // Skip over header data.
            }

            uint numberOfFaces = Math.Max(1, header.NumberOfFaces);
            List<ImageData> faces = new List<ImageData>((int)numberOfFaces);
            for (int i = 0; i < numberOfFaces; i++)
            {
                faces.Add(new ImageData(header.NumberOfMipmapLevels) { Width = header.PixelWidth, Height = header.PixelHeight});
            }
            for (uint mipLevel = 0; mipLevel < header.NumberOfMipmapLevels; mipLevel++)
            {
                uint imageSize = file.Read<uint>();
                if(mipLevel == 11/*header.NumberOfMipmapLevels - 1*/)
                {
                    //bug?
                    imageSize = 1;
                }
                // For cubemap textures, imageSize is actually the size of an individual face.
                bool isCubemap = header.NumberOfFaces == 6 && header.NumberOfArrayElements == 0;
                for (uint face = 0; face < numberOfFaces; face++)
                {
                    byte[] faceData = file.ReadArray<byte>((int)imageSize);
                    faces[(int)face].Mipmaps[mipLevel] = new MipmapData(imageSize, faceData, header.PixelWidth / (uint)(Math.Pow(2, mipLevel)), header.PixelHeight / (uint)(Math.Pow(2, mipLevel)));
                    uint cubePadding = 0u;
                    if (isCubemap)
                    {
                        cubePadding = 3 - ((imageSize + 3) % 4);
                    }
                    file.Skip((int)cubePadding);
                }

                uint mipPaddingBytes = 3 - ((imageSize + 3) % 4);
                file.Skip((int)mipPaddingBytes);
            }

            return new KtxFile(header, kvps, faces.ToArray());
            
        }

        private static unsafe KtxKeyValuePair ReadNextKeyValuePair(File file, out int bytesRead)
        {
            uint keyAndValueByteSize = file.Read<uint>();
            byte* keyAndValueBytes = stackalloc byte[(int)keyAndValueByteSize];
            ReadBytes(file, keyAndValueBytes, (int)keyAndValueByteSize);
            int paddingByteCount = (int)(3 - ((keyAndValueByteSize + 3) % 4));

            file.Skip(paddingByteCount);

            // Find the key's null terminator
            int i;
            for (i = 0; i < keyAndValueByteSize; i++)
            {
                if (keyAndValueBytes[i] == 0)
                {
                    break;
                }
                Debug.Assert(i != keyAndValueByteSize); // Fail
            }


            int keySize = i; // Don't include null terminator.
            string key = Encoding.UTF8.GetString(keyAndValueBytes, keySize);
            byte* valueStart = keyAndValueBytes + i + 1; // Skip null terminator
            int valueSize = (int)(keyAndValueByteSize - keySize - 1);
            byte[] value = new byte[valueSize];
            for (int v = 0; v < valueSize; v++)
            {
                value[v] = valueStart[v];
            }

            bytesRead = (int)(keyAndValueByteSize + paddingByteCount + sizeof(uint));
            return new KtxKeyValuePair(key, value);
        }

        private static unsafe T ReadStruct<T>(File file)
        {
            int size = Unsafe.SizeOf<T>();
            byte* bytes = stackalloc byte[size];
            for (int i = 0; i < size; i++)
            {
                bytes[i] = file.Read<byte>();
            }

            return Unsafe.Read<T>(bytes);
        }

        private static unsafe void ReadBytes(File file, byte* destination, int count)
        {
            for (int i = 0; i < count; i++)
            {
                destination[i] = file.Read<byte>();
            }
        }

        public ulong GetTotalSize()
        {
            ulong totalSize = 0;

            for (int mipLevel = 0; mipLevel < Header.NumberOfMipmapLevels; mipLevel++)
            {
                for (int face = 0; face < Header.NumberOfFaces; face++)
                {
                    MipmapData mipmap = Faces[face].Mipmaps[mipLevel];
                    totalSize += mipmap.SizeInBytes;
                }
            }

            return totalSize;
        }

        public byte[] GetAllTextureData()
        {
            byte[] result = new byte[GetTotalSize()];
            uint start = 0;
            for (int face = 0; face < Header.NumberOfFaces; face++)
            {
                for (int mipLevel = 0; mipLevel < Header.NumberOfMipmapLevels; mipLevel++)
                {
                    MipmapData mipmap = Faces[face].Mipmaps[mipLevel];
                    mipmap.Data.CopyTo(result, (int)start);
                    start += mipmap.SizeInBytes;
                }
            }

            return result;
        }
    }

    public class KtxKeyValuePair
    {
        public string Key { get; }
        public byte[] Value { get; }
        public KtxKeyValuePair(string key, byte[] value)
        {
            Key = key;
            Value = value;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct KtxHeader
    {
        public fixed byte Identifier[12];
        public readonly uint Endianness;
        public readonly uint GlType;
        public readonly uint GlTypeSize;
        public readonly uint GlFormat;
        public readonly uint GlInternalFormat;
        public readonly uint GlBaseInternalFormat;
        public readonly uint PixelWidth;
        private readonly uint _pixelHeight;
        public uint PixelHeight => Math.Max(1, _pixelHeight);
        public readonly uint PixelDepth;
        public readonly uint NumberOfArrayElements;
        public readonly uint NumberOfFaces;
        public readonly uint NumberOfMipmapLevels;
        public readonly uint BytesOfKeyValueData;
    }

}
