﻿using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using global::System.Runtime.CompilerServices;
    using static Vulkan.VulkanNative;

    public partial class Texture : Resource
    {
        public unsafe static Texture Create2D(uint w, uint h, Format format, byte* tex2DDataPtr, bool dynamic = false)
        {
            var texture = new Texture
            {
                width = w,
                height = h,
                mipLevels = 1,
                depth = 1,
                format = format
            };
            
            // Create optimal tiled target image
            ImageCreateInfo imageCreateInfo = new ImageCreateInfo
            {
                imageType = ImageType.Image2D,
                format = format,
                mipLevels = texture.mipLevels,
                arrayLayers = 1,
                samples = SampleCountFlags.Count1,
                tiling = ImageTiling.Optimal,
                sharingMode = SharingMode.Exclusive,
                // Set initial layout of the image to undefined
                initialLayout = ImageLayout.Undefined,
                extent = new Extent3D { width = texture.width, height = texture.height, depth = 1 },
                usage = ImageUsageFlags.TransferDst | ImageUsageFlags.Sampled
            };

            texture.image = new Image(ref imageCreateInfo);
            Device.GetImageMemoryRequirements(texture.image.handle, out var memReqs);

            VkMemoryAllocateInfo memAllocInfo = VkMemoryAllocateInfo.New();
            memAllocInfo.allocationSize = memReqs.size;
            memAllocInfo.memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

            ulong totalBytes = memAllocInfo.allocationSize;

            texture.deviceMemory = Device.AllocateMemory(ref memAllocInfo);
            Device.BindImageMemory(texture.image.handle, texture.deviceMemory, 0);

            { 
                // Create a host-visible staging buffer that contains the raw image data
                VkBuffer stagingBuffer;
                VkDeviceMemory stagingMemory;

                VkBufferCreateInfo bufferCreateInfo = VkBufferCreateInfo.New();
                bufferCreateInfo.size = (ulong)totalBytes;
                // This buffer is used as a transfer source for the buffer copy
                bufferCreateInfo.usage = VkBufferUsageFlags.TransferSrc;
                bufferCreateInfo.sharingMode = VkSharingMode.Exclusive;

                stagingBuffer = Device.CreateBuffer(ref bufferCreateInfo);

                // Get memory requirements for the staging buffer (alignment, memory type bits)
                Device.GetBufferMemoryRequirements(stagingBuffer, out memReqs);

                memAllocInfo.allocationSize = memReqs.size;
                // Get memory type index for a host visible buffer
                memAllocInfo.memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);

                stagingMemory = Device.AllocateMemory(ref memAllocInfo);
                Device.BindBufferMemory(stagingBuffer, stagingMemory, 0);

                // Copy texture data into staging buffer
                IntPtr data = Device.MapMemory(stagingMemory, 0, memReqs.size, 0);
                Unsafe.CopyBlock((void*)data, tex2DDataPtr, (uint)totalBytes);
                Device.UnmapMemory(stagingMemory);

                // Setup buffer copy regions for each mip level
                NativeList<VkBufferImageCopy> bufferCopyRegions = new NativeList<VkBufferImageCopy>();

                uint offset = 0;
                {
                    VkBufferImageCopy bufferCopyRegion = new VkBufferImageCopy();
                    bufferCopyRegion.imageSubresource.aspectMask = VkImageAspectFlags.Color;
                    bufferCopyRegion.imageSubresource.mipLevel = 0;
                    bufferCopyRegion.imageSubresource.baseArrayLayer = 0;
                    bufferCopyRegion.imageSubresource.layerCount = 1;
                    bufferCopyRegion.imageExtent.width = (uint)w;// tex2D.Faces[0].Mipmaps[i].Width;
                    bufferCopyRegion.imageExtent.height = (uint)h;// tex2D.Faces[0].Mipmaps[i].Height;
                    bufferCopyRegion.imageExtent.depth = 1;
                    bufferCopyRegion.bufferOffset = offset;
                    bufferCopyRegions.Add(bufferCopyRegion);
                }

                VkCommandBuffer copyCmd = Device.CreateCommandBuffer(VkCommandBufferLevel.Primary, true);
                // The sub resource range describes the regions of the image we will be transition
                VkImageSubresourceRange subresourceRange = new VkImageSubresourceRange
                {
                    aspectMask = VkImageAspectFlags.Color,
                    baseMipLevel = 0,
                    levelCount = (uint)texture.mipLevels,
                    layerCount = 1
                };

                VulkanUtil.SetImageLayout(
                    copyCmd,
                    texture.image.handle,
                     VkImageAspectFlags.Color,
                     VkImageLayout.Undefined,
                     VkImageLayout.TransferDstOptimal,
                    subresourceRange);

                vkCmdCopyBufferToImage(
                    copyCmd,
                    stagingBuffer,
                    texture.image.handle,
                     VkImageLayout.TransferDstOptimal,
                    bufferCopyRegions.Count,
                    bufferCopyRegions.Data);

                // Change texture image layout to shader read after all mip levels have been copied
                texture.imageLayout = ImageLayout.ShaderReadOnlyOptimal;
                VulkanUtil.SetImageLayout(
                    copyCmd,
                    texture.image.handle,
                    VkImageAspectFlags.Color,
                    VkImageLayout.TransferDstOptimal,
                    (VkImageLayout)texture.imageLayout,
                    subresourceRange);

                Device.FlushCommandBuffer(copyCmd, Graphics.GraphicsQueue, true);
                Device.FreeMemory(stagingMemory);
                Device.DestroyBuffer(stagingBuffer);
            }

            SamplerCreateInfo sampler = new SamplerCreateInfo
            {
                magFilter = Filter.Linear,
                minFilter = Filter.Linear,
                mipmapMode = SamplerMipmapMode.Linear,
                addressModeU = SamplerAddressMode.Repeat,
                addressModeV = SamplerAddressMode.Repeat,
                addressModeW = SamplerAddressMode.Repeat,
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

            // Create image view
            // Textures are not directly accessed by the shaders and
            // are abstracted by image views containing additional
            // information and sub resource ranges
            ImageViewCreateInfo viewCreateInfo = new ImageViewCreateInfo
            {
                viewType = ImageViewType.Image2D,
                format = format,
                components = new ComponentMapping(ComponentSwizzle.R, ComponentSwizzle.G, ComponentSwizzle.B, ComponentSwizzle.A)
            };
            // The subresource range describes the set of mip levels (and array layers) that can be accessed through this image view
            // It's possible to create multiple image views for a single image referring to different (and/or overlapping) ranges of the image
            viewCreateInfo.subresourceRange.aspectMask = VkImageAspectFlags.Color;
            viewCreateInfo.subresourceRange.baseMipLevel = 0;
            viewCreateInfo.subresourceRange.baseArrayLayer = 0;
            viewCreateInfo.subresourceRange.layerCount = 1;
            // Linear tiling usually won't support mip maps
            // Only set mip map count if optimal tiling is used
            viewCreateInfo.subresourceRange.levelCount = (uint)texture.mipLevels;
            // The view will be based on the texture's image
            viewCreateInfo.image = texture.image;
            texture.imageView = new ImageView(ref viewCreateInfo);
            texture.UpdateDescriptor();
            return texture;
        }

        public unsafe void SetImage2D(ImageData tex2D, bool forceLinear = false)
        {
            width = tex2D.Width;
            height = tex2D.Height;

            if (height == 0)
                height = width;

            mipLevels = (uint)tex2D.Mipmaps.Length;

            // Get device properites for the requested texture format
            //VkFormatProperties formatProperties;
            //vkGetPhysicalDeviceFormatProperties(Device.PhysicalDevice, (VkFormat)format,
            //    out formatProperties);

            // Only use linear tiling if requested (and supported by the device)
            // Support for linear tiling is mostly limited, so prefer to use
            // optimal tiling instead
            // On most implementations linear tiling will only support a very
            // limited amount of formats and features (mip maps, cubemaps, arrays, etc.)
            bool useStaging = !forceLinear;

            VkMemoryAllocateInfo memAllocInfo = VkMemoryAllocateInfo.New();
            VkMemoryRequirements memReqs;

            // Use a separate command buffer for texture loading
            VkCommandBuffer copyCmd = Device.CreateCommandBuffer(VkCommandBufferLevel.Primary, true);

            if (useStaging)
            {
                // Create a host-visible staging buffer that contains the raw image data
                VkBuffer stagingBuffer;
                VkDeviceMemory stagingMemory;

                VkBufferCreateInfo bufferCreateInfo = VkBufferCreateInfo.New();
                bufferCreateInfo.size = tex2D.GetTotalSize();
                // This buffer is used as a transfer source for the buffer copy
                bufferCreateInfo.usage = VkBufferUsageFlags.TransferSrc;
                bufferCreateInfo.sharingMode = VkSharingMode.Exclusive;

                stagingBuffer = Device.CreateBuffer(ref bufferCreateInfo);

                // Get memory requirements for the staging buffer (alignment, memory type bits)
                Device.GetBufferMemoryRequirements(stagingBuffer, out memReqs);

                memAllocInfo.allocationSize = memReqs.size;
                // Get memory type index for a host visible buffer
                memAllocInfo.memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);

                stagingMemory = Device.AllocateMemory(ref memAllocInfo);

                Device.BindBufferMemory(stagingBuffer, stagingMemory, 0);

                // Copy texture data into staging buffer
                IntPtr data = Device.MapMemory( stagingMemory, 0, memReqs.size, 0);
                byte[] pixelData = tex2D.GetAllTextureData();
                fixed (byte* pixelDataPtr = &pixelData[0])
                {
                    Unsafe.CopyBlock((void*)data, pixelDataPtr, (uint)pixelData.Length);
                }
                Device.UnmapMemory(stagingMemory);

                // Setup buffer copy regions for each mip level
                NativeList<VkBufferImageCopy> bufferCopyRegions = new NativeList<VkBufferImageCopy>();
                uint offset = 0;

                for (uint i = 0; i < mipLevels; i++)
                {
                    VkBufferImageCopy bufferCopyRegion = new VkBufferImageCopy();
                    bufferCopyRegion.imageSubresource.aspectMask = VkImageAspectFlags.Color;
                    bufferCopyRegion.imageSubresource.mipLevel = i;
                    bufferCopyRegion.imageSubresource.baseArrayLayer = 0;
                    bufferCopyRegion.imageSubresource.layerCount = 1;
                    bufferCopyRegion.imageExtent.width = tex2D.Mipmaps[i].Width;
                    bufferCopyRegion.imageExtent.height = tex2D.Mipmaps[i].Height;
                    bufferCopyRegion.imageExtent.depth = 1;
                    bufferCopyRegion.bufferOffset = offset;

                    bufferCopyRegions.Add(bufferCopyRegion);

                    offset += tex2D.Mipmaps[i].SizeInBytes;
                }

                // Create optimal tiled target image
                ImageCreateInfo imageCreateInfo = new ImageCreateInfo
                {
                    imageType = ImageType.Image2D,
                    format = format,
                    mipLevels = mipLevels,
                    arrayLayers = 1,
                    samples = SampleCountFlags.Count1,
                    tiling = ImageTiling.Optimal,
                    sharingMode = SharingMode.Exclusive,
                    initialLayout = ImageLayout.Undefined,
                    extent = new Extent3D { width = width, height = height, depth = 1 },
                    usage = imageUsageFlags
                };

                // Ensure that the TRANSFER_DST bit is set for staging
                if ((imageCreateInfo.usage & ImageUsageFlags.TransferDst) == 0)
                {
                    imageCreateInfo.usage |= ImageUsageFlags.TransferDst;
                }

                image = new Image(ref imageCreateInfo);

                vkGetImageMemoryRequirements(Device.LogicalDevice, image.handle, &memReqs);

                memAllocInfo.allocationSize = memReqs.size;

                memAllocInfo.memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);
                VulkanUtil.CheckResult(vkAllocateMemory(Device.LogicalDevice, &memAllocInfo, null, out deviceMemory));
                VulkanUtil.CheckResult(vkBindImageMemory(Device.LogicalDevice, image.handle, deviceMemory, 0));

                VkImageSubresourceRange subresourceRange = new VkImageSubresourceRange();
                subresourceRange.aspectMask = VkImageAspectFlags.Color;
                subresourceRange.baseMipLevel = 0;
                subresourceRange.levelCount = (uint)mipLevels;
                subresourceRange.layerCount = 1;

                // Image barrier for optimal image (target)
                // Optimal image will be used as destination for the copy
                VulkanUtil.SetImageLayout(
                    copyCmd,
                    image.handle,
                    VkImageAspectFlags.Color,
                    VkImageLayout.Undefined,
                    VkImageLayout.TransferDstOptimal,
                    subresourceRange);

                // Copy mip levels from staging buffer
                vkCmdCopyBufferToImage(
                    copyCmd,
                    stagingBuffer,
                    image.handle,
                    VkImageLayout.TransferDstOptimal,
                    bufferCopyRegions.Count,
                    bufferCopyRegions.Data);

                // Change texture image layout to shader read after all mip levels have been copied
                //this.imageLayout = imageLayout;
                VulkanUtil.SetImageLayout(
                    copyCmd,
                    image.handle,
                    VkImageAspectFlags.Color,
                    VkImageLayout.TransferDstOptimal,
                    (VkImageLayout)imageLayout,
                    subresourceRange);

                Device.FlushCommandBuffer(copyCmd, Graphics.GraphicsQueue);

                // Clean up staging resources
                Device.FreeMemory(stagingMemory);
                Device.DestroyBuffer(stagingBuffer);
            }
            else
            {
                throw new NotImplementedException();
            }

            // Create a defaultsampler
            SamplerCreateInfo samplerCreateInfo = new SamplerCreateInfo
            {
                magFilter = Filter.Linear,
                minFilter = Filter.Linear,
                mipmapMode = SamplerMipmapMode.Linear,
                addressModeU = SamplerAddressMode.Repeat,
                addressModeV = SamplerAddressMode.Repeat,
                addressModeW = SamplerAddressMode.Repeat,
                mipLodBias = 0.0f,
                compareOp = CompareOp.Never,
                minLod = 0.0f,
                // Max level-of-detail should match mip level count
                maxLod = (useStaging) ? (float)mipLevels : 0.0f,
                // Enable anisotropic filtering
                maxAnisotropy = 8,
                anisotropyEnable = true,
                borderColor = BorderColor.FloatOpaqueWhite
            };
            sampler = new Sampler(ref samplerCreateInfo);

            // Create image view
            // Textures are not directly accessed by the shaders and
            // are abstracted by image views containing additional
            // information and sub resource ranges
            ImageViewCreateInfo viewCreateInfo = new ImageViewCreateInfo
            {
                viewType = ImageViewType.Image2D,
                format = format,
                components = new ComponentMapping { r = ComponentSwizzle.R, g = ComponentSwizzle.G, b = ComponentSwizzle.B, a = ComponentSwizzle.A },
                subresourceRange = new VkImageSubresourceRange { aspectMask = VkImageAspectFlags.Color, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1 }
            };
            // Linear tiling usually won't support mip maps
            // Only set mip map count if optimal tiling is used
            viewCreateInfo.subresourceRange.levelCount = (useStaging) ? (uint)mipLevels : 1;
            viewCreateInfo.image = image;
            imageView = new ImageView(ref viewCreateInfo);

            // Update descriptor image info member that can be used for setting up descriptor sets
            UpdateDescriptor();
        }
    }
}
