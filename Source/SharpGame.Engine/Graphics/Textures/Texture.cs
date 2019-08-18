using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Vulkan;

namespace SharpGame
{
    using static Vulkan.VulkanNative;

    public partial class Texture : Resource, IBindableResource
    {
        public uint width;
        public uint height;
        public uint layers;
        public uint mipLevels;
        public uint depth;

        public Format format;
        public ImageUsageFlags imageUsageFlags;
        public ImageLayout imageLayout;

        internal ImageView imageView;
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
            descriptor.imageView = imageView.handle;
            descriptor.imageLayout = (VkImageLayout)imageLayout;
        }


        protected override void Destroy()
        {
            imageView?.Dispose();
            image?.Dispose();
            sampler?.Dispose();

            Device.FreeMemory(deviceMemory);

            base.Destroy();
        }

        static int NumMipmapLevels(uint width, uint height)
        {
            int levels = 1;
            while (((width | height) >> levels) != 0)
            {
                ++levels;
            }
            return levels;
        }

        public static Texture White;
        public static Texture Gray;
        public static Texture Black;
        public static Texture Purple;

        public unsafe static void Init()
        {
            Texture CreateTex(Color color)
            {
                byte* c = &color.R;
                return Texture.Create2D(1, 1, Format.R8g8b8a8Unorm, c);
            }

            White = CreateTex(Color.White);
            Gray = CreateTex(Color.Gray);
            Black = CreateTex(Color.Black);
            Purple = CreateTex(Color.Purple);
        }

        public static Texture Create(uint width, uint height, uint layers, Format format, uint levels = 0, ImageUsageFlags additionalUsage = ImageUsageFlags.None)
        {
            Texture texture = new Texture
            {
                width = width,
                height = height,
                layers = layers,
                mipLevels = (levels > 0) ? levels : (uint)NumMipmapLevels(width, height)
            };

            ImageUsageFlags usage = ImageUsageFlags.Sampled | ImageUsageFlags.TransferDst | additionalUsage;
            if (texture.mipLevels > 1)
            {
                usage |= ImageUsageFlags.TransferSrc; // For mipmap generation
            }

            texture.image = Image.Create(width, height, layers, texture.mipLevels, format, 1, usage);
            texture.imageView = ImageView.Create(texture, format, VkImageAspectFlags.Color, 0, RemainingMipLevels);


            SamplerCreateInfo sampler = new SamplerCreateInfo
            {
                magFilter = Filter.Linear,
                minFilter = Filter.Linear,
                mipmapMode = SamplerMipmapMode.Linear,
                addressModeU = SamplerAddressMode.ClampToBorder,
                addressModeV = SamplerAddressMode.ClampToBorder,
                addressModeW = SamplerAddressMode.ClampToBorder,
                mipLodBias = 0.0f,
                compareOp = CompareOp.Never,
                minLod = 0.0f,
                // Set max level-of-detail to mip level count of the texture
                maxLod = (float)texture.mipLevels
            };
            // Enable anisotropic filtering
            // This feature is optional, so we must check if it's supported on the Device
            if (Device.Features.samplerAnisotropy == 1)
            {
                // Use max. level of anisotropy for this example
                sampler.maxAnisotropy = Device.Properties.limits.maxSamplerAnisotropy;
                sampler.anisotropyEnable = true;
            }
            else
            {
                // The Device does not support anisotropic filtering
                sampler.maxAnisotropy = 1.0f;
                sampler.anisotropyEnable = false;
            }

            sampler.borderColor = BorderColor.FloatOpaqueWhite;
            texture.sampler = new Sampler(ref sampler);
            texture.UpdateDescriptor();
            return texture;
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
