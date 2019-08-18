﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class Image : DisposeBase
    {
        internal VkImage handle;

        internal VkDeviceMemory memory;
        internal ulong allocationSize;
        internal uint memoryTypeIndex;

        public Image(ref ImageCreateInfo imageCreateInfo)
        {
            imageCreateInfo.ToNative(out VkImageCreateInfo native);
            handle = Device.CreateImage(ref native);
        }

        protected override void Destroy()
        {
            Device.Destroy(handle);
        }

        public unsafe static Image Create(uint width, uint height, uint layers, uint levels, Format format, uint samples, ImageUsageFlags usage)
        {
            ImageCreateInfo createInfo = new ImageCreateInfo
            {
                flags = (layers == 6) ? ImageCreateFlags.CubeCompatible : 0,
                imageType = ImageType.Image2D,
                format = format,
                extent = new Extent3D { width = width, height = height, depth = 1 },
                mipLevels = levels,
                arrayLayers = layers,
                samples = (SampleCountFlags)samples,
                tiling = ImageTiling.Optimal,
                usage = usage,
                sharingMode = SharingMode.Exclusive,
                initialLayout = ImageLayout.Undefined
            };

            Image image = new Image(ref createInfo);
         
            VkMemoryRequirements memoryRequirements;
            Device.GetImageMemoryRequirements(image.handle, out memoryRequirements);
           
            VkMemoryAllocateInfo allocateInfo = VkMemoryAllocateInfo.New();
            allocateInfo.allocationSize = memoryRequirements.size;
            allocateInfo.memoryTypeIndex = Device.GetMemoryType(memoryRequirements.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal, null);
            image.memory = Device.AllocateMemory(ref allocateInfo);

            Device.BindImageMemory(image.handle, image.memory, 0);
            image.allocationSize = allocateInfo.allocationSize;
            image.memoryTypeIndex = allocateInfo.memoryTypeIndex;
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


    public struct ImageMemoryBarrier
    {
        internal VkImageMemoryBarrier barrier;
        public ImageMemoryBarrier(Texture texture, AccessFlags srcAccessMask, AccessFlags dstAccessMask, ImageLayout oldLayout, ImageLayout newLayout)
        {
            barrier = VkImageMemoryBarrier.New();

            barrier.srcAccessMask = (VkAccessFlags)srcAccessMask;
            barrier.dstAccessMask = (VkAccessFlags)dstAccessMask;
            barrier.oldLayout = (VkImageLayout)oldLayout;
            barrier.newLayout = (VkImageLayout)newLayout;
            barrier.srcQueueFamilyIndex = uint.MaxValue;
            barrier.dstQueueFamilyIndex = uint.MaxValue;
            barrier.image = texture.image.handle;
            barrier.subresourceRange.aspectMask = VkImageAspectFlags.Color;
            barrier.subresourceRange.levelCount = uint.MaxValue;
            barrier.subresourceRange.layerCount = uint.MaxValue;
	    }

        public void SetApectMask(VkImageAspectFlags aspectMask)
        {
            barrier.subresourceRange.aspectMask = aspectMask;
        }

        public void SetMipLevels(uint baseMipLevel, uint levelCount = uint.MaxValue)
        {
            barrier.subresourceRange.baseMipLevel = baseMipLevel;
            barrier.subresourceRange.levelCount = levelCount;
        }

        public void SetArrayLayers(uint baseArrayLayer, uint layerCount = uint.MaxValue)
        {
            barrier.subresourceRange.baseArrayLayer = baseArrayLayer;
            barrier.subresourceRange.layerCount = layerCount;
        }
    }

}
