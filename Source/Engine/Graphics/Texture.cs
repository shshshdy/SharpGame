using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VulkanCore;
using Buffer = VulkanCore.Buffer;

namespace SharpGame
{
    public class TextureData
    {
        public Mipmap[] Mipmaps { get; set; }
        public Format Format { get; set; }

        public class Mipmap
        {
            public byte[] Data { get; set; }
            public Extent3D Extent { get; set; }
            public int Size { get; set; }
        }
    }

    public class Texture : IDisposable
    {
        private Texture(Image image, DeviceMemory memory, ImageView view, Format format)
        {
            Image = image;
            Memory = memory;
            View = view;
            Format = format;
        }

        public Format Format { get; protected set; }

        public Image Image { get; protected set; }
        public ImageView View { get; protected set; }
        public DeviceMemory Memory { get; protected set; }

        public void Dispose()
        {
            View.Dispose();
            Memory.Dispose();
            Image.Dispose();
        }

        public static implicit operator Image(Texture value) => value.Image;

        public static Texture DepthStencil(Graphics device, int width, int height)
        {
            Format[] validFormats =
            {
                Format.D32SFloatS8UInt,
                Format.D32SFloat,
                Format.D24UNormS8UInt,
                Format.D16UNormS8UInt,
                Format.D16UNorm
            };

            Format? potentialFormat = validFormats.FirstOrDefault(
                validFormat =>
                {
                    FormatProperties formatProps = device.PhysicalDevice.GetFormatProperties(validFormat);
                    return (formatProps.OptimalTilingFeatures & FormatFeatures.DepthStencilAttachment) > 0;
                });

            if (!potentialFormat.HasValue)
                throw new InvalidOperationException("Required depth stencil format not supported.");

            Format format = potentialFormat.Value;

            Image image = device.Device.CreateImage(new ImageCreateInfo
            {
                ImageType = ImageType.Image2D,
                Format = format,
                Extent = new Extent3D(width, height, 1),
                MipLevels = 1,
                ArrayLayers = 1,
                Samples = SampleCounts.Count1,
                Tiling = ImageTiling.Optimal,
                Usage = ImageUsages.DepthStencilAttachment | ImageUsages.TransferSrc
            });
            MemoryRequirements memReq = image.GetMemoryRequirements();
            int heapIndex = device.MemoryProperties.MemoryTypes.IndexOf(
                memReq.MemoryTypeBits, MemoryProperties.DeviceLocal);
            DeviceMemory memory = device.Device.AllocateMemory(new MemoryAllocateInfo(memReq.Size, heapIndex));
            image.BindMemory(memory);
            ImageView view = image.CreateView(new ImageViewCreateInfo(format,
                new ImageSubresourceRange(ImageAspects.Depth | ImageAspects.Stencil, 0, 1, 0, 1)));

            return new Texture(image, memory, view, format);
        }

        internal static Texture Texture2D(Graphics ctx, TextureData tex2D)
        {
            Buffer stagingBuffer = ctx.Device.CreateBuffer(
                new BufferCreateInfo(tex2D.Mipmaps[0].Size, BufferUsages.TransferSrc));
            MemoryRequirements stagingMemReq = stagingBuffer.GetMemoryRequirements();
            int heapIndex = ctx.MemoryProperties.MemoryTypes.IndexOf(
                stagingMemReq.MemoryTypeBits, MemoryProperties.HostVisible);
            DeviceMemory stagingMemory = ctx.Device.AllocateMemory(
                new MemoryAllocateInfo(stagingMemReq.Size, heapIndex));
            stagingBuffer.BindMemory(stagingMemory);

            IntPtr ptr = stagingMemory.Map(0, stagingMemReq.Size);
            Interop.Write(ptr, tex2D.Mipmaps[0].Data);
            stagingMemory.Unmap();

            // Setup buffer copy regions for each mip level.
            var bufferCopyRegions = new BufferImageCopy[tex2D.Mipmaps.Length];
            int offset = 0;
            for (int i = 0; i < bufferCopyRegions.Length; i++)
            {
                bufferCopyRegions = new[]
                {
                    new BufferImageCopy
                    {
                        ImageSubresource = new ImageSubresourceLayers(ImageAspects.Color, i, 0, 1),
                        ImageExtent = tex2D.Mipmaps[0].Extent,
                        BufferOffset = offset
                    }
                };
                offset += tex2D.Mipmaps[i].Size;
            }

            // Create optimal tiled target image.
            Image image = ctx.Device.CreateImage(new ImageCreateInfo
            {
                ImageType = ImageType.Image2D,
                Format = tex2D.Format,
                MipLevels = tex2D.Mipmaps.Length,
                ArrayLayers = 1,
                Samples = SampleCounts.Count1,
                Tiling = ImageTiling.Optimal,
                SharingMode = SharingMode.Exclusive,
                InitialLayout = ImageLayout.Undefined,
                Extent = tex2D.Mipmaps[0].Extent,
                Usage = ImageUsages.Sampled | ImageUsages.TransferDst
            });
            MemoryRequirements imageMemReq = image.GetMemoryRequirements();
            int imageHeapIndex = ctx.MemoryProperties.MemoryTypes.IndexOf(
                imageMemReq.MemoryTypeBits, MemoryProperties.DeviceLocal);
            DeviceMemory memory = ctx.Device.AllocateMemory(new MemoryAllocateInfo(imageMemReq.Size, imageHeapIndex));
            image.BindMemory(memory);

            var subresourceRange = new ImageSubresourceRange(ImageAspects.Color, 0, tex2D.Mipmaps.Length, 0, 1);

            // Copy the data from staging buffers to device local buffers.
            CommandBuffer cmdBuffer = ctx.GraphicsCommandPool.AllocateBuffers(new CommandBufferAllocateInfo(CommandBufferLevel.Primary, 1))[0];
            cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.OneTimeSubmit));
            cmdBuffer.CmdPipelineBarrier(PipelineStages.TopOfPipe, PipelineStages.Transfer,
                imageMemoryBarriers: new[]
                {
                    new ImageMemoryBarrier(
                        image, subresourceRange,
                        0, Accesses.TransferWrite,
                        ImageLayout.Undefined, ImageLayout.TransferDstOptimal)
                });
            cmdBuffer.CmdCopyBufferToImage(stagingBuffer, image, ImageLayout.TransferDstOptimal, bufferCopyRegions);
            cmdBuffer.CmdPipelineBarrier(PipelineStages.Transfer, PipelineStages.FragmentShader,
                imageMemoryBarriers: new[]
                {
                    new ImageMemoryBarrier(
                        image, subresourceRange,
                        Accesses.TransferWrite, Accesses.ShaderRead,
                        ImageLayout.TransferDstOptimal, ImageLayout.General/*ImageLayout.ShaderReadOnlyOptimal*/)
                });
            cmdBuffer.End();

            // Submit.
            Fence fence = ctx.Device.CreateFence();
            ctx.GraphicsQueue.Submit(new SubmitInfo(commandBuffers: new[] { cmdBuffer }), fence);
            fence.Wait();

            // Cleanup staging resources.
            fence.Dispose();
            stagingMemory.Dispose();
            stagingBuffer.Dispose();

            // Create image view.
            ImageView view = image.CreateView(new ImageViewCreateInfo(tex2D.Format, subresourceRange));

            return new Texture(image, memory, view, tex2D.Format);
        }


        // Ktx spec: https://www.khronos.org/opengles/sdk/tools/KTX/file_format_spec/

        private static readonly byte[] KtxIdentifier =
        {
            0xAB, 0x4B, 0x54, 0x58, 0x20, 0x31, 0x31, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A
        };

        // OpenGL internal color formats are described in table 8.12 at
        // https://khronos.org/registry/OpenGL/specs/gl/glspec44.core.pdf
        private static readonly Dictionary<int, Format> _glInternalFormatToVkFormat = new Dictionary<int, Format>()
        {
            [32855] = Format.R5G5B5A1UNormPack16,
            [32856] = Format.R8G8B8A8UNorm
        };

        public static Texture Load(IPlatform host, Graphics ctx, string path)
        {
            using (var reader = new BinaryReader(host.Open(path)))
            {
                byte[] identifier = reader.ReadBytes(12);

                if (!identifier.SequenceEqual(KtxIdentifier))
                    throw new InvalidOperationException("File is not in Khronos Texture format.");

                int endienness = reader.ReadInt32();
                int glType = reader.ReadInt32();
                int glTypeSize = reader.ReadInt32();
                int glFormat = reader.ReadInt32();
                int glInternalFormat = reader.ReadInt32();
                int glBaseInternalFormat = reader.ReadInt32();
                int pixelWidth = reader.ReadInt32();
                int pixelHeight = reader.ReadInt32();
                int pixelDepth = reader.ReadInt32();
                int numberOfArrayElements = reader.ReadInt32();
                int numberOfFaces = reader.ReadInt32();
                int numberOfMipmapLevels = reader.ReadInt32();
                int bytesOfKeyValueData = reader.ReadInt32();

                // Skip key-value data.
                reader.ReadBytes(bytesOfKeyValueData);

                // Some of the values may be 0 - ensure at least 1.
                pixelWidth = Math.Max(pixelWidth, 1);
                pixelHeight = Math.Max(pixelHeight, 1);
                pixelDepth = Math.Max(pixelDepth, 1);
                numberOfArrayElements = Math.Max(numberOfArrayElements, 1);
                numberOfFaces = Math.Max(numberOfFaces, 1);
                numberOfMipmapLevels = Math.Max(numberOfMipmapLevels, 1);

                int numberOfSlices = Math.Max(numberOfFaces, numberOfArrayElements);

                if (!_glInternalFormatToVkFormat.TryGetValue(glInternalFormat, out Format format))
                    throw new NotImplementedException("glInternalFormat not mapped to VkFormat.");

                var data = new TextureData
                {
                    Mipmaps = new TextureData.Mipmap[numberOfMipmapLevels],
                    Format = format
                };

                for (int i = 0; i < numberOfMipmapLevels; i++)
                {
                    var mipmap = new TextureData.Mipmap
                    {
                        Size = reader.ReadInt32(),
                        Extent = new Extent3D(pixelWidth, pixelHeight, pixelDepth)
                    };
                    mipmap.Data = reader.ReadBytes(mipmap.Size);
                    data.Mipmaps[i] = mipmap;

                    break; // TODO: impl
                    //for (int j = 0; j < numberOfArrayElements; j++)
                    //{
                    //    for (int k = 0; k < numberOfFaces; k++)
                    //    {
                    //        for (int l = 0; l < pixelDepth; l++)
                    //        {
                    //            //for (int row = 0;
                    //            //    row < )
                    //        }
                    //    }
                    //}
                }

                return Texture.Texture2D(ctx, data);
            }
        }
    }
}
