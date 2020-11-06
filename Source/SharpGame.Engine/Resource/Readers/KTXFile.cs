using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    // A hand-crafted KTX file parser.
    // https://www.khronos.org/opengles/sdk/tools/KTX/file_format_spec
    public class KtxFile
    {
        public KtxHeader Header { get; }
        public KtxKeyValuePair[] KeyValuePairs { get; }
        public MipmapLevel[] Mipmaps { get; }

        public KtxFile(KtxHeader header, KtxKeyValuePair[] keyValuePairs, MipmapLevel[] mipmaps)
        {
            Header = header;
            KeyValuePairs = keyValuePairs;
            Mipmaps = mipmaps;
        }

        public static KtxFile Load(byte[] bytes, bool readKeyValuePairs)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return Load(ms, readKeyValuePairs);
            }
        }

        public static KtxFile Load(Stream s, bool readKeyValuePairs)
        {
            using (BinaryReader br = new BinaryReader(s))
            {
                KtxHeader header = ReadStruct<KtxHeader>(br);

                KtxKeyValuePair[] kvps = null;
                if (readKeyValuePairs)
                {
                    int keyValuePairBytesRead = 0;
                    List<KtxKeyValuePair> keyValuePairs = new List<KtxKeyValuePair>();
                    while (keyValuePairBytesRead < header.BytesOfKeyValueData)
                    {
                        int bytesRemaining = (int)(header.BytesOfKeyValueData - keyValuePairBytesRead);
                        KtxKeyValuePair kvp = ReadNextKeyValuePair(br, out int read);
                        keyValuePairBytesRead += read;
                        keyValuePairs.Add(kvp);
                    }

                    kvps = keyValuePairs.ToArray();
                }
                else
                {
                    br.BaseStream.Seek(header.BytesOfKeyValueData, SeekOrigin.Current); // Skip over header data.
                }

                uint numberOfMipmapLevels = Math.Max(1, header.NumberOfMipmapLevels);
                uint numberOfArrayElements = Math.Max(1, header.NumberOfArrayElements);
                uint numberOfFaces = Math.Max(1, header.NumberOfFaces);

                uint baseWidth = Math.Max(1, header.PixelWidth);
                uint baseHeight = Math.Max(1, header.PixelHeight);
                uint baseDepth = Math.Max(1, header.PixelDepth);

                MipmapLevel[] images = new MipmapLevel[numberOfMipmapLevels];
                for (int mip = 0; mip < numberOfMipmapLevels; mip++)
                {
                    uint mipWidth = Math.Max(1, baseWidth / (uint)(Math.Pow(2, mip)));
                    uint mipHeight = Math.Max(1, baseHeight / (uint)(Math.Pow(2, mip)));
                    uint mipDepth = Math.Max(1, baseDepth / (uint)(Math.Pow(2, mip)));

                    uint imageSize = br.ReadUInt32();
                    uint arrayElementSize = imageSize / numberOfArrayElements;
                    ArrayElement[] arrayElements = new ArrayElement[numberOfArrayElements];
                    for (int arr = 0; arr < numberOfArrayElements; arr++)
                    {
                        uint faceSize = arrayElementSize / numberOfFaces;
                        ImageFace[] faces = new ImageFace[numberOfFaces];
                        for (int face = 0; face < numberOfFaces; face++)
                        {
                            faces[face] = new ImageFace(br.ReadBytes((int)faceSize));
                        }

                        arrayElements[arr] = new ArrayElement(faces);
                    }

                    images[mip] = new MipmapLevel(
                        mipWidth,
                        mipHeight,
                        mipDepth,
                        imageSize,
                        arrayElementSize,
                        arrayElements);

                    uint mipPaddingBytes = 3 - ((imageSize + 3) % 4);
                    br.BaseStream.Seek(mipPaddingBytes, SeekOrigin.Current);
                }

                return new KtxFile(header, kvps, images);
            }
        }

        private static unsafe KtxKeyValuePair ReadNextKeyValuePair(BinaryReader br, out int bytesRead)
        {
            uint keyAndValueByteSize = br.ReadUInt32();
            byte* keyAndValueBytes = stackalloc byte[(int)keyAndValueByteSize];
            ReadBytes(br, keyAndValueBytes, (int)keyAndValueByteSize);
            int paddingByteCount = (int)(3 - ((keyAndValueByteSize + 3) % 4));
            br.BaseStream.Seek(paddingByteCount, SeekOrigin.Current); // Skip padding bytes

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

        private static unsafe T ReadStruct<T>(BinaryReader br)
        {
            int size = Unsafe.SizeOf<T>();
            byte* bytes = stackalloc byte[size];
            for (int i = 0; i < size; i++)
            {
                bytes[i] = br.ReadByte();
            }

            return Unsafe.Read<T>(bytes);
        }

        private static unsafe void ReadBytes(BinaryReader br, byte* destination, int count)
        {
            for (int i = 0; i < count; i++)
            {
                destination[i] = br.ReadByte();
            }
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