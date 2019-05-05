using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
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

    public class Texture : Resource
    {
        public Format Format { get; set; }

        [IgnoreDataMember]
        public Image Image { get; protected set; }

        [IgnoreDataMember]
        public ImageView View { get; protected set; }

        [IgnoreDataMember]
        public DeviceMemory Memory { get; protected set; }

        [IgnoreDataMember]
        public Sampler Sampler { get; set; }

        public Texture()
        {
        }

        internal Texture(Image image, DeviceMemory memory, ImageView view, Format format)
        {
            Image = image;
            Memory = memory;
            View = view;
            Format = format;
        }

        public Texture(TextureData textureData)
        {
            SetTextureData(textureData);
        }

        public override void Dispose()
        {
            View.Dispose();
            Memory.Dispose();
            Image.Dispose();
        }

        public async override Task<bool> Load(File stream)
        {
            var graphics = Get<Graphics>();
            var fileSystem = Get<FileSystem>();
            using (var reader = new BinaryReader(stream.Stream))
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

                SetTextureData(data);
                Sampler = graphics.CreateSampler();
            }

            return true;
        }

        public void SetTextureData(TextureData tex2D)
        {
            var graphics = Get<Graphics>();
            Buffer stagingBuffer = graphics.Device.CreateBuffer(
                new BufferCreateInfo(tex2D.Mipmaps[0].Size, BufferUsages.TransferSrc));
            MemoryRequirements stagingMemReq = stagingBuffer.GetMemoryRequirements();
            int heapIndex = graphics.MemoryProperties.MemoryTypes.IndexOf(
                stagingMemReq.MemoryTypeBits, MemoryProperties.HostVisible);
            DeviceMemory stagingMemory = graphics.Device.AllocateMemory(
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
            var image = graphics.Device.CreateImage(new ImageCreateInfo
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
            int imageHeapIndex = graphics.MemoryProperties.MemoryTypes.IndexOf(
                imageMemReq.MemoryTypeBits, MemoryProperties.DeviceLocal);

            var memory = graphics.Device.AllocateMemory(new MemoryAllocateInfo(imageMemReq.Size, imageHeapIndex));
            image.BindMemory(memory);

            var subresourceRange = new ImageSubresourceRange(ImageAspects.Color, 0, tex2D.Mipmaps.Length, 0, 1);

            // Copy the data from staging buffers to device local buffers.
            CommandBuffer cmdBuffer = graphics.GraphicsCommandPool.AllocateBuffers(new CommandBufferAllocateInfo(CommandBufferLevel.Primary, 1))[0];
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
            Fence fence = graphics.Device.CreateFence();
            graphics.GraphicsQueue.Submit(new SubmitInfo(commandBuffers: new[] { cmdBuffer }), fence);
            fence.Wait();

            // Cleanup staging resources.
            fence.Dispose();
            stagingMemory.Dispose();
            stagingBuffer.Dispose();

            View = image.CreateView(new ImageViewCreateInfo(tex2D.Format, subresourceRange));
            Image = image;
            Memory = memory;
            Format = tex2D.Format;
        }


        public static Texture Create2D(TextureData tex2D)
        {
            return new Texture(tex2D);
        }

        public static Texture Create2D(int width, int height, int bytes_per_pixel, IntPtr pixels)
        {            
            var tex = new Texture();

            return tex;
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

    }
}
