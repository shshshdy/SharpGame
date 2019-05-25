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
        public VkImageView view;
        public VkImage image;
        public VkSampler sampler;
        public VkDeviceMemory deviceMemory;
        public uint width;
        public uint height;
        public uint mipLevels;
        public uint depth;
        public Format format;
        public ImageLayout imageLayout;
        public VkDescriptorImageInfo descriptor;

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
