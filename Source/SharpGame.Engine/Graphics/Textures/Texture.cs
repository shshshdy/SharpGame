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

}
