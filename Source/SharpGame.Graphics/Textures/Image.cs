using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class Image : DisposeBase
    {
        public ImageType imageType;
        public Format format;
        public Extent3D extent;
        public uint mipLevels;
        public uint arrayLayers;

        internal VkImage handle;
        internal VkDeviceMemory memory;
        internal ulong allocationSize;
        internal uint memoryTypeIndex;

        internal Image(VkImage handle)
        {
            this.handle = handle;
        }

        public unsafe Image(ref ImageCreateInfo imageCreateInfo)
        {            
            imageCreateInfo.ToNative(out VkImageCreateInfo native);
            handle = Device.CreateImage(ref native);

            Device.GetImageMemoryRequirements(handle, out var memReqs);

            VkMemoryAllocateInfo memAllocInfo = VkMemoryAllocateInfo.New();
            memAllocInfo.allocationSize = memReqs.size;
            memAllocInfo.memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, MemoryPropertyFlags.DeviceLocal);

            memory = Device.AllocateMemory(ref memAllocInfo);
            Device.BindImageMemory(handle, memory, 0);

            imageType = imageCreateInfo.imageType;
            format = imageCreateInfo.format;
            extent = imageCreateInfo.extent;
            mipLevels = imageCreateInfo.mipLevels;
            arrayLayers = imageCreateInfo.arrayLayers;

            allocationSize = memAllocInfo.allocationSize;
            memoryTypeIndex = memAllocInfo.memoryTypeIndex;
        }

        protected override void Destroy(bool disposing)
        {
            Device.FreeMemory(memory);

            if(memory != VkDeviceMemory.Null)
                Device.Destroy(handle);
        }

        public unsafe static Image Create(uint width, uint height, ImageCreateFlags flags, uint layers, uint levels, Format format, SampleCountFlags samples,
            ImageUsageFlags usage)
        {
            ImageCreateInfo createInfo = new ImageCreateInfo
            {
                flags = flags,
                imageType = ImageType.Image2D,
                format = format,
                extent = new Extent3D { width = width, height = height, depth = 1 },
                mipLevels = levels,
                arrayLayers = layers,
                samples = samples,
                tiling = ImageTiling.Optimal,
                usage = usage,
                sharingMode = SharingMode.Exclusive,
                initialLayout = ImageLayout.Undefined
            };

            Image image = new Image(ref createInfo);
            return image;
        }


    }

    public struct ImageCreateInfo
    {
        public ImageCreateFlags flags;
        public ImageType imageType;
        public Format format;
        public Extent3D extent;
        public uint mipLevels;
        public uint arrayLayers;
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

    public enum ImageAspectFlags
    {
        None = 0,
        Color = 1,
        Depth = 2,
        Stencil = 4,
        Metadata = 8,
        Plane0KHR = 16,
        Plane1KHR = 32,
        Plane2KHR = 64
    }

    public struct ImageMemoryBarrier
    {
        internal VkImageMemoryBarrier barrier;
        public ImageMemoryBarrier(Image image)
        {
            barrier = VkImageMemoryBarrier.New();
            barrier.image = image.handle;
            barrier.srcQueueFamilyIndex = uint.MaxValue;
            barrier.dstQueueFamilyIndex = uint.MaxValue;
        }

        public ImageMemoryBarrier(Image image, AccessFlags srcAccessMask, AccessFlags dstAccessMask, ImageLayout oldLayout, ImageLayout newLayout,
            ImageAspectFlags aspectMask = ImageAspectFlags.Color, uint baseMipLevel = 0, uint levelCount = uint.MaxValue)
        {
            barrier = VkImageMemoryBarrier.New();

            barrier.srcAccessMask = (VkAccessFlags)srcAccessMask;
            barrier.dstAccessMask = (VkAccessFlags)dstAccessMask;
            barrier.oldLayout = (VkImageLayout)oldLayout;
            barrier.newLayout = (VkImageLayout)newLayout;
            barrier.srcQueueFamilyIndex = uint.MaxValue;
            barrier.dstQueueFamilyIndex = uint.MaxValue;
            barrier.image = image.handle;

            barrier.subresourceRange.aspectMask = (VkImageAspectFlags)aspectMask;
            barrier.subresourceRange.baseMipLevel = baseMipLevel;
            barrier.subresourceRange.levelCount = levelCount;
            barrier.subresourceRange.layerCount = uint.MaxValue;
	    }

        public ImageMemoryBarrier(Image image, AccessFlags srcAccessMask, AccessFlags dstAccessMask, ImageLayout oldLayout, ImageLayout newLayout,
            ImageSubresourceRange subresourceRange)
        {
            barrier = VkImageMemoryBarrier.New();

            barrier.srcAccessMask = (VkAccessFlags)srcAccessMask;
            barrier.dstAccessMask = (VkAccessFlags)dstAccessMask;
            barrier.oldLayout = (VkImageLayout)oldLayout;
            barrier.newLayout = (VkImageLayout)newLayout;
            barrier.srcQueueFamilyIndex = uint.MaxValue;
            barrier.dstQueueFamilyIndex = uint.MaxValue;
            barrier.image = image.handle;

            barrier.subresourceRange = new VkImageSubresourceRange
            {
                aspectMask = (VkImageAspectFlags)subresourceRange.aspectMask,
                baseMipLevel = subresourceRange.baseMipLevel,
                levelCount = subresourceRange.levelCount,
                baseArrayLayer = subresourceRange.baseArrayLayer,
                layerCount = subresourceRange.layerCount,
            };
        }

        public AccessFlags srcAccessMask { get => (AccessFlags)barrier.srcAccessMask; set => barrier.srcAccessMask = (VkAccessFlags)value; }
        public AccessFlags dstAccessMask { get => (AccessFlags)barrier.dstAccessMask; set => barrier.dstAccessMask = (VkAccessFlags)value; }
        public ImageLayout oldLayout { set => barrier.oldLayout = (VkImageLayout)value; }
        public ImageLayout newLayout { set => barrier.newLayout = (VkImageLayout)value; }

        public ImageSubresourceRange subresourceRange
        {
            set
            {
                barrier.subresourceRange = new VkImageSubresourceRange
                {
                    aspectMask = (VkImageAspectFlags)value.aspectMask,
                    baseMipLevel = value.baseMipLevel,
                    levelCount = value.levelCount,
                    baseArrayLayer = value.baseArrayLayer,
                    layerCount = value.layerCount,
                };
             }
        }

    }

    public struct ImageSubresourceLayers
    {
        public ImageAspectFlags aspectMask;
        public uint mipLevel;
        public uint baseArrayLayer;
        public uint layerCount;
    }

    public struct ImageCopy
    {
        public ImageSubresourceLayers srcSubresource;
        public Offset3D srcOffset;
        public ImageSubresourceLayers dstSubresource;
        public Offset3D dstOffset;
        public Extent3D extent;
    }

    public struct ImageBlit
    {
        public ImageSubresourceLayers srcSubresource;
        public Offset3D srcOffsets_0;
        public Offset3D srcOffsets_1;
        public ImageSubresourceLayers dstSubresource;
        public Offset3D dstOffsets_0;
        public Offset3D dstOffsets_1;
    }
}
