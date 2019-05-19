using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Vulkan;
using static Vulkan.VulkanNative;

namespace SharpGame
{
    public class Texture : DisposeBase, IBindable
    {
        public VkImageView view;
        public VkImage image;
        public VkSampler sampler;
        public VkDeviceMemory deviceMemory;
        public uint width;
        public uint height;
        public uint mipLevels;
        public VkImageLayout imageLayout;
        public VkDescriptorImageInfo descriptor;


        /** @brief Update image descriptor from current sampler, view and image layout */
        internal void updateDescriptor()
        {
            descriptor.sampler = sampler;
            descriptor.imageView = view;
            descriptor.imageLayout = imageLayout;
        }

        protected override void Destroy()
        {

            vkDestroyImageView(Graphics.device, view, IntPtr.Zero);
            vkDestroyImage(Graphics.device, image, IntPtr.Zero);
            vkDestroySampler(Graphics.device, sampler, IntPtr.Zero);
            vkFreeMemory(Graphics.device, deviceMemory, IntPtr.Zero);

            base.Destroy();
        }

    }

    public unsafe class Texture2D : Texture
    {

        public static Texture Create(uint w, uint h, uint bytesPerPixel, byte* tex2DDataPtr, bool dynamic = false)
        {
            var texture = new Texture2D
            {
                width = w,
                height = h,
                mipLevels = 1
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

                for (uint i = 0; i < texture.mipLevels; i++)
                {
                    VkBufferImageCopy bufferCopyRegion = new VkBufferImageCopy();
                    bufferCopyRegion.imageSubresource.aspectMask = VkImageAspectFlags.Color;
                    bufferCopyRegion.imageSubresource.mipLevel = i;
                    bufferCopyRegion.imageSubresource.baseArrayLayer = 0;
                    bufferCopyRegion.imageSubresource.layerCount = 1;
                    bufferCopyRegion.imageExtent.width = w;// tex2D.Faces[0].Mipmaps[i].Width;
                    bufferCopyRegion.imageExtent.height = h;// tex2D.Faces[0].Mipmaps[i].Height;
                    bufferCopyRegion.imageExtent.depth = 1;
                    bufferCopyRegion.bufferOffset = offset;

                    bufferCopyRegions.Add(bufferCopyRegion);

                    //   offset += tex2D.Faces[0].Mipmaps[i].SizeInBytes;
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
                VkImageSubresourceRange subresourceRange = new VkImageSubresourceRange();
                // Image only contains color data
                subresourceRange.aspectMask = VkImageAspectFlags.Color;
                // Start at first mip level
                subresourceRange.baseMipLevel = 0;
                // We will transition on all mip levels
                subresourceRange.levelCount = texture.mipLevels;
                // The 2D texture only has one layer
                subresourceRange.layerCount = 1;

                // Optimal image will be used as destination for the copy, so we must transfer from our
                // initial undefined image layout to the transfer destination layout
                setImageLayout(
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
                texture.imageLayout = VkImageLayout.ShaderReadOnlyOptimal;
                setImageLayout(
                    copyCmd,
                    texture.image,
                    VkImageAspectFlags.Color,
                    VkImageLayout.TransferDstOptimal,
                    texture.imageLayout,
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
            texture.updateDescriptor();
            return texture;
        }

        // Create an image memory barrier for changing the layout of
        // an image and put it into an active command buffer
        static void setImageLayout(
            VkCommandBuffer cmdBuffer,
            VkImage image,
            VkImageAspectFlags aspectMask,
            VkImageLayout oldImageLayout,
            VkImageLayout newImageLayout,
            VkImageSubresourceRange subresourceRange)
        {
            // Create an image barrier object
            VkImageMemoryBarrier imageMemoryBarrier = Builder.ImageMemoryBarrier(); ;
            imageMemoryBarrier.oldLayout = oldImageLayout;
            imageMemoryBarrier.newLayout = newImageLayout;
            imageMemoryBarrier.image = image;
            imageMemoryBarrier.subresourceRange = subresourceRange;

            // Only sets masks for layouts used in this example
            // For a more complete version that can be used with other layouts see vks::tools::setImageLayout

            // Source layouts (old)
            switch (oldImageLayout)
            {
                case VkImageLayout.Undefined:
                    // Only valid as initial layout, memory contents are not preserved
                    // Can be accessed directly, no source dependency required
                    imageMemoryBarrier.srcAccessMask = 0;
                    break;
                case VkImageLayout.Preinitialized:
                    // Only valid as initial layout for linear images, preserves memory contents
                    // Make sure host writes to the image have been finished
                    imageMemoryBarrier.srcAccessMask = VkAccessFlags.HostWrite;
                    break;
                case VkImageLayout.TransferDstOptimal:
                    // Old layout is transfer destination
                    // Make sure any writes to the image have been finished
                    imageMemoryBarrier.srcAccessMask = VkAccessFlags.TransferWrite;
                    break;
            }

            // Target layouts (new)
            switch (newImageLayout)
            {
                case VkImageLayout.TransferSrcOptimal:
                    // Transfer source (copy, blit)
                    // Make sure any reads from the image have been finished
                    imageMemoryBarrier.dstAccessMask = VkAccessFlags.TransferRead;
                    break;
                case VkImageLayout.TransferDstOptimal:
                    // Transfer destination (copy, blit)
                    // Make sure any writes to the image have been finished
                    imageMemoryBarrier.dstAccessMask = VkAccessFlags.TransferWrite;
                    break;
                case VkImageLayout.ShaderReadOnlyOptimal:
                    // Shader read (sampler, input attachment)
                    imageMemoryBarrier.dstAccessMask = VkAccessFlags.ShaderRead;
                    break;
            }

            // Put barrier on top of pipeline
            VkPipelineStageFlags srcStageFlags = VkPipelineStageFlags.TopOfPipe;
            VkPipelineStageFlags destStageFlags = VkPipelineStageFlags.TopOfPipe;

            // Put barrier inside setup command buffer
            vkCmdPipelineBarrier(
                cmdBuffer,
                srcStageFlags,
                destStageFlags,
                VkDependencyFlags.None,
                0, null,
                0, null,
                1, &imageMemoryBarrier);
        }


        public void loadFromFile(
            string filename,
            VkFormat format,
            VkQueue copyQueue,
            VkImageUsageFlags imageUsageFlags = VkImageUsageFlags.Sampled,
            VkImageLayout imageLayout = VkImageLayout.ShaderReadOnlyOptimal,
            bool forceLinear = false)
        {
            KtxFile tex2D;
            using (var fs = System.IO.File.OpenRead(filename))
            {
                tex2D = KtxFile.Load(fs, false);
            }

            width = tex2D.Header.PixelWidth;
            height = tex2D.Header.PixelHeight;
            if (height == 0) height = width;
            mipLevels = tex2D.Header.NumberOfMipmapLevels;

            // Get device properites for the requested texture format
            VkFormatProperties formatProperties;
            vkGetPhysicalDeviceFormatProperties(Device.PhysicalDevice, format, out formatProperties);

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

                Util.CheckResult(vkCreateBuffer(Device.LogicalDevice, &bufferCreateInfo, null, &stagingBuffer));

                // Get memory requirements for the staging buffer (alignment, memory type bits)
                vkGetBufferMemoryRequirements(Device.LogicalDevice, stagingBuffer, &memReqs);

                memAllocInfo.allocationSize = memReqs.size;
                // Get memory type index for a host visible buffer
                memAllocInfo.memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);

                Util.CheckResult(vkAllocateMemory(Device.LogicalDevice, &memAllocInfo, null, &stagingMemory));
                Util.CheckResult(vkBindBufferMemory(Device.LogicalDevice, stagingBuffer, stagingMemory, 0));

                // Copy texture data into staging buffer
                byte* data;
                Util.CheckResult(vkMapMemory(Device.LogicalDevice, stagingMemory, 0, memReqs.size, 0, (void**)&data));
                byte[] pixelData = tex2D.GetAllTextureData();
                fixed (byte* pixelDataPtr = &pixelData[0])
                {
                    Unsafe.CopyBlock(data, pixelDataPtr, (uint)pixelData.Length);
                }
                vkUnmapMemory(Device.LogicalDevice, stagingMemory);

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
                    bufferCopyRegion.imageExtent.width = tex2D.Faces[0].Mipmaps[i].Width;
                    bufferCopyRegion.imageExtent.height = tex2D.Faces[0].Mipmaps[i].Height;
                    bufferCopyRegion.imageExtent.depth = 1;
                    bufferCopyRegion.bufferOffset = offset;

                    bufferCopyRegions.Add(bufferCopyRegion);

                    offset += tex2D.Faces[0].Mipmaps[i].SizeInBytes;
                }

                // Create optimal tiled target image
                VkImageCreateInfo imageCreateInfo = VkImageCreateInfo.New();
                imageCreateInfo.imageType = VkImageType.Image2D;
                imageCreateInfo.format = format;
                imageCreateInfo.mipLevels = mipLevels;
                imageCreateInfo.arrayLayers = 1;
                imageCreateInfo.samples = VkSampleCountFlags.Count1;
                imageCreateInfo.tiling = VkImageTiling.Optimal;
                imageCreateInfo.sharingMode = VkSharingMode.Exclusive;
                imageCreateInfo.initialLayout = VkImageLayout.Undefined;
                imageCreateInfo.extent = new VkExtent3D { width = width, height = height, depth = 1 };
                imageCreateInfo.usage = imageUsageFlags;
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
                Tools.setImageLayout(
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
                this.imageLayout = imageLayout;
                Tools.setImageLayout(
                    copyCmd,
                    image,
                    VkImageAspectFlags.Color,
                    VkImageLayout.TransferDstOptimal,
                    imageLayout,
                    subresourceRange);

                Device.FlushCommandBuffer(copyCmd, copyQueue);

                // Clean up staging resources
                vkFreeMemory(Device.LogicalDevice, stagingMemory, null);
                vkDestroyBuffer(Device.LogicalDevice, stagingBuffer, null);
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
            viewCreateInfo.format = format;
            viewCreateInfo.components = new VkComponentMapping { r = VkComponentSwizzle.R, g = VkComponentSwizzle.G, b = VkComponentSwizzle.B, a = VkComponentSwizzle.A };
            viewCreateInfo.subresourceRange = new VkImageSubresourceRange { aspectMask = VkImageAspectFlags.Color, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1 };
            // Linear tiling usually won't support mip maps
            // Only set mip map count if optimal tiling is used
            viewCreateInfo.subresourceRange.levelCount = (useStaging) ? mipLevels : 1;
            viewCreateInfo.image = image;
            Util.CheckResult(vkCreateImageView(Device.LogicalDevice, &viewCreateInfo, null, out view));

            // Update descriptor image info member that can be used for setting up descriptor sets
            updateDescriptor();
        }


    }
}
