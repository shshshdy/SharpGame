using System;
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
                DeviceBuffer stagingBuffer = DeviceBuffer.CreateStagingBuffer(totalBytes, tex2DDataPtr);
                
                BufferImageCopy bufferCopyRegion = new BufferImageCopy();
                bufferCopyRegion.imageSubresource.aspectMask = ImageAspectFlags.Color;
                bufferCopyRegion.imageSubresource.mipLevel = 0;
                bufferCopyRegion.imageSubresource.baseArrayLayer = 0;
                bufferCopyRegion.imageSubresource.layerCount = 1;
                bufferCopyRegion.imageExtent.width = (uint)w;
                bufferCopyRegion.imageExtent.height = (uint)h;
                bufferCopyRegion.imageExtent.depth = 1;
                bufferCopyRegion.bufferOffset = 0;
                   

                CommandBuffer copyCmd = Graphics.CreateCommandBuffer(CommandBufferLevel.Primary, true);
                // The sub resource range describes the regions of the image we will be transition
                ImageSubresourceRange subresourceRange = new ImageSubresourceRange
                {
                    aspectMask = ImageAspectFlags.Color,
                    baseMipLevel = 0,
                    levelCount = (uint)texture.mipLevels,
                    layerCount = 1
                };

                copyCmd.SetImageLayout(
                    texture.image,
                     ImageAspectFlags.Color,
                     ImageLayout.Undefined,
                     ImageLayout.TransferDstOptimal,
                    subresourceRange);

                copyCmd.CopyBufferToImage(                    
                    stagingBuffer,
                    texture.image,
                     ImageLayout.TransferDstOptimal,
                    ref bufferCopyRegion);

                // Change texture image layout to shader read after all mip levels have been copied
                texture.imageLayout = ImageLayout.ShaderReadOnlyOptimal;
                copyCmd.SetImageLayout(                    
                    texture.image,
                    ImageAspectFlags.Color,
                    ImageLayout.TransferDstOptimal,
                    texture.imageLayout,
                    subresourceRange);

                Graphics.FlushCommandBuffer(copyCmd, Graphics.GraphicsQueue, true);

                stagingBuffer.Dispose();
            }

            texture.imageView = ImageView.Create(texture.image, ImageViewType.Image2D, format, ImageAspectFlags.Color, 0, texture.mipLevels);
            texture.sampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.Repeat, Device.Features.samplerAnisotropy == 1);   
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
            
            DeviceBuffer stagingBuffer;
            byte[] pixelData = tex2D.GetAllTextureData();
            fixed (byte* pixelDataPtr = &pixelData[0])
            {
                stagingBuffer = DeviceBuffer.CreateStagingBuffer(tex2D.GetTotalSize(), pixelDataPtr);
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

            Device.GetImageMemoryRequirements(image.handle, out var memReqs);

            VkMemoryAllocateInfo memAllocInfo = VkMemoryAllocateInfo.New();
            memAllocInfo.allocationSize = memReqs.size;
            memAllocInfo.memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

            deviceMemory = Device.AllocateMemory(ref memAllocInfo);
            Device.BindImageMemory(image.handle, deviceMemory, 0);

            // Setup buffer copy regions for each mip level
            Span<BufferImageCopy> bufferCopyRegions = stackalloc BufferImageCopy[(int)(mipLevels)];
            uint offset = 0;
            int index = 0;
            for (uint i = 0; i < mipLevels; i++)
            {
                BufferImageCopy bufferCopyRegion = new BufferImageCopy();
                bufferCopyRegion.imageSubresource.aspectMask = ImageAspectFlags.Color;
                bufferCopyRegion.imageSubresource.mipLevel = i;
                bufferCopyRegion.imageSubresource.baseArrayLayer = 0;
                bufferCopyRegion.imageSubresource.layerCount = 1;
                bufferCopyRegion.imageExtent.width = tex2D.Mipmaps[i].Width;
                bufferCopyRegion.imageExtent.height = tex2D.Mipmaps[i].Height;
                bufferCopyRegion.imageExtent.depth = 1;
                bufferCopyRegion.bufferOffset = offset;

                bufferCopyRegions[index++] = bufferCopyRegion;

                offset += tex2D.Mipmaps[i].SizeInBytes;
            }

            ImageSubresourceRange subresourceRange = new ImageSubresourceRange();
            subresourceRange.aspectMask = ImageAspectFlags.Color;
            subresourceRange.baseMipLevel = 0;
            subresourceRange.levelCount = (uint)mipLevels;
            subresourceRange.layerCount = 1;

            // Use a separate command buffer for texture loading
            CommandBuffer copyCmd = Graphics.CreateCommandBuffer(CommandBufferLevel.Primary, true);

            // Image barrier for optimal image (target)
            // Optimal image will be used as destination for the copy
            copyCmd.SetImageLayout(
                image,
                ImageAspectFlags.Color,
                ImageLayout.Undefined,
                ImageLayout.TransferDstOptimal,
                subresourceRange);

            // Copy mip levels from staging buffer
            copyCmd.CopyBufferToImage(
                stagingBuffer,
                image,
                ImageLayout.TransferDstOptimal,
                bufferCopyRegions);

            // Change texture image layout to shader read after all mip levels have been copied
            //this.imageLayout = imageLayout;
            copyCmd.SetImageLayout(
                image,
                ImageAspectFlags.Color,
                ImageLayout.TransferDstOptimal,
                imageLayout,
                subresourceRange);

            Graphics.FlushCommandBuffer(copyCmd, Graphics.GraphicsQueue);

            // Clean up staging resources
            stagingBuffer.Dispose();

            sampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.Repeat, Device.Features.samplerAnisotropy == 1);

            ImageViewCreateInfo viewCreateInfo = new ImageViewCreateInfo
            {
                viewType = ImageViewType.Image2D,
                format = format,
                components = new ComponentMapping { r = ComponentSwizzle.R, g = ComponentSwizzle.G, b = ComponentSwizzle.B, a = ComponentSwizzle.A },
                subresourceRange = new ImageSubresourceRange { aspectMask = ImageAspectFlags.Color, baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1 }
            };

            // Linear tiling usually won't support mip maps
            // Only set mip map count if optimal tiling is used
            viewCreateInfo.subresourceRange.levelCount = (uint)mipLevels;
            viewCreateInfo.image = image;
            imageView = new ImageView(ref viewCreateInfo);

            // Update descriptor image info member that can be used for setting up descriptor sets
            UpdateDescriptor();
        }
    }
}
