﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGame.ImageSharp
{
    using Image = SixLabors.ImageSharp.Image;

    public class ImageSharpTexture
    {
        /// <summary>
        /// An array of images, each a single element in the mipmap chain.
        /// The first element is the largest, most detailed level, and each subsequent element
        /// is half its size, down to 1x1 pixel.
        /// </summary>
        public Image<Rgba32>[] Images { get; }

        /// <summary>
        /// The width of the largest image in the chain.
        /// </summary>
        public uint Width => (uint)Images[0].Width;

        /// <summary>
        /// The height of the largest image in the chain.
        /// </summary>
        public uint Height => (uint)Images[0].Height;

        /// <summary>
        /// The pixel format of all images.
        /// </summary>
        public Format Format { get; }

        /// <summary>
        /// The size of each pixel, in bytes.
        /// </summary>
        public uint PixelSizeInBytes => sizeof(byte) * 4;

        /// <summary>
        /// The number of levels in the mipmap chain. This is equal to the length of the Images array.
        /// </summary>
        public uint MipLevels => (uint)Images.Length;

        public ImageSharpTexture(string path) : this(Image.Load<Rgba32>(path), true) { }
        public ImageSharpTexture(string path, bool mipmap) : this(Image.Load<Rgba32>(path), mipmap) { }
        public ImageSharpTexture(string path, bool mipmap, bool srgb) : this(Image.Load<Rgba32>(path), mipmap, srgb) { }
        public ImageSharpTexture(Stream stream) : this(Image.Load<Rgba32>(stream), true) { }
        public ImageSharpTexture(Stream stream, bool mipmap) : this(Image.Load<Rgba32>(stream), mipmap) { }
        public ImageSharpTexture(Stream stream, bool mipmap, bool srgb) : this(Image.Load<Rgba32>(stream), mipmap, srgb) { }
        public ImageSharpTexture(Image<Rgba32> image, bool mipmap = true) : this(image, mipmap, false) { }
        public ImageSharpTexture(Image<Rgba32> image, bool mipmap, bool srgb)
        {
            Format = /*srgb ? Format.R8g8b8a8Unorm_Srgb :*/ Format.R8g8b8a8Unorm;
            if (mipmap)
            {
                Images = MipmapHelper.GenerateMipmaps(image);
            }
            else
            {
                Images = new Image<Rgba32>[] { image };
            }
        }
  
        public unsafe Texture CreateDeviceTexture()
        {
            return CreateTextureViaUpdate();
        }
      /*
        private unsafe Texture CreateTextureViaStaging(GraphicsDevice gd, ResourceFactory factory)
        {
            Texture staging = factory.CreateTexture(
                TextureDescription.Texture2D(Width, Height, MipLevels, 1, Format, TextureUsage.Staging));

            Texture ret = factory.CreateTexture(
                TextureDescription.Texture2D(Width, Height, MipLevels, 1, Format, TextureUsage.Sampled));

            CommandList cl = gd.ResourceFactory.CreateCommandList();
            cl.Begin();
            for (uint level = 0; level < MipLevels; level++)
            {
                Image<Rgba32> image = Images[level];
                fixed (void* pin = &MemoryMarshal.GetReference(image.GetPixelSpan()))
                {
                    MappedResource map = gd.Map(staging, MapMode.Write, level);
                    uint rowWidth = (uint)(image.Width * 4);
                    if (rowWidth == map.RowPitch)
                    {
                        Unsafe.CopyBlock(map.Data.ToPointer(), pin, (uint)(image.Width * image.Height * 4));
                    }
                    else
                    {
                        for (uint y = 0; y < image.Height; y++)
                        {
                            byte* dstStart = (byte*)map.Data.ToPointer() + y * map.RowPitch;
                            byte* srcStart = (byte*)pin + y * rowWidth;
                            Unsafe.CopyBlock(dstStart, srcStart, rowWidth);
                        }
                    }
                    gd.Unmap(staging, level);

                    cl.CopyTexture(
                        staging, 0, 0, 0, level, 0,
                        ret, 0, 0, 0, level, 0,
                        (uint)image.Width, (uint)image.Height, 1, 1);

                }
            }
            cl.End();

            gd.SubmitCommands(cl);
            staging.Dispose();
            cl.Dispose();

            return ret;
        }*/

        private unsafe Texture CreateTextureViaUpdate()
        {
            Texture tex = new Texture2D
            {
                width = Width,
                height = Height,
                mipLevels = MipLevels,
                depth = 1,
                format = Format
            };

                /*factory.CreateTexture(TextureDescription.Texture2D(
                Width, Height, MipLevels, 1, Format, TextureUsage.Sampled));*/
            for (int level = 0; level < MipLevels; level++)
            {
                Image<Rgba32> image = Images[level];
                fixed (void* pin = &MemoryMarshal.GetReference(image.GetPixelSpan()))
                {
                    tex.UpdateTexture(                        
                        (IntPtr)pin,
                        (uint)(PixelSizeInBytes * image.Width * image.Height),
                        0,
                        0,
                        0,
                        (uint)image.Width,
                        (uint)image.Height,
                        1,
                        (uint)level,
                        0);
                }
            }

            return tex;
        }
    }
}
