using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vulkan;

namespace SharpGame.ImageSharp
{
    using Image = SixLabors.ImageSharp.Image;

    public class ImageSharpTexture : IDisposable
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
        public int Width => Images[0].Width;

        /// <summary>
        /// The height of the largest image in the chain.
        /// </summary>
        public int Height => Images[0].Height;

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
        public int MipLevels => Images.Length;

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
            MipmapData[] mipmaps = new MipmapData[Images.Length];
            for(int i = 0; i < this.Images.Length; i++)
            {
                var image = Images[i];
                uint imageSize = (uint)(image.Width * image.Height * Unsafe.SizeOf<Rgba32>());
                mipmaps[i] = new MipmapData(imageSize, MemoryMarshal.AsBytes(image.GetPixelSpan()).ToArray(), (uint)image.Width, (uint)image.Height);
            }

            ImageData face = new ImageData(Width, Height, Images.Length, mipmaps);
            Texture2D tex = new Texture2D
            {
                width = Width,
                height = Height,
                mipLevels = MipLevels,
                depth = 1,
                format = Format,
                imageUsageFlags = ImageUsageFlags.Sampled,
                imageLayout = ImageLayout.ShaderReadOnlyOptimal,
            };

            tex.SetImage2D(face);

            return tex;

        }

        public void Dispose()
        {
            foreach(var img in Images)
            {
                img.Dispose();
            }
        }

#if false
        private unsafe Texture CreateTextureViaStaging()
        {
            //Texture staging = factory.CreateTexture(
            //    TextureDescription.Texture2D(Width, Height, MipLevels, 1, Format, TextureUsage.Staging));

            Texture staging = new Texture2D
            {
                width = Width,
                height = Height,
                mipLevels = MipLevels,
                depth = 1,
                format = Format
            };

            Device.CreateImage(Width, Height, Format, VkImageTiling.Linear, ImageUsageFlags.TransferSrc,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, out VkImage stagingImage,
                out VkDeviceMemory stagingImageMemory);

            Texture ret 
                //= factory.CreateTexture(
                //TextureDescription.Texture2D(Width, Height, MipLevels, 1, Format, TextureUsage.Sampled));
            = new Texture2D
            {
                width = Width,
                height = Height,
                mipLevels = MipLevels,
                depth = 1,
                format = Format
            };

            Device.CreateImage(Width, Height, Format, VkImageTiling.Optimal, ImageUsageFlags.TransferDst | ImageUsageFlags.Sampled,
                VkMemoryPropertyFlags.DeviceLocal, out VkImage retImage,
                out VkDeviceMemory retImageMemory);

            var cl = Device.BeginOneTimeCommands();
            ulong offset = 0;
            
            for (uint level = 0; level < MipLevels; level++)
            {
                Image<Rgba32> image = Images[level];
                fixed (void* pin = &MemoryMarshal.GetReference(image.GetPixelSpan()))
                {
                    VkImageSubresource subresource = new VkImageSubresource();
                    subresource.aspectMask = VkImageAspectFlags.Color;
                    subresource.mipLevel = level;
                    subresource.arrayLayer = 0;

                    VulkanNative.vkGetImageSubresourceLayout(Graphics.device, stagingImage, ref subresource, out VkSubresourceLayout stagingImageLayout);
                    ulong rowPitch = stagingImageLayout.rowPitch;
                    ulong imageSize = (ulong)(image.Width * image.Height * Unsafe.SizeOf<Rgba32>());

                    void* mappedPtr = Device.MapMemory(stagingImageMemory, offset, imageSize, 0);
                    uint rowWidth = (uint)(image.Width * 4);
                    if (rowWidth == rowPitch)
                    {
                        Unsafe.CopyBlock(mappedPtr, pin, (uint)(image.Width * image.Height * 4));
                    }
                    else
                    {
                        for (uint y = 0; y < image.Height; y++)
                        {
                            byte* dstStart = (byte*)mappedPtr + y * rowPitch;
                            byte* srcStart = (byte*)pin + y * rowWidth;
                            Unsafe.CopyBlock(dstStart, srcStart, rowWidth);
                        }
                    }

                    Device.UnmapMemory(stagingImageMemory);
                    offset += imageSize;
                    /*
                    Tools.SetImageLayout(
                            cl,
                            image,
                            VkImageAspectFlags.Color,
                            VkImageLayout.Undefined,
                            VkImageLayout.TransferDstOptimal,
                            subresource);*/


                    //VulkanNative.vkCmdCopyImage(cl, stagingImage, re)
                    /*
                        cl.CopyTexture(
                        staging, 0, 0, 0, level, 0,
                        ret, 0, 0, 0, level, 0,
                        (uint)image.Width, (uint)image.Height, 1, 1);*/


                }
            }

            Device.EndOneTimeCommands(cl);

            Device.TransitionImageLayout(retImage, VkFormat.R8g8b8a8Unorm, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);

            staging.Dispose();
            
            return ret;
        }

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
                {/*
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
                        0);*/
                }
            }

            return tex;
        }

#endif
    }
}
