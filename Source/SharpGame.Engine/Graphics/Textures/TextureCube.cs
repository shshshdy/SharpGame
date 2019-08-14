﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using static Vulkan.VulkanNative;
    public unsafe class TextureCube : Texture
    {
        public static TextureCube LoadFromFile(string filename, Format format)
        {
            var tex = new TextureCube();
            tex.LoadFromFile(filename, format, false);
            return tex;
        }

        public void LoadFromFile(string filename, Format format, bool forceLinearTiling)
        {
            KtxFile texCube;
            using (var fs = FileSystem.Instance.GetFile(filename))
            {
                texCube = KtxFile.Load(fs, readKeyValuePairs: false);
            }

            width = (int)texCube.Header.PixelWidth;
            height = (int)texCube.Header.PixelHeight;
            mipLevels = (int)texCube.Header.NumberOfMipmapLevels;

            VkMemoryAllocateInfo memAllocInfo = VkMemoryAllocateInfo.New();
            VkMemoryRequirements memReqs;

            // Create a host-visible staging buffer that contains the raw image data
            VkBuffer stagingBuffer;
            VkDeviceMemory stagingMemory;

            VkBufferCreateInfo bufferCreateInfo = VkBufferCreateInfo.New();
            bufferCreateInfo.size = texCube.GetTotalSize();
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
            byte* data = (byte*)Device.MapMemory(stagingMemory, 0, memReqs.size, 0);
            byte[] allTextureData = texCube.GetAllTextureData();
            fixed (byte* texCubeDataPtr = &allTextureData[0])
            {
                Unsafe.CopyBlock(data, texCubeDataPtr, (uint)allTextureData.Length);
            }

            Device.UnmapMemory(stagingMemory);

            // Create optimal tiled target image
            ImageCreateInfo imageCreateInfo = new ImageCreateInfo
            {
                imageType = ImageType.Image2D,
                format = format,
                mipLevels = mipLevels,
                samples = SampleCountFlags.Count1,
                tiling = ImageTiling.Optimal,
                sharingMode = SharingMode.Exclusive,
                initialLayout = ImageLayout.Undefined,
                extent = new Extent3D { width = width, height = height, depth = 1 },
                usage = ImageUsageFlags.TransferDst | ImageUsageFlags.Sampled,
                // Cube faces count as array layers in Vulkan
                arrayLayers = 6,
                // This flag is required for cube map images
                flags = ImageCreateFlags.CubeCompatible
            };

            image = new Image(ref imageCreateInfo);

            vkGetImageMemoryRequirements(Device.LogicalDevice, image.handle, &memReqs);

            memAllocInfo.allocationSize = memReqs.size;
            memAllocInfo.memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

            deviceMemory = Device.AllocateMemory(ref memAllocInfo);
            Device.BindImageMemory(image.handle, deviceMemory, 0);
            VkCommandBuffer copyCmd = Device.CreateCommandBuffer(VkCommandBufferLevel.Primary, true);

            // Setup buffer copy regions for each face including all of it's miplevels
            NativeList<VkBufferImageCopy> bufferCopyRegions = new NativeList<VkBufferImageCopy>();
            uint offset = 0;

            for (uint face = 0; face < 6; face++)
            {
                for (uint level = 0; level < mipLevels; level++)
                {
                    VkBufferImageCopy bufferCopyRegion = new VkBufferImageCopy();
                    bufferCopyRegion.imageSubresource.aspectMask = VkImageAspectFlags.Color;
                    bufferCopyRegion.imageSubresource.mipLevel = level;
                    bufferCopyRegion.imageSubresource.baseArrayLayer = face;
                    bufferCopyRegion.imageSubresource.layerCount = 1;
                    bufferCopyRegion.imageExtent.width = texCube.Faces[face].Mipmaps[level].Width;
                    bufferCopyRegion.imageExtent.height = texCube.Faces[face].Mipmaps[level].Height;
                    bufferCopyRegion.imageExtent.depth = 1;
                    bufferCopyRegion.bufferOffset = offset;

                    bufferCopyRegions.Add(bufferCopyRegion);

                    // Increase offset into staging buffer for next level / face
                    offset += texCube.Faces[face].Mipmaps[level].SizeInBytes;
                }
            }

            // Image barrier for optimal image (target)
            // Set initial layout for all array layers (faces) of the optimal (target) tiled texture
            VkImageSubresourceRange subresourceRange = new VkImageSubresourceRange();
            subresourceRange.aspectMask = VkImageAspectFlags.Color;
            subresourceRange.baseMipLevel = 0;
            subresourceRange.levelCount = (uint)mipLevels;
            subresourceRange.layerCount = 6;

            VulkanUtil.SetImageLayout(
                copyCmd,
                image.handle,
                VkImageAspectFlags.Color,
                VkImageLayout.Undefined,
                VkImageLayout.TransferDstOptimal,
                subresourceRange);

            // Copy the cube map faces from the staging buffer to the optimal tiled image
            vkCmdCopyBufferToImage(
                copyCmd,
                stagingBuffer,
                image.handle,
                VkImageLayout.TransferDstOptimal,
                bufferCopyRegions.Count,
                bufferCopyRegions.Data);

            // Change texture image layout to shader read after all faces have been copied
            imageLayout = ImageLayout.ShaderReadOnlyOptimal;
            VulkanUtil.SetImageLayout(
                copyCmd,
                image.handle,
                VkImageAspectFlags.Color,
                VkImageLayout.TransferDstOptimal,
                (VkImageLayout)imageLayout,
                subresourceRange);

            Device.FlushCommandBuffer(copyCmd, Graphics.GraphicsQueue, true);

            // Create sampler
            SamplerCreateInfo sampler = new SamplerCreateInfo();
            sampler.magFilter = Filter.Linear;
            sampler.minFilter = Filter.Linear;
            sampler.mipmapMode = SamplerMipmapMode.Linear;
            sampler.addressModeU = SamplerAddressMode.ClampToEdge;
            sampler.addressModeV = sampler.addressModeU;
            sampler.addressModeW = sampler.addressModeU;
            sampler.mipLodBias = 0.0f;
            sampler.compareOp = CompareOp.Never;
            sampler.minLod = 0.0f;
            sampler.maxLod = mipLevels;
            sampler.borderColor = BorderColor.FloatOpaqueWhite;
            sampler.maxAnisotropy = 1.0f;
            if (Device.Features.samplerAnisotropy == 1)
            {
                sampler.maxAnisotropy = Device.Properties.limits.maxSamplerAnisotropy;
                sampler.anisotropyEnable = true;
            }

            this.sampler = new Sampler(ref sampler);

            // Create image view
            ImageViewCreateInfo view = new ImageViewCreateInfo();
            // Cube map view type
            view.viewType = ImageViewType.ImageCube;
            view.format = format;
            view.components = new ComponentMapping(ComponentSwizzle.R, ComponentSwizzle.G, ComponentSwizzle.B, ComponentSwizzle.A );
            view.subresourceRange = new VkImageSubresourceRange { aspectMask = VkImageAspectFlags.Color, baseMipLevel = 0, layerCount = 1, baseArrayLayer = 0, levelCount = 1 };
            // 6 array layers (faces)
            view.subresourceRange.layerCount = 6;
            // Set number of mip levels
            view.subresourceRange.levelCount = (uint)mipLevels;
            view.image = image;

            this.view = new ImageView(ref view);

            // Clean up staging resources
            Device.FreeMemory(stagingMemory);
            Device.DestroyBuffer(stagingBuffer);
            UpdateDescriptor();
        }

    }
}
