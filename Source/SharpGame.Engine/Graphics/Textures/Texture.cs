using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Vulkan;

namespace SharpGame
{
    using static Vulkan.VulkanNative;

    public class Texture : Resource, IBindable
    {
        public uint width;
        public uint height;
        public uint mipLevels;
        public uint depth;
        public Format format;
        public ImageUsageFlags imageUsageFlags;
        public ImageLayout imageLayout;

        internal VkImageView view;
        internal VkImage image;
        internal VkSampler sampler;
        internal VkDeviceMemory deviceMemory;
        internal VkDescriptorImageInfo descriptor;

        public Texture()
        {
        }

        public void UpdateTexture(IntPtr pixel, uint sizeInBytes,
            uint x,
            uint y,
            uint z,
            uint width,
            uint height,
            uint depth,
            uint mipLevel,
            uint arrayLayer)
        {
            this.width = width;
            this.height = height;
            this.mipLevels = mipLevel;
            this.depth = depth;
            //    format = Format.R8g8b8a8Unorm
        }

        internal void UpdateDescriptor()
        {
            descriptor.sampler = sampler;
            descriptor.imageView = view;
            descriptor.imageLayout = (VkImageLayout)imageLayout;
        }

        protected override void Destroy()
        {

            vkDestroyImageView(Graphics.device, view, IntPtr.Zero);
            vkDestroyImage(Graphics.device, image, IntPtr.Zero);
            vkDestroySampler(Graphics.device, sampler, IntPtr.Zero);
            Device.FreeMemory(deviceMemory);

            base.Destroy();
        }

    }

    public class ImageData
    {
        public uint Width { get; set; }
        public uint Height { get; set; }
        public uint NumberOfMipmapLevels { get; }
        public MipmapData[] Mipmaps { get; }

        public ImageData(uint width, uint height, uint numberOfMipmapLevels, MipmapData[] mipmaps)
        {
            Width = width;
            Height = height;
            NumberOfMipmapLevels = numberOfMipmapLevels;
            Mipmaps = mipmaps;
        }

        public ImageData(uint numberOfMipmapLevels)
        {
            NumberOfMipmapLevels = numberOfMipmapLevels;
            Mipmaps = new MipmapData[numberOfMipmapLevels];
        }


        public ulong GetTotalSize()
        {
            ulong totalSize = 0;

            for (int mipLevel = 0; mipLevel < Mipmaps.Length; mipLevel++)
            {
                MipmapData mipmap = Mipmaps[mipLevel];
                totalSize += mipmap.SizeInBytes;

            }

            return totalSize;
        }


        public byte[] GetAllTextureData()
        {
            byte[] result = new byte[GetTotalSize()];
            uint start = 0;

            for (int mipLevel = 0; mipLevel < Mipmaps.Length; mipLevel++)
            {
                MipmapData mipmap = Mipmaps[mipLevel];
                mipmap.Data.CopyTo(result, (int)start);
                start += mipmap.SizeInBytes;
            }


            return result;
        }
    }

    public class MipmapData
    {
        public uint SizeInBytes { get; }
        public byte[] Data { get; }
        public uint Width { get; }
        public uint Height { get; }

        public MipmapData(uint sizeInBytes, byte[] data, uint width, uint height)
        {
            SizeInBytes = sizeInBytes;
            Data = data;
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
        }
    }
}
