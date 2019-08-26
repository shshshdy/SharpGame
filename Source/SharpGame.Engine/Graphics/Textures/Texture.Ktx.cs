using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using static Vulkan.VulkanNative;
    public partial class Texture : Resource, IBindableResource
    {
        public static Texture LoadFromFile(string filename, Format format, SamplerAddressMode samplerAddressMode = SamplerAddressMode.Repeat)
        {
            var tex = new Texture();
            tex.LoadFromFileInternal(filename, format, samplerAddressMode);
            return tex;
        }

        public unsafe void LoadFromFileInternal(string filename, Format format, SamplerAddressMode samplerAddressMode = SamplerAddressMode.Repeat)
        {
            KtxFile texFile;
            using (var fs = FileSystem.Instance.GetFile(filename))
            {
                texFile = KtxFile.Load(fs, readKeyValuePairs: false);
            }

            width = texFile.Header.PixelWidth;
            height = texFile.Header.PixelHeight;
            mipLevels = texFile.Header.NumberOfMipmapLevels;
            layers = (uint)texFile.Faces.Length;

            VkMemoryAllocateInfo memAllocInfo = VkMemoryAllocateInfo.New();
            VkMemoryRequirements memReqs;

            // Create a host-visible staging buffer that contains the raw image data
            VkBuffer stagingBuffer;
            VkDeviceMemory stagingMemory;

            VkBufferCreateInfo bufferCreateInfo = VkBufferCreateInfo.New();
            bufferCreateInfo.size = texFile.GetTotalSize();
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
            byte[] allTextureData = texFile.GetAllTextureData();
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
                arrayLayers = layers,
                // This flag is required for cube map images
                flags = layers == 6 ? ImageCreateFlags.CubeCompatible : ImageCreateFlags.None
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

            for (uint face = 0; face < layers; face++)
            {
                for (uint level = 0; level < mipLevels; level++)
                {
                    VkBufferImageCopy bufferCopyRegion = new VkBufferImageCopy();
                    bufferCopyRegion.imageSubresource.aspectMask = VkImageAspectFlags.Color;
                    bufferCopyRegion.imageSubresource.mipLevel = level;
                    bufferCopyRegion.imageSubresource.baseArrayLayer = face;
                    bufferCopyRegion.imageSubresource.layerCount = 1;
                    bufferCopyRegion.imageExtent.width = texFile.Faces[face].Mipmaps[level].Width;
                    bufferCopyRegion.imageExtent.height = texFile.Faces[face].Mipmaps[level].Height;
                    bufferCopyRegion.imageExtent.depth = 1;
                    bufferCopyRegion.bufferOffset = offset;

                    bufferCopyRegions.Add(bufferCopyRegion);

                    // Increase offset into staging buffer for next level / face
                    offset += texFile.Faces[face].Mipmaps[level].SizeInBytes;
                }
            }

            // Image barrier for optimal image (target)
            // Set initial layout for all array layers (faces) of the optimal (target) tiled texture
            VkImageSubresourceRange subresourceRange = new VkImageSubresourceRange();
            subresourceRange.aspectMask = VkImageAspectFlags.Color;
            subresourceRange.baseMipLevel = 0;
            subresourceRange.levelCount = (uint)mipLevels;
            subresourceRange.layerCount = layers;

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

            Device.FlushCommandBuffer(copyCmd, Graphics.GraphicsQueue.native, true);
            
            sampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, samplerAddressMode, Device.Features.samplerAnisotropy == 1);

            // Create image view
            ImageViewCreateInfo view = new ImageViewCreateInfo
            {
                // Cube map view type
                viewType = layers == 6 ? ImageViewType.ImageCube : ImageViewType.Image2D,
                format = format,
                components = new ComponentMapping(ComponentSwizzle.R, ComponentSwizzle.G, ComponentSwizzle.B, ComponentSwizzle.A),
                subresourceRange = new ImageSubresourceRange { aspectMask = ImageAspectFlags.Color, baseMipLevel = 0, layerCount = 1, baseArrayLayer = 0, levelCount = 1 }
            };
            // array layers (faces)
            view.subresourceRange.layerCount = layers;
            // Set number of mip levels
            view.subresourceRange.levelCount = (uint)mipLevels;
            view.image = image;

            imageView = new ImageView(ref view);

            // Clean up staging resources
            Device.FreeMemory(stagingMemory);
            Device.DestroyBuffer(stagingBuffer);

            UpdateDescriptor();
        }

    }
}
