﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Vulkan;

namespace SharpGame
{
    using static Vulkan.VulkanNative;

    public partial class Texture : Resource, IBindableResource
    {
        public Image image;
        public ImageView imageView;
        public Sampler sampler;

        public uint width;
        public uint height;
        public uint layers;
        public uint mipLevels;
        public uint depth;
        public Format format;
        public ImageCreateFlags imageCreateFlags = ImageCreateFlags.None;
        public ImageUsageFlags imageUsageFlags = ImageUsageFlags.Sampled;
        public ImageLayout imageLayout = ImageLayout.ShaderReadOnlyOptimal;
        public SamplerAddressMode samplerAddressMode = SamplerAddressMode.Repeat;

        internal DescriptorImageInfo descriptor;

        public Texture()
        {
        }

        public unsafe void SetImageData(ImageData imageData)
        {
            width = imageData.Width;
            height = imageData.Height;
            mipLevels = (uint)imageData.Mipmaps.Length;
            layers = (uint)imageData.Mipmaps[0].ArrayElements.Length;

            ulong totalSize = imageData.GetTotalSize();        

            Buffer stagingBuffer = Buffer.CreateStagingBuffer(totalSize, IntPtr.Zero);

            image = Image.Create(width, height, imageCreateFlags/*layers == 6 ? ImageCreateFlags.CubeCompatible : ImageCreateFlags.None*/, 
                layers, mipLevels, format, SampleCountFlags.Count1, ImageUsageFlags.TransferDst | ImageUsageFlags.Sampled);

            IntPtr mapped = stagingBuffer.Map();
            // Setup buffer copy regions for each face including all of it's miplevels
            Span<BufferImageCopy> bufferCopyRegions = stackalloc BufferImageCopy[(int)(/*layers **/ mipLevels)];
            uint offset = 0;
            int index = 0;

            for (uint level = 0; level < mipLevels; level++)
            {
                var mipLevel = imageData.Mipmaps[level];
                for (uint layer = 0; layer < mipLevel.ArrayElementSize; layer++)
                {
                    for (uint face = 0; face < layers; face++)
                    {
                        var faceElement = mipLevel.ArrayElements[layer].Faces[face]; 
                        Unsafe.CopyBlock((void*)(mapped + (int)offset), Unsafe.AsPointer(ref faceElement.Data[0]), (uint)faceElement.Data.Length);
                    }
                }

                BufferImageCopy bufferCopyRegion = new BufferImageCopy
                {
                    imageSubresource = new ImageSubresourceLayers
                    {
                        aspectMask = ImageAspectFlags.Color,
                        mipLevel = level,
                        baseArrayLayer = 0,
                        layerCount = mipLevel.ArrayElementSize
                    },

                    imageExtent = new Extent3D(mipLevel.Width, mipLevel.Height, mipLevel.Depth),
                    bufferOffset = offset
                };

                bufferCopyRegions[index++] = bufferCopyRegion;
                offset += mipLevel.TotalSize;
            }

            stagingBuffer.Unmap();

            ImageSubresourceRange subresourceRange = new ImageSubresourceRange(ImageAspectFlags.Color, 0, mipLevels, 0, layers);

            CommandBuffer copyCmd = Graphics.BeginPrimaryCmd();
            copyCmd.SetImageLayout(image, ImageAspectFlags.Color, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, subresourceRange);
            copyCmd.CopyBufferToImage(stagingBuffer, image, ImageLayout.TransferDstOptimal, bufferCopyRegions);
            copyCmd.SetImageLayout(image, ImageAspectFlags.Color, ImageLayout.TransferDstOptimal, imageLayout, subresourceRange);
            Graphics.EndPrimaryCmd(copyCmd);

            imageLayout = ImageLayout.ShaderReadOnlyOptimal;

            stagingBuffer.Dispose();

            imageView = ImageView.Create(image, layers == 6 ? ImageViewType.ImageCube : ImageViewType.Image2D, format, ImageAspectFlags.Color, 0, mipLevels, 0, layers);
            sampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, samplerAddressMode, Device.Features.samplerAnisotropy == 1);

            UpdateDescriptor();

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

        public void GenerateMipmaps()
        {
            CommandBuffer commandBuffer = Graphics.BeginPrimaryCmd();

            // Iterate through mip chain and consecutively blit from previous level to next level with linear filtering.
            for (uint level = 1, prevLevelWidth = width, prevLevelHeight = height; level < mipLevels; ++level, prevLevelWidth /= 2, prevLevelHeight /= 2)
            {
                var preBlitBarrier = new ImageMemoryBarrier(image, 0, AccessFlags.TransferWrite, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, ImageAspectFlags.Color, level, 1);
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

                    srcOffsets_1 = new Offset3D((int)(prevLevelWidth), (int)(prevLevelHeight), 1),
                    dstOffsets_1 = new Offset3D((int)(prevLevelWidth / 2), (int)(prevLevelHeight / 2), 1),
                };

                commandBuffer.BlitImage(image, ImageLayout.TransferSrcOptimal, image, ImageLayout.TransferDstOptimal, ref region, Filter.Linear);

                var postBlitBarrier = new ImageMemoryBarrier(image, AccessFlags.TransferWrite, AccessFlags.TransferRead, ImageLayout.TransferDstOptimal, ImageLayout.TransferSrcOptimal, ImageAspectFlags.Color, level, 1);
                commandBuffer.PipelineBarrier(PipelineStageFlags.Transfer, PipelineStageFlags.Transfer, ref postBlitBarrier);
            }

            // Transition whole mip chain to shader read only layout.
            {
                var barrier = new ImageMemoryBarrier(image, AccessFlags.TransferWrite, 0, ImageLayout.TransferSrcOptimal, ImageLayout.ShaderReadOnlyOptimal);
                commandBuffer.PipelineBarrier(PipelineStageFlags.Transfer, PipelineStageFlags.BottomOfPipe, ref barrier);
            }

            Graphics.EndPrimaryCmd(commandBuffer);
        }

        internal void UpdateDescriptor()
        {
            descriptor = new DescriptorImageInfo(sampler, imageView, imageLayout);
        }

        protected override void Destroy(bool disposing)
        {
            image?.Dispose();
            imageView?.Dispose();
            sampler?.Dispose();

            base.Destroy(disposing);
        }

        public static Texture White;
        public static Texture Gray;
        public static Texture Black;
        public static Texture Purple;
        public static Texture Blue;

        public unsafe static void Init()
        {
            White = CreateByColor(Color.White);
            Gray = CreateByColor(Color.Gray);
            Black = CreateByColor(Color.Black);
            Purple = CreateByColor(Color.Purple);
            Blue = CreateByColor(Color.Blue);
        }

        public static Texture CreateByColor(Color color)
        {
            return Texture.Create2D(1, 1, Format.R8g8b8a8Unorm, Utilities.AsPointer(ref color));
        }

        public static Texture Create(uint width, uint height, ImageViewType imageViewType, uint layers, Format format, uint levels = 0, ImageUsageFlags additionalUsage = ImageUsageFlags.None)
        {
            Texture texture = new Texture
            {
                width = width,
                height = height,
                layers = layers,
                mipLevels = (levels > 0) ? levels : (uint)NumMipmapLevels(width, height),

            };

            ImageUsageFlags usage = ImageUsageFlags.Sampled | ImageUsageFlags.TransferDst | additionalUsage;
            if (texture.mipLevels > 1)
            {
                usage |= ImageUsageFlags.TransferSrc; // For mipmap generation
            }

            texture.image = Image.Create(width, height, (imageViewType == ImageViewType.ImageCube || imageViewType == ImageViewType.ImageCubeArray) ? ImageCreateFlags.CubeCompatible : ImageCreateFlags.None, layers, texture.mipLevels, format,  SampleCountFlags.Count1, usage);
            texture.imageView = ImageView.Create(texture.image, imageViewType, format, ImageAspectFlags.Color, 0, RemainingMipLevels, 0, layers);            
            texture.sampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.ClampToBorder, Device.Features.samplerAnisotropy == 1);
            texture.UpdateDescriptor();
            return texture;
        }

        public unsafe static Texture Create2D(uint w, uint h, Format format, IntPtr tex2DDataPtr, bool dynamic = false)
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

            using (Buffer stagingBuffer = Buffer.CreateStagingBuffer(totalBytes, tex2DDataPtr))
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

                // The sub resource range describes the regions of the image we will be transition
                ImageSubresourceRange subresourceRange = new ImageSubresourceRange(ImageAspectFlags.Color, 0, 1, 0, 1);
                CommandBuffer copyCmd = Graphics.BeginPrimaryCmd();
                copyCmd.SetImageLayout(texture.image, ImageAspectFlags.Color, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, subresourceRange);
                copyCmd.CopyBufferToImage(stagingBuffer, texture.image, ImageLayout.TransferDstOptimal, ref bufferCopyRegion);
                copyCmd.SetImageLayout(texture.image, ImageAspectFlags.Color, ImageLayout.TransferDstOptimal, texture.imageLayout, subresourceRange);
                Graphics.EndPrimaryCmd(copyCmd);

                // Change texture image layout to shader read after all mip levels have been copied
                texture.imageLayout = ImageLayout.ShaderReadOnlyOptimal;
            }

            texture.imageView = ImageView.Create(texture.image, ImageViewType.Image2D, format, ImageAspectFlags.Color, 0, texture.mipLevels);
            texture.sampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.Repeat, Device.Features.samplerAnisotropy == 1);
            texture.UpdateDescriptor();
            return texture;
        }

        // Prepare a texture target that is used to store compute shader calculations
        public static Texture CreateStorage(uint width, uint height, Format format)
        {
            var texture = new Texture
            {
                width = width,
                height = height,
                mipLevels = 1,
                depth = 1,
                format = format
            };

            ImageCreateInfo createInfo = new ImageCreateInfo
            {
                flags = ImageCreateFlags.None,
                imageType = ImageType.Image2D,
                format = format,
                extent = new Extent3D { width = width, height = height, depth = 1 },
                mipLevels = 1,
                arrayLayers = 1,
                samples = SampleCountFlags.Count1,
                tiling = ImageTiling.Optimal,
                usage = ImageUsageFlags.Storage | ImageUsageFlags.Sampled,
                sharingMode = SharingMode.Exclusive,
                initialLayout = ImageLayout.Preinitialized
            };

            texture.image = new Image(ref createInfo);

            Graphics.WithCommandBuffer((cmd) => {
                cmd.SetImageLayout(texture.image, ImageAspectFlags.Color, ImageLayout.Preinitialized, ImageLayout.General);
            });

            texture.imageLayout = ImageLayout.General;
            texture.imageView = ImageView.Create(texture.image, ImageViewType.Image2D, format, ImageAspectFlags.Color, 0, texture.mipLevels);
            texture.sampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.Repeat, Device.Features.samplerAnisotropy == 1);
            texture.UpdateDescriptor();
            return texture;
        }
    }

    public class ImageData
    {
        public uint Width { get; set; }
        public uint Height { get; set; }
        public uint NumberOfMipmapLevels { get; }
        public MipmapLevel[] Mipmaps { get; }

        public ImageData(uint width, uint height, uint numberOfMipmapLevels, MipmapLevel[] mipmaps)
        {
            Width = width;
            Height = height;
            NumberOfMipmapLevels = numberOfMipmapLevels;
            Mipmaps = mipmaps;
        }

        public ImageData(uint numberOfMipmapLevels)
        {
            NumberOfMipmapLevels = numberOfMipmapLevels;
            Mipmaps = new MipmapLevel[numberOfMipmapLevels];
        }

        public ulong GetTotalSize()
        {
            ulong totalSize = 0;

            for (int mipLevel = 0; mipLevel < Mipmaps.Length; mipLevel++)
            {
                var mipmap = Mipmaps[mipLevel];
                totalSize += mipmap.TotalSize;

            }

            return totalSize;
        }

    }

    // for each mipmap_level in numberOfMipmapLevels
    public struct MipmapLevel
    {
        public MipmapLevel(uint totalSize, byte[] data, uint width, uint height, uint depth)
        {
            Width = width;
            Height = height;
            Depth = depth;
            TotalSize = totalSize;
            ArrayElementSize = 1;

            ArrayElements = new[]
            {
                new ArrayElement(new[] { new ImageFace(data) })
            };

        }

        public MipmapLevel(uint width, uint height, uint depth, uint totalSize, uint arraySliceSize, ArrayElement[] slices)
        {
            Width = width;
            Height = height;
            Depth = depth;
            TotalSize = totalSize;
            ArrayElementSize = arraySliceSize;
            ArrayElements = slices;
        }

        public uint Width { get; }
        public uint Height { get; }
        public uint Depth { get; }
        public uint TotalSize { get; }
        public uint ArrayElementSize { get; }
        public ArrayElement[] ArrayElements { get; }
    }

    public struct ArrayElement
    {
        public ImageFace[] Faces { get; }
        public ArrayElement(ImageFace[] faces)
        {
            Faces = faces;
        }

    }

    // for each face in numberOfFaces
    public struct ImageFace
    {
        public byte[] Data { get; }
        public ImageFace(byte[] data)
        {
            Data = data;
        }

    }
}
