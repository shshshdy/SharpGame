using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Vulkan;

namespace SharpGame
{
    using static Vulkan.VulkanNative;

    public class Texture : Resource, IBindableResource
    {
        public int width;
        public int height;
        public int mipLevels;
        public int depth;
        public Format format;
        public ImageUsageFlags imageUsageFlags;
        public ImageLayout imageLayout;

        internal ImageView view;
        internal Image image;
        internal Sampler sampler;

        internal VkDeviceMemory deviceMemory;
        internal VkDescriptorImageInfo descriptor;

        public Texture()
        {
        }

        internal void UpdateDescriptor()
        {
            descriptor.sampler = sampler.handle;
            descriptor.imageView = view.handle;
            descriptor.imageLayout = (VkImageLayout)imageLayout;
        }

        protected override void Destroy()
        {
            view.Dispose();
            image.Dispose();
            sampler.Dispose();
            Device.FreeMemory(deviceMemory);

            base.Destroy();
        }

    }

    public class ImageData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int NumberOfMipmapLevels { get; }
        public MipmapData[] Mipmaps { get; }

        public ImageData(int width, int height, int numberOfMipmapLevels, MipmapData[] mipmaps)
        {
            Width = width;
            Height = height;
            NumberOfMipmapLevels = numberOfMipmapLevels;
            Mipmaps = mipmaps;
        }

        public ImageData(int numberOfMipmapLevels)
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
