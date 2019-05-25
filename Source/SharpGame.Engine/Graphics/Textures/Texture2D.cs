using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using global::System.Runtime.CompilerServices;
    using static Vulkan.VulkanNative;

    public unsafe class Texture2D : Texture
    {
        public Texture2D()
        {
        }

        public override bool Load(File stream)
        {
            var tex2D = KtxFile.Load(stream, false);

            //LoadKtxFile(tex2D);
            return true;
        }

        public static Texture Create(uint w, uint h, uint bytesPerPixel, byte* tex2DDataPtr, bool dynamic = false)
        {
            var texture = new Texture2D
            {
                width = w,
                height = h,
                mipLevels = 1,
                depth = 1,
                format = Format.R8g8b8a8Unorm
            };

            uint totalBytes = bytesPerPixel * w * h;

            VkFormat format = VkFormat.R8g8b8a8Unorm;
            VkFormatProperties formatProperties;
            // Get Device properites for the requested texture format
            vkGetPhysicalDeviceFormatProperties(Device.PhysicalDevice, format, &formatProperties);

            uint useStaging = 1;

            VkMemoryAllocateInfo memAllocInfo = VkMemoryAllocateInfo.New();
            VkMemoryRequirements memReqs = new VkMemoryRequirements();

            if (useStaging == 1)
            {
                // Create a host-visible staging buffer that contains the raw image data
                VkBuffer stagingBuffer;
                VkDeviceMemory stagingMemory;

                VkBufferCreateInfo bufferCreateInfo = VkBufferCreateInfo.New();
                bufferCreateInfo.size = totalBytes;
                // This buffer is used as a transfer source for the buffer copy
                bufferCreateInfo.usage = VkBufferUsageFlags.TransferSrc;
                bufferCreateInfo.sharingMode = VkSharingMode.Exclusive;

                Util.CheckResult(vkCreateBuffer(Graphics.device, &bufferCreateInfo, null, &stagingBuffer));

                // Get memory requirements for the staging buffer (alignment, memory type bits)
                vkGetBufferMemoryRequirements(Graphics.device, stagingBuffer, &memReqs);

                memAllocInfo.allocationSize = memReqs.size;
                // Get memory type index for a host visible buffer
                memAllocInfo.memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);

                Util.CheckResult(vkAllocateMemory(Graphics.device, &memAllocInfo, null, &stagingMemory));
                Util.CheckResult(vkBindBufferMemory(Graphics.device, stagingBuffer, stagingMemory, 0));

                // Copy texture data into staging buffer
                byte* data;
                Util.CheckResult(vkMapMemory(Graphics.device, stagingMemory, 0, memReqs.size, 0, (void**)&data));
                Unsafe.CopyBlock(data, tex2DDataPtr, totalBytes);
                vkUnmapMemory(Graphics.device, stagingMemory);

                // Setup buffer copy regions for each mip level
                NativeList<VkBufferImageCopy> bufferCopyRegions = new NativeList<VkBufferImageCopy>();

                uint offset = 0;
                {
                    VkBufferImageCopy bufferCopyRegion = new VkBufferImageCopy();
                    bufferCopyRegion.imageSubresource.aspectMask = VkImageAspectFlags.Color;
                    bufferCopyRegion.imageSubresource.mipLevel = 0;
                    bufferCopyRegion.imageSubresource.baseArrayLayer = 0;
                    bufferCopyRegion.imageSubresource.layerCount = 1;
                    bufferCopyRegion.imageExtent.width = w;// tex2D.Faces[0].Mipmaps[i].Width;
                    bufferCopyRegion.imageExtent.height = h;// tex2D.Faces[0].Mipmaps[i].Height;
                    bufferCopyRegion.imageExtent.depth = 1;
                    bufferCopyRegion.bufferOffset = offset;
                    bufferCopyRegions.Add(bufferCopyRegion);
                }

                // Create optimal tiled target image
                VkImageCreateInfo imageCreateInfo = VkImageCreateInfo.New();
                imageCreateInfo.imageType = VkImageType.Image2D;
                imageCreateInfo.format = format;
                imageCreateInfo.mipLevels = texture.mipLevels;
                imageCreateInfo.arrayLayers = 1;
                imageCreateInfo.samples = VkSampleCountFlags.Count1;
                imageCreateInfo.tiling = VkImageTiling.Optimal;
                imageCreateInfo.sharingMode = VkSharingMode.Exclusive;
                // Set initial layout of the image to undefined
                imageCreateInfo.initialLayout = VkImageLayout.Undefined;
                imageCreateInfo.extent = new VkExtent3D { width = texture.width, height = texture.height, depth = 1 };
                imageCreateInfo.usage = VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled;

                Util.CheckResult(vkCreateImage(Graphics.device, &imageCreateInfo, null, out texture.image));

                vkGetImageMemoryRequirements(Graphics.device, texture.image, &memReqs);

                memAllocInfo.allocationSize = memReqs.size;
                memAllocInfo.memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

                Util.CheckResult(vkAllocateMemory(Graphics.device, &memAllocInfo, null, out texture.deviceMemory));
                Util.CheckResult(vkBindImageMemory(Graphics.device, texture.image, texture.deviceMemory, 0));

                VkCommandBuffer copyCmd = Device.CreateCommandBuffer(VkCommandBufferLevel.Primary, true);

                // Image barrier for optimal image

                // The sub resource range describes the regions of the image we will be transition
                VkImageSubresourceRange subresourceRange = new VkImageSubresourceRange
                {
                    // Image only contains color data
                    aspectMask = VkImageAspectFlags.Color,
                    // Start at first mip level
                    baseMipLevel = 0,
                    // We will transition on all mip levels
                    levelCount = texture.mipLevels,
                    // The 2D texture only has one layer
                    layerCount = 1
                };

                // Optimal image will be used as destination for the copy, so we must transfer from our
                // initial undefined image layout to the transfer destination layout
                Tools.SetImageLayout(
                    copyCmd,
                    texture.image,
                     VkImageAspectFlags.Color,
                     VkImageLayout.Undefined,
                     VkImageLayout.TransferDstOptimal,
                    subresourceRange);

                // Copy mip levels from staging buffer
                vkCmdCopyBufferToImage(
                    copyCmd,
                    stagingBuffer,
                    texture.image,
                     VkImageLayout.TransferDstOptimal,
                    bufferCopyRegions.Count,
                    bufferCopyRegions.Data);

                // Change texture image layout to shader read after all mip levels have been copied
                texture.imageLayout = ImageLayout.ShaderReadOnlyOptimal;
                Tools.SetImageLayout(
                    copyCmd,
                    texture.image,
                    VkImageAspectFlags.Color,
                    VkImageLayout.TransferDstOptimal,
                    (VkImageLayout)texture.imageLayout,
                    subresourceRange);

                Device.FlushCommandBuffer(copyCmd, Graphics.queue, true);

                // Clean up staging resources
                vkFreeMemory(Graphics.device, stagingMemory, null);
                vkDestroyBuffer(Graphics.device, stagingBuffer, null);
            }

            VkSamplerCreateInfo sampler = VkSamplerCreateInfo.New();
            sampler.magFilter = VkFilter.Linear;
            sampler.minFilter = VkFilter.Linear;
            sampler.mipmapMode = VkSamplerMipmapMode.Linear;
            sampler.addressModeU = VkSamplerAddressMode.ClampToEdge;
            sampler.addressModeV = VkSamplerAddressMode.ClampToEdge;
            sampler.addressModeW = VkSamplerAddressMode.ClampToEdge;
            sampler.mipLodBias = 0.0f;
            sampler.compareOp = VkCompareOp.Never;
            sampler.minLod = 0.0f;
            // Set max level-of-detail to mip level count of the texture
            sampler.maxLod = (useStaging == 1) ? (float)texture.mipLevels : 0.0f;
            // Enable anisotropic filtering
            // This feature is optional, so we must check if it's supported on the Device
            if (Device.Features.samplerAnisotropy == 1)
            {
                // Use max. level of anisotropy for this example
                sampler.maxAnisotropy = Device.Properties.limits.maxSamplerAnisotropy;
                sampler.anisotropyEnable = True;
            }
            else
            {
                // The Device does not support anisotropic filtering
                sampler.maxAnisotropy = 1.0f;
                sampler.anisotropyEnable = False;
            }
            sampler.borderColor = VkBorderColor.FloatOpaqueWhite;
            Util.CheckResult(vkCreateSampler(Graphics.device, ref sampler, null, out texture.sampler));

            // Create image view
            // Textures are not directly accessed by the shaders and
            // are abstracted by image views containing additional
            // information and sub resource ranges
            VkImageViewCreateInfo view = VkImageViewCreateInfo.New();
            view.viewType = VkImageViewType.Image2D;
            view.format = format;
            view.components = new VkComponentMapping { r = VkComponentSwizzle.R, g = VkComponentSwizzle.G, b = VkComponentSwizzle.B, a = VkComponentSwizzle.A };
            // The subresource range describes the set of mip levels (and array layers) that can be accessed through this image view
            // It's possible to create multiple image views for a single image referring to different (and/or overlapping) ranges of the image
            view.subresourceRange.aspectMask = VkImageAspectFlags.Color;
            view.subresourceRange.baseMipLevel = 0;
            view.subresourceRange.baseArrayLayer = 0;
            view.subresourceRange.layerCount = 1;
            // Linear tiling usually won't support mip maps
            // Only set mip map count if optimal tiling is used
            view.subresourceRange.levelCount = (useStaging == 1) ? texture.mipLevels : 1;
            // The view will be based on the texture's image
            view.image = texture.image;
            Util.CheckResult(vkCreateImageView(Graphics.device, &view, null, out texture.view));
            texture.UpdateDescriptor();
            return texture;
        }


        public void LoadFromFile(
            string filename,
            Format format,
            ImageUsageFlags imageUsageFlags = ImageUsageFlags.Sampled,
            ImageLayout imageLayout = ImageLayout.ShaderReadOnlyOptimal,
            bool forceLinear = false)
        {
            KtxFile tex2D;

            this.format = format;
            this.imageUsageFlags = imageUsageFlags;
            this.imageLayout = imageLayout;

            using (var file = FileSystem.Instance.OpenFile(filename))
            {
                tex2D = KtxFile.Load(file, false);
                tex2D.Faces[0].Width = tex2D.Header.PixelWidth;
                tex2D.Faces[0].Height = tex2D.Header.PixelWidth;
                LoadKtxFile(tex2D.Faces[0], forceLinear);
            }

        }

        void LoadKtxFile(KtxFace tex2D, bool forceLinear = false)
        {
            width = tex2D.Width;
            height = tex2D.Height;

            if (height == 0)
                height = width;

            mipLevels = (uint)tex2D.Mipmaps.Length;

            // Get device properites for the requested texture format
            VkFormatProperties formatProperties;
            vkGetPhysicalDeviceFormatProperties(Device.PhysicalDevice, (VkFormat)format,
                out formatProperties);

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
                void* data = Device.MapMemory( stagingMemory, 0, memReqs.size, 0);
                byte[] pixelData = tex2D.GetAllTextureData();
                fixed (byte* pixelDataPtr = &pixelData[0])
                {
                    Unsafe.CopyBlock(data, pixelDataPtr, (uint)pixelData.Length);
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
                VkImageCreateInfo imageCreateInfo = VkImageCreateInfo.New();
                imageCreateInfo.imageType = VkImageType.Image2D;
                imageCreateInfo.format = (VkFormat)format;
                imageCreateInfo.mipLevels = mipLevels;
                imageCreateInfo.arrayLayers = 1;
                imageCreateInfo.samples = VkSampleCountFlags.Count1;
                imageCreateInfo.tiling = VkImageTiling.Optimal;
                imageCreateInfo.sharingMode = VkSharingMode.Exclusive;
                imageCreateInfo.initialLayout = VkImageLayout.Undefined;
                imageCreateInfo.extent = new VkExtent3D { width = width, height = height, depth = 1 };
                imageCreateInfo.usage = (VkImageUsageFlags)imageUsageFlags;

                // Ensure that the TRANSFER_DST bit is set for staging
                if ((imageCreateInfo.usage & VkImageUsageFlags.TransferDst) == 0)
                {
                    imageCreateInfo.usage |= VkImageUsageFlags.TransferDst;
                }

                Util.CheckResult(vkCreateImage(Device.LogicalDevice, &imageCreateInfo, null, out image));

                vkGetImageMemoryRequirements(Device.LogicalDevice, image, &memReqs);

                memAllocInfo.allocationSize = memReqs.size;

                memAllocInfo.memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);
                Util.CheckResult(vkAllocateMemory(Device.LogicalDevice, &memAllocInfo, null, out deviceMemory));
                Util.CheckResult(vkBindImageMemory(Device.LogicalDevice, image, deviceMemory, 0));

                VkImageSubresourceRange subresourceRange = new VkImageSubresourceRange();
                subresourceRange.aspectMask = VkImageAspectFlags.Color;
                subresourceRange.baseMipLevel = 0;
                subresourceRange.levelCount = mipLevels;
                subresourceRange.layerCount = 1;

                // Image barrier for optimal image (target)
                // Optimal image will be used as destination for the copy
                Tools.SetImageLayout(
                    copyCmd,
                    image,
                    VkImageAspectFlags.Color,
                    VkImageLayout.Undefined,
                    VkImageLayout.TransferDstOptimal,
                    subresourceRange);

                // Copy mip levels from staging buffer
                vkCmdCopyBufferToImage(
                    copyCmd,
                    stagingBuffer,
                    image,
                    VkImageLayout.TransferDstOptimal,
                    bufferCopyRegions.Count,
                    bufferCopyRegions.Data);

                // Change texture image layout to shader read after all mip levels have been copied
                //this.imageLayout = imageLayout;
                Tools.SetImageLayout(
                    copyCmd,
                    image,
                    VkImageAspectFlags.Color,
                    VkImageLayout.TransferDstOptimal,
                    (VkImageLayout)imageLayout,
                    subresourceRange);

                Device.FlushCommandBuffer(copyCmd, Graphics.queue);

                // Clean up staging resources
                Device.FreeMemory(stagingMemory);
                Device.DestroyBuffer(stagingBuffer);
            }
            else
            {
                throw new NotImplementedException();
            }

            // Create a defaultsampler
            VkSamplerCreateInfo samplerCreateInfo = VkSamplerCreateInfo.New();
            samplerCreateInfo.magFilter = VkFilter.Linear;
            samplerCreateInfo.minFilter = VkFilter.Linear;
            samplerCreateInfo.mipmapMode = VkSamplerMipmapMode.Linear;
            samplerCreateInfo.addressModeU = VkSamplerAddressMode.Repeat;
            samplerCreateInfo.addressModeV = VkSamplerAddressMode.Repeat;
            samplerCreateInfo.addressModeW = VkSamplerAddressMode.Repeat;
            samplerCreateInfo.mipLodBias = 0.0f;
            samplerCreateInfo.compareOp = VkCompareOp.Never;
            samplerCreateInfo.minLod = 0.0f;
            // Max level-of-detail should match mip level count
            samplerCreateInfo.maxLod = (useStaging) ? (float)mipLevels : 0.0f;
            // Enable anisotropic filtering
            samplerCreateInfo.maxAnisotropy = 8;
            samplerCreateInfo.anisotropyEnable = True;
            samplerCreateInfo.borderColor = VkBorderColor.FloatOpaqueWhite;
            Util.CheckResult(vkCreateSampler(Device.LogicalDevice, &samplerCreateInfo, null, out sampler));

            // Create image view
            // Textures are not directly accessed by the shaders and
            // are abstracted by image views containing additional
            // information and sub resource ranges
            VkImageViewCreateInfo viewCreateInfo = VkImageViewCreateInfo.New();
            viewCreateInfo.viewType = VkImageViewType.Image2D;
            viewCreateInfo.format = (VkFormat)format;
            viewCreateInfo.components = new VkComponentMapping { r = VkComponentSwizzle.R, g = VkComponentSwizzle.G, b = VkComponentSwizzle.B, a = VkComponentSwizzle.A };
            viewCreateInfo.subresourceRange = new VkImageSubresourceRange { aspectMask = VkImageAspectFlags.Color, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1 };
            // Linear tiling usually won't support mip maps
            // Only set mip map count if optimal tiling is used
            viewCreateInfo.subresourceRange.levelCount = (useStaging) ? mipLevels : 1;
            viewCreateInfo.image = image;
            Util.CheckResult(vkCreateImageView(Device.LogicalDevice, &viewCreateInfo, null, out view));

            // Update descriptor image info member that can be used for setting up descriptor sets
            UpdateDescriptor();
        }
    }
}
