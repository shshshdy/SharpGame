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

            texture.image = Image.Create(w, h, ImageCreateFlags.None, 1, 1, format, SampleCountFlags.Count1, ImageUsageFlags.TransferDst | ImageUsageFlags.Sampled);

            ulong totalBytes = texture.image.allocationSize;

            using (DeviceBuffer stagingBuffer = DeviceBuffer.CreateStagingBuffer(totalBytes, tex2DDataPtr))
            {
                BufferImageCopy bufferCopyRegion = new BufferImageCopy
                {
                    imageSubresource = new ImageSubresourceLayers
                    {
                        aspectMask = ImageAspectFlags.Color,
                        mipLevel = 0,
                        baseArrayLayer = 0,
                        layerCount = 1,
                    },

                    imageExtent = new Extent3D(w, h, 1),
                    bufferOffset = 0
                };

                CommandBuffer copyCmd = Graphics.CreateCommandBuffer(CommandBufferLevel.Primary, true);
                // The sub resource range describes the regions of the image we will be transition
                ImageSubresourceRange subresourceRange = new ImageSubresourceRange(ImageAspectFlags.Color, 0, 1, 0, 1);

                copyCmd.SetImageLayout(texture.image, ImageAspectFlags.Color, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, subresourceRange);
                copyCmd.CopyBufferToImage(stagingBuffer, texture.image, ImageLayout.TransferDstOptimal, ref bufferCopyRegion);
                copyCmd.SetImageLayout(texture.image, ImageAspectFlags.Color, ImageLayout.TransferDstOptimal, texture.imageLayout, subresourceRange);

                Graphics.FlushCommandBuffer(copyCmd, Graphics.GraphicsQueue, true);

                // Change texture image layout to shader read after all mip levels have been copied
                texture.imageLayout = ImageLayout.ShaderReadOnlyOptimal;               
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
            
            image = Image.Create(width, height, ImageCreateFlags.None, 1, mipLevels, format, SampleCountFlags.Count1, ImageUsageFlags.TransferDst | imageUsageFlags);

            // Setup buffer copy regions for each mip level
            Span<BufferImageCopy> bufferCopyRegions = stackalloc BufferImageCopy[(int)(mipLevels)];
            uint offset = 0;
            int index = 0;
            for (uint i = 0; i < mipLevels; i++)
            {
                BufferImageCopy bufferCopyRegion = new BufferImageCopy()
                {
                    imageSubresource = new ImageSubresourceLayers
                    {
                        aspectMask = ImageAspectFlags.Color,
                        mipLevel = i,
                        baseArrayLayer = 0,
                        layerCount = 1
                    },

                    imageExtent = new Extent3D(tex2D.Mipmaps[i].Width, tex2D.Mipmaps[i].Height, 1),
                    bufferOffset = offset
                };

                bufferCopyRegions[index++] = bufferCopyRegion;

                offset += tex2D.Mipmaps[i].SizeInBytes;
            }

            ImageSubresourceRange subresourceRange = new ImageSubresourceRange(ImageAspectFlags.Color, 0, mipLevels, 0, 1);

            // Use a separate command buffer for texture loading
            CommandBuffer copyCmd = Graphics.CreateCommandBuffer(CommandBufferLevel.Primary, true);
            // Image barrier for optimal image (target)
            // Optimal image will be used as destination for the copy
            copyCmd.SetImageLayout(image, ImageAspectFlags.Color, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, subresourceRange);
            copyCmd.CopyBufferToImage(stagingBuffer, image, ImageLayout.TransferDstOptimal, bufferCopyRegions);
            copyCmd.SetImageLayout(image, ImageAspectFlags.Color, ImageLayout.TransferDstOptimal, imageLayout, subresourceRange);

            Graphics.FlushCommandBuffer(copyCmd, Graphics.GraphicsQueue);
            // Clean up staging resources
            stagingBuffer.Dispose();

            imageView = ImageView.Create(image, ImageViewType.Image2D, format, ImageAspectFlags.Color, 0, mipLevels);
            sampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.Repeat, Device.Features.samplerAnisotropy == 1);
            
            UpdateDescriptor();
        }
    }
}
