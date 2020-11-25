﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SharpGame.ImageSharp
{
    using Image = SixLabors.ImageSharp.Image;
    public class ImageSharpCubemapTexture
    {
        /// <summary>
        /// An array of images, each face of a cubemap.
        /// Access of CubemapTextures[2][3] means face 2 with mipmap level 3
        /// </summary>
        public Image<Rgba32>[][] CubemapTextures { get; }

        /// <summary>
        /// The width of a cubemap texture.
        /// </summary>
        public uint Width => (uint)CubemapTextures[0][0].Width;

        /// <summary>
        /// The height of a cubemap texture.
        /// </summary>
        public uint Height => (uint)CubemapTextures[0][0].Height;

        /// <summary>
        /// The pixel format cubemap textures.
        /// </summary>
        public VkFormat VkFormat => VkFormat.R8G8B8A8UNorm;

        /// <summary>
        /// The size of each pixel, in bytes.
        /// </summary>
        public uint PixelSizeInBytes => sizeof(byte) * 4;

        /// <summary>
        /// The number of levels in the mipmap chain. This is equal to the length of the Images array.
        /// </summary>
        public uint MipLevels => (uint)CubemapTextures[0].Length;

        /// <summary>
        /// Provides standardized access to the cubemap texture array
        /// </summary>
        private const int PositiveXArrayLayer = 0;
        private const int NegativeXArrayLayer = 1;
        private const int PostitiveYArrayLayer = 2;
        private const int NegativeYArrayLayer = 3;
        private const int PositiveZArrayLayer = 4;
        private const int NegativeZArrayLayer = 5;

        public ImageSharpCubemapTexture(
            string positiveXPath,
            string negativeXPath,
            string positiveYPath,
            string negativeYPath,
            string positiveZPath,
            string negativeZPath) : this(
                Image.Load<Rgba32>(positiveXPath),
                Image.Load<Rgba32>(negativeXPath),
                Image.Load<Rgba32>(positiveYPath),
                Image.Load<Rgba32>(negativeYPath),
                Image.Load<Rgba32>(positiveZPath), 
                Image.Load<Rgba32>(negativeZPath),
                true) { }

        public ImageSharpCubemapTexture(
            string positiveXPath,
            string negativeXPath,
            string positiveYPath,
            string negativeYPath,
            string positiveZPath,
            string negativeZPath,
            bool mipmap) : this(
                Image.Load<Rgba32>(positiveXPath),
                Image.Load<Rgba32>(negativeXPath),
                Image.Load<Rgba32>(positiveYPath),
                Image.Load<Rgba32>(negativeYPath),
                Image.Load<Rgba32>(positiveZPath),
                Image.Load<Rgba32>(negativeZPath),
                mipmap)
        { }

        public ImageSharpCubemapTexture(
            Stream positiveXStream,
            Stream negativeXStream,
            Stream positiveYStream,
            Stream negativeYStream,
            Stream positiveZStream,
            Stream negativeZStream,
            bool mipmap) : this(
                Image.Load<Rgba32>(positiveXStream),
                Image.Load<Rgba32>(negativeXStream),
                Image.Load<Rgba32>(positiveYStream),
                Image.Load<Rgba32>(negativeYStream),
                Image.Load<Rgba32>(positiveZStream),
                Image.Load<Rgba32>(negativeZStream),
                mipmap)
        { }

        public ImageSharpCubemapTexture(
            Image<Rgba32> positiveX,
            Image<Rgba32> negativeX,
            Image<Rgba32> positiveY,
            Image<Rgba32> negativeY,
            Image<Rgba32> positiveZ,
            Image<Rgba32> negativeZ,
            bool mipmap = true)
        {
            CubemapTextures = new Image<Rgba32>[6][];
            if (mipmap)
            {
                CubemapTextures[0] = MipmapHelper.GenerateMipmaps(positiveX);
                CubemapTextures[1] = MipmapHelper.GenerateMipmaps(negativeX);
                CubemapTextures[2] = MipmapHelper.GenerateMipmaps(positiveY);
                CubemapTextures[3] = MipmapHelper.GenerateMipmaps(negativeY);
                CubemapTextures[4] = MipmapHelper.GenerateMipmaps(positiveZ);
                CubemapTextures[5] = MipmapHelper.GenerateMipmaps(negativeZ);
            }

            else
            {
                CubemapTextures[0] = new Image<Rgba32>[1] { positiveX };
                CubemapTextures[1] = new Image<Rgba32>[1] { negativeX };
                CubemapTextures[2] = new Image<Rgba32>[1] { positiveY };
                CubemapTextures[3] = new Image<Rgba32>[1] { negativeY };
                CubemapTextures[4] = new Image<Rgba32>[1] { positiveZ };
                CubemapTextures[5] = new Image<Rgba32>[1] { negativeZ };
            }

        }
        /*
        public unsafe Texture CreateDeviceTexture(GraphicsDevice gd, ResourceFactory factory)
        {
            Texture cubemapTexture = factory.CreateTexture(TextureDescription.Texture2D(
                        Width,
                        Height,
                        MipLevels,
                        1,
                        VkFormat,
                        TextureUsage.Sampled | TextureUsage.Cubemap));

            for (int level = 0; level < MipLevels; level++){
                fixed (Rgba32* positiveXPin = &MemoryMarshal.GetReference(CubemapTextures[PositiveXArrayLayer][level].GetPixelSpan()))
                fixed (Rgba32* negativeXPin = &MemoryMarshal.GetReference(CubemapTextures[NegativeXArrayLayer][level].GetPixelSpan()))
                fixed (Rgba32* positiveYPin = &MemoryMarshal.GetReference(CubemapTextures[PostitiveYArrayLayer][level].GetPixelSpan()))
                fixed (Rgba32* negativeYPin = &MemoryMarshal.GetReference(CubemapTextures[NegativeYArrayLayer][level].GetPixelSpan()))
                fixed (Rgba32* positiveZPin = &MemoryMarshal.GetReference(CubemapTextures[PositiveZArrayLayer][level].GetPixelSpan()))
                fixed (Rgba32* negativeZPin = &MemoryMarshal.GetReference(CubemapTextures[NegativeZArrayLayer][level].GetPixelSpan()))
                {
                    Image<Rgba32> image = CubemapTextures[0][level];
                    uint width = (uint)image.Width;
                    uint height = (uint)image.Height;
                    uint faceSize = width * height * PixelSizeInBytes;
                    gd.UpdateTexture(cubemapTexture, (IntPtr)positiveXPin, faceSize, 0, 0, 0, width, height, 1, (uint)level, PositiveXArrayLayer);
                    gd.UpdateTexture(cubemapTexture, (IntPtr)negativeXPin, faceSize, 0, 0, 0, width, height, 1, (uint)level, NegativeXArrayLayer);
                    gd.UpdateTexture(cubemapTexture, (IntPtr)positiveYPin, faceSize, 0, 0, 0, width, height, 1, (uint)level, PostitiveYArrayLayer);
                    gd.UpdateTexture(cubemapTexture, (IntPtr)negativeYPin, faceSize, 0, 0, 0, width, height, 1, (uint)level, NegativeYArrayLayer);
                    gd.UpdateTexture(cubemapTexture, (IntPtr)positiveZPin, faceSize, 0, 0, 0, width, height, 1, (uint)level, PositiveZArrayLayer);
                    gd.UpdateTexture(cubemapTexture, (IntPtr)negativeZPin, faceSize, 0, 0, 0, width, height, 1, (uint)level, NegativeZArrayLayer);
                }
            }
            return cubemapTexture;
        }*/
    }
}

