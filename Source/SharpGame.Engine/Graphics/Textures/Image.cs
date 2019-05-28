using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class Image : DisposeBase
    {
        internal VkImage handle;

        public Image(ref ImageCreateInfo imageCreateInfo)
        {
            imageCreateInfo.ToNative(out VkImageCreateInfo native);
            handle = Device.CreateImage(ref native);
        }

        protected override void Destroy()
        {
            Device.Destroy(handle);
        }
    }

    public struct ImageCreateInfo
    {
        public ImageCreateFlags flags;
        public ImageType imageType;
        public Format format;
        public Extent3D extent;
        public int mipLevels;
        public int arrayLayers;
        public SampleCountFlags samples;
        public ImageTiling tiling;
        public ImageUsageFlags usage;
        public SharingMode sharingMode;
        public uint[] queueFamilyIndices;
        public ImageLayout initialLayout;

        internal unsafe void ToNative(out VkImageCreateInfo native)
        {
            native = VkImageCreateInfo.New();
            native.flags = (VkImageCreateFlags)flags;
            native.imageType = (VkImageType)imageType;
            native.format = (VkFormat)format;
            native.extent = new VkExtent3D { width = (uint)extent.width, height = (uint)extent.height, depth = (uint)extent.depth };
            native.mipLevels = (uint)mipLevels;
            native.arrayLayers = (uint)arrayLayers;
            native.samples = (VkSampleCountFlags)samples;
            native.tiling = (VkImageTiling)tiling;
            native.usage = (VkImageUsageFlags)usage;
            native.sharingMode = (VkSharingMode)sharingMode;

            if (!queueFamilyIndices.IsNullOrEmpty())
            {
                native.queueFamilyIndexCount = (uint)queueFamilyIndices.Length;
                native.pQueueFamilyIndices = (uint*)Unsafe.AsPointer(ref queueFamilyIndices[0]);
            }

            native.initialLayout = (VkImageLayout)initialLayout;

        }
    }

}
