using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
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
            
            byte[] allTextureData = texFile.GetAllTextureData();
            DeviceBuffer stagingBuffer;
            fixed (byte* texCubeDataPtr = &allTextureData[0])
            {
                stagingBuffer = DeviceBuffer.CreateStagingBuffer(texFile.GetTotalSize(), texCubeDataPtr);
            }

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

            // Setup buffer copy regions for each face including all of it's miplevels
            Span<BufferImageCopy> bufferCopyRegions = stackalloc BufferImageCopy[(int)(layers* mipLevels)];
            uint offset = 0;
            int index = 0;
            for (uint face = 0; face < layers; face++)
            {
                for (uint level = 0; level < mipLevels; level++)
                {
                    BufferImageCopy bufferCopyRegion = new BufferImageCopy();
                    bufferCopyRegion.imageSubresource.aspectMask = ImageAspectFlags.Color;
                    bufferCopyRegion.imageSubresource.mipLevel = level;
                    bufferCopyRegion.imageSubresource.baseArrayLayer = face;
                    bufferCopyRegion.imageSubresource.layerCount = 1;
                    bufferCopyRegion.imageExtent.width = texFile.Faces[face].Mipmaps[level].Width;
                    bufferCopyRegion.imageExtent.height = texFile.Faces[face].Mipmaps[level].Height;
                    bufferCopyRegion.imageExtent.depth = 1;
                    bufferCopyRegion.bufferOffset = offset;

                    bufferCopyRegions[index++] = bufferCopyRegion;

                    // Increase offset into staging buffer for next level / face
                    offset += texFile.Faces[face].Mipmaps[level].SizeInBytes;
                }
            }

            // Image barrier for optimal image (target)
            // Set initial layout for all array layers (faces) of the optimal (target) tiled texture
            ImageSubresourceRange subresourceRange = new ImageSubresourceRange
            {
                aspectMask = ImageAspectFlags.Color,
                baseMipLevel = 0,
                levelCount = mipLevels,
                layerCount = layers
            };

            CommandBuffer copyCmd = Graphics.CreateCommandBuffer(CommandBufferLevel.Primary, true);
            copyCmd.SetImageLayout(image, ImageAspectFlags.Color, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, subresourceRange);
            copyCmd.CopyBufferToImage(stagingBuffer, image, ImageLayout.TransferDstOptimal, bufferCopyRegions);
            copyCmd.SetImageLayout(image, ImageAspectFlags.Color, ImageLayout.TransferDstOptimal, imageLayout, subresourceRange);
            Graphics.FlushCommandBuffer(copyCmd, Graphics.GraphicsQueue, true);
            
            // Change texture image layout to shader read after all faces have been copied
            imageLayout = ImageLayout.ShaderReadOnlyOptimal;

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

            UpdateDescriptor();

            stagingBuffer.Dispose();
        }

    }
}
