using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Vulkan;

namespace SharpGame
{
    using static Vulkan.VulkanNative;

    public partial class Texture : Resource, IBindableResource
    {
        public uint width;
        public uint height;
        public uint layers;
        public uint mipLevels;
        public uint depth;

        public Format format;
        public ImageUsageFlags imageUsageFlags;
        public ImageLayout imageLayout;

        public ImageView imageView;
        public Image image;
        public Sampler sampler;

        internal VkDeviceMemory deviceMemory;
        internal DescriptorImageInfo descriptor;

        public Texture()
        {
        }

        internal void UpdateDescriptor()
        {
            descriptor = new DescriptorImageInfo(sampler, imageView, imageLayout);           
        }


        protected override void Destroy()
        {
            imageView?.Dispose();
            image?.Dispose();
            sampler?.Dispose();

            Device.FreeMemory(deviceMemory);

            base.Destroy();
        }

        static int NumMipmapLevels(uint width, uint height)
        {
            int levels = 1;
            while (((width | height) >> levels) != 0)
            {
                ++levels;
            }
            return levels;
        }

        public static Texture White;
        public static Texture Gray;
        public static Texture Black;
        public static Texture Purple;

        public unsafe static void Init()
        {
            Texture CreateTex(Color color)
            {
                byte* c = &color.R;
                return Texture.Create2D(1, 1, Format.R8g8b8a8Unorm, c);
            }

            White = CreateTex(Color.White);
            Gray = CreateTex(Color.Gray);
            Black = CreateTex(Color.Black);
            Purple = CreateTex(Color.Purple);
        }

        public static Texture Create(uint width, uint height, ImageViewType imageViewType, uint layers, Format format, uint levels = 0, ImageUsageFlags additionalUsage = ImageUsageFlags.None)
        {
            Texture texture = new Texture
            {
                width = width,
                height = height,
                layers = layers,
                mipLevels = (levels > 0) ? levels : (uint)NumMipmapLevels(width, height)
            };

            ImageUsageFlags usage = ImageUsageFlags.Sampled | ImageUsageFlags.TransferDst | additionalUsage;
            if (texture.mipLevels > 1)
            {
                usage |= ImageUsageFlags.TransferSrc; // For mipmap generation
            }

            texture.image = Image.Create(width, height, (imageViewType == ImageViewType.ImageCube || imageViewType == ImageViewType.ImageCubeArray) ? ImageCreateFlags.CubeCompatible : ImageCreateFlags.None, layers, texture.mipLevels, format,  SampleCountFlags.Count1, usage);
            texture.imageView = ImageView.Create(texture.image, imageViewType, format, ImageAspectFlags.Color, 0, RemainingMipLevels);            
            texture.sampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.ClampToBorder, Device.Features.samplerAnisotropy == 1);
            texture.UpdateDescriptor();
            return texture;
        }

        public void GenerateMipmaps()
        {             
            CommandBuffer commandBuffer = Graphics.Instance.BeginWorkCommandBuffer();

	        // Iterate through mip chain and consecutively blit from previous level to next level with linear filtering.
	        for(uint level=1, prevLevelWidth = width, prevLevelHeight = height; level< mipLevels; ++level, prevLevelWidth /= 2, prevLevelHeight /=2 )
            {
                var preBlitBarrier = new ImageMemoryBarrier(this, 0, AccessFlags.TransferWrite, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, ImageAspectFlags.Color, level, 1);
                commandBuffer.PipelineBarrier(PipelineStageFlags.Transfer, PipelineStageFlags.Transfer, ref preBlitBarrier);

                ImageBlit region = new ImageBlit
                {
                    srcSubresource = new ImageSubresourceLayers
                    {
                        aspectMask = ImageAspectFlags.Color,
                        mipLevel = level - 1,
                        baseArrayLayer = 0,
                        layerCount = layers
                    },

                    dstSubresource = new ImageSubresourceLayers
                    {
                        aspectMask = ImageAspectFlags.Color,
                        mipLevel = level,
                        baseArrayLayer = 0,
                        layerCount = layers
                    },

                    srcOffsets_1 = new Offset3D((int)(prevLevelWidth),  (int)(prevLevelHeight), 1 ),
                    dstOffsets_1 = new Offset3D((int)(prevLevelWidth / 2),(int)(prevLevelHeight / 2), 1),
                };

                commandBuffer.BlitImage(image,  ImageLayout.TransferSrcOptimal, image, ImageLayout.TransferDstOptimal, ref region,  Filter.Linear);

                var postBlitBarrier = new ImageMemoryBarrier(this, AccessFlags.TransferWrite, AccessFlags.TransferRead, ImageLayout.TransferDstOptimal, ImageLayout.TransferSrcOptimal, ImageAspectFlags.Color, level, 1);
                commandBuffer.PipelineBarrier(PipelineStageFlags.Transfer, PipelineStageFlags.Transfer, ref postBlitBarrier);
            }

            // Transition whole mip chain to shader read only layout.
            {
		        var barrier = new ImageMemoryBarrier(this, AccessFlags.TransferWrite, 0, ImageLayout.TransferSrcOptimal,  ImageLayout.ShaderReadOnlyOptimal);
                commandBuffer.PipelineBarrier(PipelineStageFlags.Transfer, PipelineStageFlags.BottomOfPipe, ref barrier);
	        }

            Graphics.Instance.EndWorkCommandBuffer(commandBuffer);
        }

    }

    public class ImageData
    {
        public uint Width { get; set; }
        public uint Height { get; set; }
        public uint NumberOfMipmapLevels { get; }
        public MipmapData[] Mipmaps { get; }

        public ImageData(uint width, uint height, uint numberOfMipmapLevels, MipmapData[] mipmaps)
        {
            Width = width;
            Height = height;
            NumberOfMipmapLevels = numberOfMipmapLevels;
            Mipmaps = mipmaps;
        }

        public ImageData(uint numberOfMipmapLevels)
        {
            NumberOfMipmapLevels = numberOfMipmapLevels;
            Mipmaps = new MipmapData[numberOfMipmapLevels];
        }


        public ulong GetTotalSize()
        {
            ulong totalSize = 0;

            for (int mipLevel = 0; mipLevel < Mipmaps.Length; mipLevel++)
            {
                MipmapData mipmap = Mipmaps[mipLevel];
                totalSize += mipmap.SizeInBytes;

            }

            return totalSize;
        }


        public byte[] GetAllTextureData()
        {
            byte[] result = new byte[GetTotalSize()];
            uint start = 0;

            for (int mipLevel = 0; mipLevel < Mipmaps.Length; mipLevel++)
            {
                MipmapData mipmap = Mipmaps[mipLevel];
                mipmap.Data.CopyTo(result, (int)start);
                start += mipmap.SizeInBytes;
            }


            return result;
        }
    }

    public class MipmapData
    {
        public uint SizeInBytes { get; }
        public byte[] Data { get; }
        public uint Width { get; }
        public uint Height { get; }

        public MipmapData(uint sizeInBytes, byte[] data, uint width, uint height)
        {
            SizeInBytes = sizeInBytes;
            Data = data;
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
        }
    }
}
