using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;


namespace SharpGame
{
    public class Image : DisposeBase
    {
        public VkImage handle;
        internal VkDeviceMemory memory;
        internal ulong allocationSize;
        internal uint memoryTypeIndex;
        public VkImageType imageType;
        public VkFormat format;
        public VkExtent3D extent;
        public uint mipLevels;
        public uint arrayLayers;

        public Image(VkImage handle)
        {
            this.handle = handle;
        }

        public unsafe Image(ref VkImageCreateInfo imageCreateInfo)
        {            
            handle = Device.CreateImage(ref imageCreateInfo);

            Device.GetImageMemoryRequirements(this, out var memReqs);

            VkMemoryAllocateInfo memAllocInfo = new VkMemoryAllocateInfo();
            memAllocInfo.sType = VkStructureType.MemoryAllocateInfo;
            memAllocInfo.allocationSize = memReqs.size;
            memAllocInfo.memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

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

            //Donot destroy swapchain image 
            if(memory != VkDeviceMemory.Null)
                Device.Destroy(handle);
        }

        public unsafe static Image Create(uint width, uint height, VkImageCreateFlags flags, uint layers, uint levels,
            Format format, VkSampleCountFlags samples, VkImageUsageFlags usage)
        {
            var imageType = height == 1 ? width > 1 ? VkImageType.Image1D : VkImageType.Image2D : VkImageType.Image2D;
            var createInfo = new VkImageCreateInfo
            {
                sType = VkStructureType.ImageCreateInfo,
                flags = flags,
                imageType = imageType,
                format = (VkFormat)format,
                extent = new VkExtent3D(width, height, 1),
                mipLevels = levels,
                arrayLayers = layers,
                samples = samples,
                tiling = VkImageTiling.Optimal,
                usage = usage,
                sharingMode = VkSharingMode.Exclusive,
                initialLayout = VkImageLayout.Undefined
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
        public VkExtent3D extent;
        public uint mipLevels;
        public uint arrayLayers;
        public SampleCountFlags samples;
        public ImageTiling tiling;
        public ImageUsageFlags usage;
        public VkSharingMode sharingMode;
        public uint[] queueFamilyIndices;
        public ImageLayout initialLayout;

        internal unsafe void ToNative(out VkImageCreateInfo native)
        {
            native = new VkImageCreateInfo();
            native.sType = VkStructureType.ImageCreateInfo;
            native.flags = (VkImageCreateFlags)flags;
            native.imageType = (VkImageType)imageType;
            native.format = (VkFormat)format;
            native.extent = new VkExtent3D ((uint)extent.width, (uint)extent.height, (uint)extent.depth);
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
    /*
    public struct ImageMemoryBarrier
    {
        internal VkImageMemoryBarrier barrier;
        public ImageMemoryBarrier(Image image)
        {
            barrier = new VkImageMemoryBarrier
            {
                sType = VkStructureType.ImageMemoryBarrier
            };
            barrier.image = image.handle;
            barrier.srcQueueFamilyIndex = uint.MaxValue;
            barrier.dstQueueFamilyIndex = uint.MaxValue;
        }

        public ImageMemoryBarrier(Image image, AccessFlags srcAccessMask, AccessFlags dstAccessMask, ImageLayout oldLayout, ImageLayout newLayout,
            ImageAspectFlags aspectMask = ImageAspectFlags.Color, uint baseMipLevel = 0, uint levelCount = uint.MaxValue)
        {
            barrier = new VkImageMemoryBarrier
            {
                sType = VkStructureType.ImageMemoryBarrier
            };

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
            VkImageSubresourceRange subresourceRange)
        {
            barrier = new VkImageMemoryBarrier
            {
                sType = VkStructureType.ImageMemoryBarrier
            };
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
        public VkImageLayout oldLayout { set => barrier.oldLayout = (VkImageLayout)value; }
        public VkImageLayout newLayout { set => barrier.newLayout = (VkImageLayout)value; }

        public VkImageSubresourceRange subresourceRange
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

    }*/


}
