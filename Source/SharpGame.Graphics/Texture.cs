using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;


namespace SharpGame
{
    using static Vulkan;

    public partial class Texture : Resource, IBindableResource
    {
        public Image image;
        public ImageView imageView;
        public Sampler sampler;
        public VkExtent3D extent;
        public uint layers;
        public uint faceCount;
        public uint mipLevels;
        public VkFormat format;
        public VkImageCreateFlags imageCreateFlags = VkImageCreateFlags.None;
        public VkImageUsageFlags imageUsageFlags = VkImageUsageFlags.Sampled;
        public VkImageLayout imageLayout = VkImageLayout.ShaderReadOnlyOptimal;
        public VkSamplerAddressMode samplerAddressMode = VkSamplerAddressMode.Repeat;

        internal VkDescriptorImageInfo descriptor;

        MipmapLevel[] imageData;
        public MipmapLevel[] ImageData => imageData;

        public Texture()
        {
        }

        public uint width => extent.width;
        public uint height => extent.height;
        public uint depth => extent.depth;

        public byte[] GetData(int level, int layer, int face)
        {
            return imageData[level].ArrayElements[layer].Faces[face].Data;
        }

        public unsafe void SetImageData(MipmapLevel[] imageData)
        {
            this.imageData = imageData;
            this.extent = new VkExtent3D(imageData[0].Width, imageData[0].Height, 1);          
            mipLevels = (uint)imageData.Length;
            layers = (uint)imageData[0].ArrayElements.Length;
            faceCount = (uint)imageData[0].ArrayElements[0].Faces.Length;

            ulong totalSize = 0;
            foreach (var mip in imageData)
            {
                totalSize += mip.TotalSize * layers * faceCount;
            }

            Buffer stagingBuffer = Buffer.CreateStagingBuffer(totalSize, IntPtr.Zero);

            image = Image.Create(width, height, imageCreateFlags, layers * faceCount, mipLevels, format, VkSampleCountFlags.Count1, VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled);

            IntPtr mapped = stagingBuffer.Map();

            // Setup buffer copy regions for each face including all of it's miplevels
            Span<VkBufferImageCopy> bufferCopyRegions = stackalloc VkBufferImageCopy[(int)(mipLevels* layers * faceCount)];
            uint offset = 0;
            int index = 0;

            for (int layer = 0; layer < layers; layer++)
            {
                for (int face = 0; face < faceCount; face++)
                {
                    for (uint level = 0; level < mipLevels; level++)
                    {
                        var mipLevel = imageData[level];
                        var layerElement = mipLevel.ArrayElements[layer];
                        var faceElement = layerElement.Faces[face];

                        Unsafe.CopyBlock((void*)(mapped + (int)offset), Unsafe.AsPointer(ref faceElement.Data[0]), (uint)faceElement.Data.Length);

                        VkBufferImageCopy bufferCopyRegion = new VkBufferImageCopy
                        {
                            imageSubresource = new VkImageSubresourceLayers
                            {
                                aspectMask = VkImageAspectFlags.Color,
                                mipLevel = level,
                                baseArrayLayer = (uint)(layer * faceCount + face),
                                layerCount = 1
                            },

                            imageExtent = new VkExtent3D(mipLevel.Width, mipLevel.Height, mipLevel.Depth),
                            bufferOffset = offset
                        };

                        bufferCopyRegions[index++] = bufferCopyRegion;
                        offset += (uint)faceElement.Data.Length;

                    }

                }

            }


            stagingBuffer.Unmap();

            var subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, mipLevels, 0, layers* faceCount);

            CommandBuffer copyCmd = Graphics.BeginPrimaryCmd();
            copyCmd.SetImageLayout(image, VkImageAspectFlags.Color, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal, subresourceRange);
            copyCmd.CopyBufferToImage(stagingBuffer, image, VkImageLayout.TransferDstOptimal, bufferCopyRegions);
            copyCmd.SetImageLayout(image, VkImageAspectFlags.Color, VkImageLayout.TransferDstOptimal, imageLayout, subresourceRange);
            Graphics.EndPrimaryCmd(copyCmd);

            imageLayout = VkImageLayout.ShaderReadOnlyOptimal;

            stagingBuffer.Dispose();

            imageView = ImageView.Create(image, ImageViewType, format, VkImageAspectFlags.Color, 0, mipLevels, 0, layers);
            sampler = new Sampler(VkFilter.Linear, VkSamplerMipmapMode.Linear, samplerAddressMode, Device.Features.samplerAnisotropy == true);

            UpdateDescriptor();

        }

        public VkImageViewType ImageViewType
        {
            get
            {
                if (layers > 1)
                {
                    if (height == 1)
                        return VkImageViewType.Image1DArray;
                    else
                    {
                        if (faceCount == 6)
                        {
                            return VkImageViewType.ImageCubeArray;
                        }
                        else
                        {
                            return VkImageViewType.Image2DArray;
                        }
                    }
                }
                else
                {
                    if (height == 1)
                        return VkImageViewType.Image1D;
                    else
                    {
                        if (faceCount == 6)
                            return VkImageViewType.ImageCube;

                        if(depth > 1)
                        {
                            return VkImageViewType.Image3D;
                        }
                        return VkImageViewType.Image2D;
                    }
                }

            }
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
                var preBlitBarrier = new VkImageMemoryBarrier(image, 0, VkAccessFlags.TransferWrite, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal, VkImageAspectFlags.Color, level, 1);
                commandBuffer.PipelineBarrier(VkPipelineStageFlags.Transfer, VkPipelineStageFlags.Transfer, ref preBlitBarrier);

                var region = new VkImageBlit
                {
                    srcSubresource = new VkImageSubresourceLayers
                    {
                        aspectMask = VkImageAspectFlags.Color,
                        mipLevel = level - 1,
                        baseArrayLayer = 0,
                        layerCount = layers
                    },

                    dstSubresource = new VkImageSubresourceLayers
                    {
                        aspectMask = VkImageAspectFlags.Color,
                        mipLevel = level,
                        baseArrayLayer = 0,
                        layerCount = layers
                    },

                    srcOffsets_1 = new VkOffset3D((int)(prevLevelWidth), (int)(prevLevelHeight), 1),
                    dstOffsets_1 = new VkOffset3D((int)(prevLevelWidth / 2), (int)(prevLevelHeight / 2), 1),
                };

                commandBuffer.BlitImage(image, VkImageLayout.TransferSrcOptimal, image, VkImageLayout.TransferDstOptimal, ref region, VkFilter.Linear);

                var postBlitBarrier = new VkImageMemoryBarrier(image, VkAccessFlags.TransferWrite, VkAccessFlags.TransferRead, VkImageLayout.TransferDstOptimal, VkImageLayout.TransferSrcOptimal, VkImageAspectFlags.Color, level, 1);
                commandBuffer.PipelineBarrier(VkPipelineStageFlags.Transfer, VkPipelineStageFlags.Transfer, ref postBlitBarrier);
            }

            // Transition whole mip chain to shader read only layout.
            {
                var barrier = new VkImageMemoryBarrier(image, VkAccessFlags.TransferWrite, 0, VkImageLayout.TransferSrcOptimal, VkImageLayout.ShaderReadOnlyOptimal);
                commandBuffer.PipelineBarrier(VkPipelineStageFlags.Transfer, VkPipelineStageFlags.BottomOfPipe, ref barrier);
            }

            Graphics.EndPrimaryCmd(commandBuffer);
        }

        public void UpdateDescriptor()
        {
            descriptor = new VkDescriptorImageInfo(sampler, imageView, imageLayout);
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
            return Texture.Create2D(1, 1, VkFormat.R8G8B8A8UNorm, Utilities.AsPointer(ref color));
        }

        public static Texture Create(uint width, uint height, VkImageViewType imageViewType, uint layers, VkFormat format, uint levels = 0, VkImageUsageFlags additionalUsage = VkImageUsageFlags.None)
        {
            Texture texture = new Texture
            {
                extent = new VkExtent3D(width, height, 1),
                layers = layers,
                mipLevels = (levels > 0) ? levels : (uint)NumMipmapLevels(width, height),

            };

            VkImageUsageFlags usage = VkImageUsageFlags.Sampled | VkImageUsageFlags.TransferDst | additionalUsage;
            if (texture.mipLevels > 1)
            {
                usage |= VkImageUsageFlags.TransferSrc; // For mipmap generation
            }

            texture.image = Image.Create(width, height, (imageViewType == VkImageViewType.ImageCube || imageViewType == VkImageViewType.ImageCubeArray) ? VkImageCreateFlags.CubeCompatible : VkImageCreateFlags.None, layers, texture.mipLevels, format, VkSampleCountFlags.Count1, usage);
            texture.imageView = ImageView.Create(texture.image, imageViewType, format, VkImageAspectFlags.Color, 0, Vulkan.RemainingMipLevels, 0, layers);
            texture.sampler = new Sampler(VkFilter.Linear, VkSamplerMipmapMode.Linear, VkSamplerAddressMode.ClampToBorder, Device.Features.samplerAnisotropy == true);
            texture.UpdateDescriptor();
            return texture;
        }

        public unsafe static Texture Create2D(uint w, uint h, VkFormat format, IntPtr tex2DDataPtr, bool dynamic = false)
        {
            var texture = new Texture
            { 
                extent = new VkExtent3D(w, h, 1),
                mipLevels = 1,
                format = format
            };

            texture.image = Image.Create(w, h, VkImageCreateFlags.None, 1, 1, format, VkSampleCountFlags.Count1, VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled);

            ulong totalBytes = texture.image.allocationSize;

            using (Buffer stagingBuffer = Buffer.CreateStagingBuffer(totalBytes, tex2DDataPtr))
            {
                VkBufferImageCopy bufferCopyRegion = new VkBufferImageCopy
                {
                    imageSubresource = new VkImageSubresourceLayers
                    {
                        aspectMask = VkImageAspectFlags.Color,
                        mipLevel = 0,
                        baseArrayLayer = 0,
                        layerCount = 1,
                    },

                    imageExtent = new VkExtent3D(w, h, 1),
                    bufferOffset = 0
                };

                // The sub resource range describes the regions of the image we will be transition
                var subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1);
                CommandBuffer copyCmd = Graphics.BeginPrimaryCmd();
                copyCmd.SetImageLayout(texture.image, VkImageAspectFlags.Color, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal, subresourceRange);
                copyCmd.CopyBufferToImage(stagingBuffer, texture.image, VkImageLayout.TransferDstOptimal, ref bufferCopyRegion);
                copyCmd.SetImageLayout(texture.image, VkImageAspectFlags.Color, VkImageLayout.TransferDstOptimal, texture.imageLayout, subresourceRange);
                Graphics.EndPrimaryCmd(copyCmd);

                // Change texture image layout to shader read after all mip levels have been copied
                texture.imageLayout = VkImageLayout.ShaderReadOnlyOptimal;
            }

            texture.imageView = ImageView.Create(texture.image, VkImageViewType.Image2D, format, VkImageAspectFlags.Color, 0, texture.mipLevels);
            texture.sampler = new Sampler(VkFilter.Linear, VkSamplerMipmapMode.Linear, VkSamplerAddressMode.Repeat, Device.Features.samplerAnisotropy == true);
            texture.UpdateDescriptor();
            return texture;
        }

        // Prepare a texture target that is used to store compute shader calculations
        public static Texture CreateStorage(uint width, uint height, VkFormat format)
        {
            var texture = new Texture
            {
                extent = new VkExtent3D(width, height, 1),
                mipLevels = 1,
                format = format
            };

            var createInfo = new VkImageCreateInfo
            {
                sType = VkStructureType.ImageCreateInfo,
                flags = VkImageCreateFlags.None,
                imageType = VkImageType.Image2D,
                format = format,
                extent = new VkExtent3D(width, height, 1),
                mipLevels = 1,
                arrayLayers = 1,
                samples = VkSampleCountFlags.Count1,
                tiling = VkImageTiling.Optimal,
                usage = VkImageUsageFlags.Storage | VkImageUsageFlags.Sampled,
                sharingMode = VkSharingMode.Exclusive,
                initialLayout = VkImageLayout.Preinitialized
            };

            texture.image = new Image(ref createInfo);

            Graphics.WithCommandBuffer((cmd) =>
            {
                cmd.SetImageLayout(texture.image, VkImageAspectFlags.Color, VkImageLayout.Preinitialized, VkImageLayout.General);
            });

            texture.imageLayout = VkImageLayout.General;
            texture.imageView = ImageView.Create(texture.image, VkImageViewType.Image2D, format, VkImageAspectFlags.Color, 0, texture.mipLevels);
            texture.sampler = new Sampler(VkFilter.Linear, VkSamplerMipmapMode.Linear, VkSamplerAddressMode.Repeat, Device.Features.samplerAnisotropy);
            texture.UpdateDescriptor();
            return texture;
        }
    }

    // for each mipmap_level in numberOfMipmapLevels
    public struct MipmapLevel
    {
        public uint Width { get; }
        public uint Height { get; }
        public uint Depth { get; }
        public uint TotalSize { get; }
        public uint LayerCount { get; }
        public ArrayElement[] ArrayElements { get; }

        public MipmapLevel(uint totalSize, byte[] data, uint width, uint height, uint depth)
        {
            Width = width;
            Height = height;
            Depth = depth;
            TotalSize = totalSize;

            ArrayElements = new[]
            {
                new ArrayElement(new[] { new ImageFace(data) })
            };

            LayerCount = 0;
        }

        public MipmapLevel(uint width, uint height, uint depth, uint totalSize, ArrayElement[] slices, uint numberOfArrayElements)
        {
            Width = width;
            Height = height;
            Depth = depth;
            TotalSize = totalSize;
            ArrayElements = slices;
            LayerCount = numberOfArrayElements;
        }

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
