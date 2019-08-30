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
            
            image = Image.Create(width, height, layers == 6 ? ImageCreateFlags.CubeCompatible : ImageCreateFlags.None, layers, mipLevels, format, SampleCountFlags.Count1, ImageUsageFlags.TransferDst | ImageUsageFlags.Sampled);

            // Setup buffer copy regions for each face including all of it's miplevels
            Span<BufferImageCopy> bufferCopyRegions = stackalloc BufferImageCopy[(int)(layers* mipLevels)];
            uint offset = 0;
            int index = 0;
            for (uint face = 0; face < layers; face++)
            {
                for (uint level = 0; level < mipLevels; level++)
                {
                    BufferImageCopy bufferCopyRegion = new BufferImageCopy
                    {
                        imageSubresource = new ImageSubresourceLayers
                        {
                            aspectMask = ImageAspectFlags.Color,
                            mipLevel = level,
                            baseArrayLayer = face,
                            layerCount = 1
                        },

                        imageExtent = new Extent3D(texFile.Faces[face].Mipmaps[level].Width, texFile.Faces[face].Mipmaps[level].Height, 1),
                        bufferOffset = offset
                    };

                    bufferCopyRegions[index++] = bufferCopyRegion;

                    // Increase offset into staging buffer for next level / face
                    offset += texFile.Faces[face].Mipmaps[level].SizeInBytes;
                }
            }

            ImageSubresourceRange subresourceRange = new ImageSubresourceRange(ImageAspectFlags.Color, 0, mipLevels, 0, layers);
            CommandBuffer copyCmd = Graphics.CreateCommandBuffer(CommandBufferLevel.Primary, true);
            copyCmd.SetImageLayout(image, ImageAspectFlags.Color, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, subresourceRange);
            copyCmd.CopyBufferToImage(stagingBuffer, image, ImageLayout.TransferDstOptimal, bufferCopyRegions);
            copyCmd.SetImageLayout(image, ImageAspectFlags.Color, ImageLayout.TransferDstOptimal, imageLayout, subresourceRange);
            Graphics.FlushCommandBuffer(copyCmd, Graphics.GraphicsQueue, true);
            imageLayout = ImageLayout.ShaderReadOnlyOptimal;

            stagingBuffer.Dispose();

            imageView = ImageView.Create(image, layers == 6 ? ImageViewType.ImageCube : ImageViewType.Image2D, format, ImageAspectFlags.Color, 0, mipLevels, 0, layers);
            sampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, samplerAddressMode, Device.Features.samplerAnisotropy == 1);

            UpdateDescriptor();

        }

    }
}
