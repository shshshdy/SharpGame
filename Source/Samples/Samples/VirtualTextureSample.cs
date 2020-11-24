using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame.Samples
{
    // Virtual texture page as a part of the partially resident texture
    // Contains memory bindings, offsets and status information
    public struct VirtualTexturePage : IDisposable
    {
        public VkOffset3D offset;
        public VkExtent3D extent;
        public VkSparseImageMemoryBind imageMemoryBind;                            // Sparse image memory bind for this page
        public ulong size;                                                  // Page (memory) size in bytes
        public uint mipLevel;                                                  // Mip level that this page belongs to
        public uint layer;                                                     // Array layer that this page belongs to
        public uint index;

        public bool resident()
        {
            return (imageMemoryBind.memory != VkDeviceMemory.Null);
        }

        public void allocate(uint memoryTypeIndex)
        {
            if (imageMemoryBind.memory != VkDeviceMemory.Null)
            {
                return;
            }

            imageMemoryBind = new VkSparseImageMemoryBind();

            var allocInfo = new VkMemoryAllocateInfo();
            allocInfo.sType = VkStructureType.MemoryAllocateInfo;
            allocInfo.allocationSize = size;
            allocInfo.memoryTypeIndex = memoryTypeIndex;
            imageMemoryBind.memory = Device.AllocateMemory(ref allocInfo);

            // Sparse image memory binding
            imageMemoryBind.subresource = new VkImageSubresource
            {
                aspectMask = VkImageAspectFlags.Color,
                mipLevel = mipLevel,
                arrayLayer = layer,
            };

            imageMemoryBind.extent = extent;
            imageMemoryBind.offset = offset;
        }

        public void Dispose()
        {
            if (imageMemoryBind.memory != VkDeviceMemory.Null)
            {
                Device.FreeMemory(imageMemoryBind.memory);
                imageMemoryBind.memory = VkDeviceMemory.Null;
            }
        }
    }

    // Virtual texture object containing all pages
    public class VirtualTexture : Texture
    {
        public BindSparseInfo bindSparseInfo;                                    // Sparse queue binding information
        public Vector<VirtualTexturePage> pages = new Vector<VirtualTexturePage>();                              // Contains all virtual pages of the texture
        Vector<VkSparseImageMemoryBind> sparseImageMemoryBinds = new Vector<VkSparseImageMemoryBind>();   // Sparse image memory bindings of all memory-backed virtual tables
        public Vector<VkSparseMemoryBind> opaqueMemoryBinds = new Vector<VkSparseMemoryBind>();                  // Sparse ópaque memory bindings for the mip tail (if present)
        SparseImageMemoryBindInfo[] imageMemoryBindInfo;                    // Sparse image memory bind info
        SparseImageOpaqueMemoryBindInfo[] opaqueMemoryBindInfo;             // Sparse image opaque memory bind info (mip tail)
        public uint mipTailStart;                                              // First mip level in mip tail
        public VkSparseImageMemoryRequirements sparseImageMemoryRequirements;
        public uint memoryTypeIndex;

        public struct MipTailInfo
        {
            public bool singleMipTail;
            public bool alingedMipSize;
        }

        public MipTailInfo mipTailInfo;

        public ref VirtualTexturePage addPage(VkOffset3D offset, VkExtent3D extent, ulong size, uint mipLevel, uint layer)
        {
            VirtualTexturePage newPage = new VirtualTexturePage
            {
                offset = offset,
                extent = extent,
                size = size,
                mipLevel = mipLevel,
                layer = layer,
                index = (uint)pages.Count,
                imageMemoryBind = new VkSparseImageMemoryBind()
                {
                    offset = offset,
                    extent = extent,
                }
            };

            pages.Add(newPage);
            return ref pages.Back();

        }

        public void updateSparseBindInfo()
        {
            // Update list of memory-backed sparse image memory binds
            sparseImageMemoryBinds.Clear();
            foreach (var page in pages)
            {
                sparseImageMemoryBinds.Add(page.imageMemoryBind);
            }

            // Image memory binds
            imageMemoryBindInfo = new[] { new SparseImageMemoryBindInfo(image, sparseImageMemoryBinds) };

            // Opaque image memory binds for the mip tail
            opaqueMemoryBindInfo = new[] { new SparseImageOpaqueMemoryBindInfo(image, opaqueMemoryBinds) };

            bindSparseInfo = new BindSparseInfo(null, null, opaqueMemoryBindInfo, imageMemoryBindInfo, null);
        }
        protected override void Destroy(bool disposing)
        {
            foreach (var page in pages)
            {
                page.Dispose();
            }

            foreach (var bind in opaqueMemoryBinds)
            {
                Device.FreeMemory(bind.memory);
            }

            base.Destroy(disposing);
        }
    }

    [SampleDesc(sortOrder = 10)]
    public class VirtualTextureSample : Sample
    {
        VirtualTexture texture;
        Semaphore bindSparseSemaphore;

        Queue queue;

        public override void Init()
        {
            queue = Graphics.WorkQueue;

            scene = new Scene()
            {
                new Octree { },
                new DebugRenderer { },

                new Environment
                {
                    SunlightDir = glm.normalize(new vec3(-1.0f, -1.0f, 0.0f))
                },

                new Node("Camera", new vec3(0, 2, -10), glm.radians(10, 0, 0) )
                {
                    new Camera
                    {
                        NearClip = 0.5f,
                        FarClip = 400,
                    },

                },

            };

            camera = scene.GetComponent<Camera>(true);

            prepareSparseTexture(4096, 4096, 1, Format.R8g8b8a8Unorm);           

            {
                var node = scene.CreateChild("Plane");
                var staticModel = node.AddComponent<StaticModel>();
                var model = GeometryUtil.CreatePlaneModel(100, 100, 1, 1);
                staticModel.SetModel(model);

                var mat = new Material("shaders/VirtualTexture.shader");
                mat.SetTexture("samplerColor", texture);
                staticModel.SetMaterial(mat);
            }

            MainView.Attach(camera, scene);

        }

        protected override void Destroy(bool disposing)
        {
            texture.Dispose();

            base.Destroy(disposing);
        }

        Uint3 alignedDivision(in VkExtent3D extent, in VkExtent3D granularity)
        {
            return new Uint3
            {
                x = extent.width / granularity.width + ((extent.width % granularity.width != 0) ? 1u : 0u),
                y = extent.height / granularity.height + ((extent.height % granularity.height != 0) ? 1u : 0u),
                z = extent.depth / granularity.depth + ((extent.depth % granularity.depth != 0) ? 1u : 0u)
            };
        }

        unsafe void prepareSparseTexture(uint width, uint height, uint layerCount, Format format)
        {
            texture = new VirtualTexture
            {
                width = width,
                height = height,
                mipLevels = (uint)Math.Floor(Math.Log2(Math.Max(width, height))) + 1,
                layers = layerCount,
                format = format
            };

            var physicalDevice = Device.PhysicalDevice;
            // Get device properites for the requested texture format
            VkFormatProperties formatProperties;
            Device.GetPhysicalDeviceFormatProperties(format, out formatProperties);

            // Get sparse image properties
            Vector<VkSparseImageFormatProperties> sparseProperties = new Vector<VkSparseImageFormatProperties>();
            // Sparse properties count for the desired format
            uint sparsePropertiesCount;
            Vulkan.vkGetPhysicalDeviceSparseImageFormatProperties(
                physicalDevice,
                (VkFormat)format,
                VkImageType.Image2D,
                VkSampleCountFlags.Count1,
                VkImageUsageFlags.Sampled,
                VkImageTiling.Optimal,
                &sparsePropertiesCount,
                null);
            // Check if sparse is supported for this format
            if (sparsePropertiesCount == 0)
            {
                Log.Error("Requested format does not support sparse features!");
                return;
            }

            // Get actual image format properties
            sparseProperties.Resize(sparsePropertiesCount);
            Vulkan.vkGetPhysicalDeviceSparseImageFormatProperties(
                physicalDevice,
                (VkFormat)format,
                VkImageType.Image2D,
                VkSampleCountFlags.Count1,
                VkImageUsageFlags.Sampled,
                VkImageTiling.Optimal,
                &sparsePropertiesCount,
                sparseProperties.DataPtr);

            Log.Info("Sparse image format properties: " + sparsePropertiesCount);
            foreach (var props in sparseProperties)
            {
                Log.Info("\t Image granularity: w = " + props.imageGranularity.width + " h = " + props.imageGranularity.height + " d = " + props.imageGranularity.depth);
                Log.Info("\t Aspect mask: " + props.aspectMask);
                Log.Info("\t Flags: " + props.flags);
            }

            // Create sparse image
            VkImageCreateInfo sparseImageCreateInfo = new VkImageCreateInfo
            {
                sType = VkStructureType.ImageCreateInfo
            };
            sparseImageCreateInfo.imageType = VkImageType.Image2D;
            sparseImageCreateInfo.format = (VkFormat)texture.format;
            sparseImageCreateInfo.mipLevels = texture.mipLevels;
            sparseImageCreateInfo.arrayLayers = texture.layers;
            sparseImageCreateInfo.samples = VkSampleCountFlags.Count1;
            sparseImageCreateInfo.tiling = VkImageTiling.Optimal;
            sparseImageCreateInfo.sharingMode = VkSharingMode.Exclusive;
            sparseImageCreateInfo.initialLayout = VkImageLayout.Undefined;
            sparseImageCreateInfo.extent = new VkExtent3D(texture.width, texture.height, 1);
            sparseImageCreateInfo.usage = VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled;
            sparseImageCreateInfo.flags = VkImageCreateFlags.SparseBinding | VkImageCreateFlags.SparseResidency;

            var img = Device.CreateImage(ref sparseImageCreateInfo);
            texture.image = new Image(img);

            Graphics.WithCommandBuffer((cmd) =>
            {
                cmd.SetImageLayout(texture.image, VkImageAspectFlags.Color, ImageLayout.Undefined, ImageLayout.ShaderReadOnlyOptimal);
            });

            // Get memory requirements
            VkMemoryRequirements sparseImageMemoryReqs;
            // Sparse image memory requirement counts
            Device.GetImageMemoryRequirements(texture.image, out sparseImageMemoryReqs);

            Log.Info("Image memory requirements:");
            Log.Info("\t Size: " + sparseImageMemoryReqs.size);
            Log.Info("\t Alignment: " + sparseImageMemoryReqs.alignment);

            // Check requested image size against hardware sparse limit
            if (sparseImageMemoryReqs.size > Device.Properties.limits.sparseAddressSpaceSize)
            {
                Log.Error("Requested sparse image size exceeds supportes sparse address space size!");
                return;
            };

            // Get sparse memory requirements
            // Count
            uint sparseMemoryReqsCount = 32;
            Vector<VkSparseImageMemoryRequirements> sparseMemoryReqs = new Vector<VkSparseImageMemoryRequirements>(sparseMemoryReqsCount, sparseMemoryReqsCount);
            Vulkan.vkGetImageSparseMemoryRequirements(Device.Handle, texture.image.handle, &sparseMemoryReqsCount, sparseMemoryReqs.DataPtr);
            if (sparseMemoryReqsCount == 0)
            {
                Log.Error("No memory requirements for the sparse image!");
                return;
            }
            sparseMemoryReqs.Resize(sparseMemoryReqsCount);
            // Get actual requirements
            Vulkan.vkGetImageSparseMemoryRequirements(Device.Handle, texture.image.handle, &sparseMemoryReqsCount, sparseMemoryReqs.DataPtr);

            Log.Info("Sparse image memory requirements: " + sparseMemoryReqsCount);
            foreach (var reqs in sparseMemoryReqs)
            {
                Log.Info("\t Image granularity: w = " + reqs.formatProperties.imageGranularity.width + " h = " + reqs.formatProperties.imageGranularity.height + " d = " + reqs.formatProperties.imageGranularity.depth);
                Log.Info("\t Mip tail first LOD: " + reqs.imageMipTailFirstLod);
                Log.Info("\t Mip tail size: " + reqs.imageMipTailSize);
                Log.Info("\t Mip tail offset: " + reqs.imageMipTailOffset);
                Log.Info("\t Mip tail stride: " + reqs.imageMipTailStride);
                //todo:multiple reqs
                texture.mipTailStart = reqs.imageMipTailFirstLod;
            }

            // Get sparse image requirements for the color aspect
            VkSparseImageMemoryRequirements sparseMemoryReq = new VkSparseImageMemoryRequirements();
            bool colorAspectFound = false;
            foreach (var reqs in sparseMemoryReqs)
            {
                if ((reqs.formatProperties.aspectMask & VkImageAspectFlags.Color) != 0)
                {
                    sparseMemoryReq = reqs;
                    colorAspectFound = true;
                    break;
                }
            }
            if (!colorAspectFound)
            {
                Log.Error("Could not find sparse image memory requirements for color aspect bit!");
                return;
            }

            // todo:
            // Calculate number of required sparse memory bindings by alignment
            Debug.Assert((sparseImageMemoryReqs.size % sparseImageMemoryReqs.alignment) == 0);
            texture.memoryTypeIndex = Device.GetMemoryType(sparseImageMemoryReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

            // Get sparse bindings
            uint sparseBindsCount = (uint)(sparseImageMemoryReqs.size / sparseImageMemoryReqs.alignment);
            Vector<VkSparseMemoryBind> sparseMemoryBinds = new Vector<VkSparseMemoryBind>(sparseBindsCount, sparseBindsCount);

            texture.sparseImageMemoryRequirements = sparseMemoryReq;

            // The mip tail contains all mip levels > sparseMemoryReq.imageMipTailFirstLod
            // Check if the format has a single mip tail for all layers or one mip tail for each layer
            // @todo: Comment
            texture.mipTailInfo.singleMipTail = (sparseMemoryReq.formatProperties.flags & VkSparseImageFormatFlags.SingleMiptail) != 0;
            texture.mipTailInfo.alingedMipSize = (sparseMemoryReq.formatProperties.flags & VkSparseImageFormatFlags.AlignedMipSize) != 0;

            // Sparse bindings for each mip level of all layers outside of the mip tail
            for (uint layer = 0; layer < texture.layers; layer++)
            {
                // sparseMemoryReq.imageMipTailFirstLod is the first mip level that's stored inside the mip tail
                for (uint mipLevel = 0; mipLevel < sparseMemoryReq.imageMipTailFirstLod; mipLevel++)
                {
                    VkExtent3D extent = new VkExtent3D
                    (
                        Math.Max(sparseImageCreateInfo.extent.width >> (int)mipLevel, 1u),
                        Math.Max(sparseImageCreateInfo.extent.height >> (int)mipLevel, 1u),
                        Math.Max(sparseImageCreateInfo.extent.depth >> (int)mipLevel, 1u)
                    );

                    VkImageSubresource subResource = new VkImageSubresource
                    {
                        aspectMask = VkImageAspectFlags.Color,
                        mipLevel = mipLevel,
                        arrayLayer = layer
                    };

                    // Aligned sizes by image granularity
                    VkExtent3D imageGranularity = sparseMemoryReq.formatProperties.imageGranularity;
                    Uint3 sparseBindCounts = alignedDivision(extent, imageGranularity);
                    Uint3 lastBlockExtent;
                    lastBlockExtent.x = (extent.width % imageGranularity.width) != 0 ? extent.width % imageGranularity.width : imageGranularity.width;
                    lastBlockExtent.y = (extent.height % imageGranularity.height) != 0 ? extent.height % imageGranularity.height : imageGranularity.height;
                    lastBlockExtent.z = (extent.depth % imageGranularity.depth) != 0 ? extent.depth % imageGranularity.depth : imageGranularity.depth;

                    // Alllocate memory for some blocks
                    uint index = 0;
                    for (uint z = 0; z < sparseBindCounts.z; z++)
                    {
                        for (uint y = 0; y < sparseBindCounts.y; y++)
                        {
                            for (uint x = 0; x < sparseBindCounts.x; x++)
                            {
                                // Offset 
                                VkOffset3D offset = new VkOffset3D
                                (
                                    (int)(x * imageGranularity.width),
                                    (int)(y * imageGranularity.height),
                                    (int)(z * imageGranularity.depth)
                                );
                                // Size of the page
                                VkExtent3D extent1 = new VkExtent3D
                                (
                                    (x == sparseBindCounts.x - 1) ? lastBlockExtent.x : imageGranularity.width,
                                    (y == sparseBindCounts.y - 1) ? lastBlockExtent.y : imageGranularity.height,
                                    (z == sparseBindCounts.z - 1) ? lastBlockExtent.z : imageGranularity.depth
                                );

                                // Add new virtual page
                                ref VirtualTexturePage newPage = ref texture.addPage(offset, extent1, sparseImageMemoryReqs.alignment, mipLevel, layer);
                                newPage.imageMemoryBind.subresource = subResource;

                                index++;
                            }
                        }
                    }
                }

                // Check if format has one mip tail per layer
                if ((!texture.mipTailInfo.singleMipTail) && (sparseMemoryReq.imageMipTailFirstLod < texture.mipLevels))
                {
                    // Allocate memory for the mip tail
                    VkMemoryAllocateInfo allocInfo = new VkMemoryAllocateInfo();
                    allocInfo.sType = VkStructureType.MemoryAllocateInfo;
                    allocInfo.allocationSize = sparseMemoryReq.imageMipTailSize;
                    allocInfo.memoryTypeIndex = texture.memoryTypeIndex;

                    VkDeviceMemory deviceMemory = Device.AllocateMemory(ref allocInfo);

                    // (Opaque) sparse memory binding
                    VkSparseMemoryBind sparseMemoryBind = new VkSparseMemoryBind
                    {
                        resourceOffset = sparseMemoryReq.imageMipTailOffset + layer * sparseMemoryReq.imageMipTailStride,
                        size = sparseMemoryReq.imageMipTailSize,
                        memory = deviceMemory
                    };

                    texture.opaqueMemoryBinds.Add(sparseMemoryBind);
                }
            } // end layers and mips

            Log.Info("Texture info:");
            Log.Info("\tDim: " + texture.width + " x " + texture.height);
            Log.Info("\tVirtual pages: " + texture.pages.Count);

            // Check if format has one mip tail for all layers
            if (((sparseMemoryReq.formatProperties.flags & VkSparseImageFormatFlags.SingleMiptail) != 0) && (sparseMemoryReq.imageMipTailFirstLod < texture.mipLevels))
            {
                // Allocate memory for the mip tail
                VkMemoryAllocateInfo allocInfo = new VkMemoryAllocateInfo();
                allocInfo.sType = VkStructureType.MemoryAllocateInfo;
                allocInfo.allocationSize = sparseMemoryReq.imageMipTailSize;
                allocInfo.memoryTypeIndex = texture.memoryTypeIndex;

                VkDeviceMemory deviceMemory = Device.AllocateMemory(ref allocInfo);

                // (Opaque) sparse memory binding
                VkSparseMemoryBind sparseMemoryBind = new VkSparseMemoryBind
                {
                    resourceOffset = sparseMemoryReq.imageMipTailOffset,
                    size = sparseMemoryReq.imageMipTailSize,
                    memory = deviceMemory
                };

                texture.opaqueMemoryBinds.Add(sparseMemoryBind);
            }

            bindSparseSemaphore = new Semaphore(0);

            // Prepare bind sparse info for reuse in queue submission
            texture.updateSparseBindInfo();

            // Bind to queue
            // todo: in draw?
            queue.BindSparse(texture.bindSparseInfo);
            //todo: use sparse bind semaphore
            queue.WaitIdle();

            texture.sampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.Repeat, false);
            texture.imageView = ImageView.Create(texture.image, ImageViewType.Image2D, format, VkImageAspectFlags.Color, 0, texture.mipLevels);
            texture.UpdateDescriptor();
        }

        unsafe void uploadContent(VirtualTexturePage page, Image image)
        {
            // Generate some random image data and upload as a buffer
            ulong bufferSize = 4 * page.extent.width * page.extent.height;

            Buffer imageBuffer = Buffer.CreateStagingBuffer(bufferSize, IntPtr.Zero);
            imageBuffer.Map();

            // Fill buffer with random colors
            byte* data = (byte*)imageBuffer.Mapped;
            byte[] rndVal = new byte[] { 0, 0, 0, 0 };
            while (rndVal[0] + rndVal[1] + rndVal[2] < 10)
            {
                rndVal[0] = (byte)glm.random(0, 255);
                rndVal[1] = (byte)glm.random(0, 255);
                rndVal[2] = (byte)glm.random(0, 255);
            }
            rndVal[3] = 255;

            for (uint y = 0; y < page.extent.height; y++)
            {
                for (uint x = 0; x < page.extent.width; x++)
                {
                    for (uint c = 0; c < 4; c++, ++data)
                    {
                        *data = rndVal[c];
                    }
                }
            }

            var copyCmd = Graphics.BeginPrimaryCmd();
            copyCmd.SetImageLayout(image, VkImageAspectFlags.Color, ImageLayout.ShaderReadOnlyOptimal, ImageLayout.TransferDstOptimal, PipelineStageFlags.TopOfPipe, PipelineStageFlags.Transfer);
            VkBufferImageCopy region = new VkBufferImageCopy();
            region.imageSubresource.aspectMask = VkImageAspectFlags.Color;
            region.imageSubresource.layerCount = 1;
            region.imageSubresource.mipLevel = page.mipLevel;
            region.imageOffset = page.offset;
            region.imageExtent = page.extent;
            copyCmd.CopyBufferToImage(imageBuffer, image, ImageLayout.TransferDstOptimal, ref region);
            copyCmd.SetImageLayout(image, VkImageAspectFlags.Color, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, PipelineStageFlags.Transfer, PipelineStageFlags.FragmentShader);

            Graphics.EndPrimaryCmd(copyCmd);

            imageBuffer.Release();
        }

        Vector<VirtualTexturePage> updatedPages = new Vector<VirtualTexturePage>();
        void fillRandomPages()
        {
            Device.WaitIdle();

            updatedPages.Clear();

            for (int i = 0; i < texture.pages.Count; i++)
            {
                ref var page = ref texture.pages[i];
                if (glm.random() < 0.5f)
                {
                    continue;
                }
                page.allocate(texture.memoryTypeIndex);
                updatedPages.Add(page);
            }

            // Update sparse queue binding
            texture.updateSparseBindInfo();

            Fence fence = new Fence(FenceCreateFlags.None);

            queue.BindSparse(texture.bindSparseInfo, fence);
            fence.Wait();

            for (int i = 0; i < updatedPages.Count; i++)
            {
                var page = updatedPages[i];
                uploadContent(page, texture.image);
            }
        }

        unsafe void fillMipTail()
        {
            //@todo: WIP
            ulong imageMipTailSize = texture.sparseImageMemoryRequirements.imageMipTailSize;
            ulong imageMipTailOffset = texture.sparseImageMemoryRequirements.imageMipTailOffset;
            // Stride between memory bindings for each mip level if not single mip tail (VK_SPARSE_IMAGE_FORMAT_SINGLE_MIPTAIL_BIT not set)
            ulong imageMipTailStride = texture.sparseImageMemoryRequirements.imageMipTailStride;

            VkSparseImageMemoryBind mipTailimageMemoryBind = new VkSparseImageMemoryBind();

            VkMemoryAllocateInfo allocInfo = new VkMemoryAllocateInfo();
            allocInfo.sType = VkStructureType.MemoryAllocateInfo;
            allocInfo.allocationSize = imageMipTailSize;
            allocInfo.memoryTypeIndex = texture.memoryTypeIndex;
            mipTailimageMemoryBind.memory = Device.AllocateMemory(ref allocInfo);

            uint mipLevel = texture.sparseImageMemoryRequirements.imageMipTailFirstLod;
            uint width = Math.Max(texture.width >> (int)texture.sparseImageMemoryRequirements.imageMipTailFirstLod, 1u);
            uint height = Math.Max(texture.height >> (int)texture.sparseImageMemoryRequirements.imageMipTailFirstLod, 1u);
            uint depth = 1;

            byte* rndVal = stackalloc byte[4] { 0, 0, 0, 0 };
            for (uint i = texture.mipTailStart; i < texture.mipLevels; i++)
            {
                width = Math.Max(texture.width >> (int)i, 1u);
                height = Math.Max(texture.height >> (int)i, 1u);

                // Generate some random image data and upload as a buffer
                ulong bufferSize = 4 * width * height;

                Buffer imageBuffer = Buffer.CreateStagingBuffer(bufferSize, IntPtr.Zero);
                imageBuffer.Map();

                // Fill buffer with random colors
                byte* data = (byte*)imageBuffer.Mapped;
                while (rndVal[0] + rndVal[1] + rndVal[2] < 10)
                {
                    rndVal[0] = (byte)glm.random(0, 255);
                    rndVal[1] = (byte)glm.random(0, 255);
                    rndVal[2] = (byte)glm.random(0, 255);
                }
                rndVal[3] = 255;

                switch (mipLevel)
                {
                    case 0:
                        rndVal[0] = rndVal[1] = rndVal[2] = 255;
                        break;
                    case 1:
                        rndVal[0] = rndVal[1] = rndVal[2] = 200;
                        break;
                    case 2:
                        rndVal[0] = rndVal[1] = rndVal[2] = 150;
                        break;
                }

                for (uint y = 0; y < height; y++)
                {
                    for (uint x = 0; x < width; x++)
                    {
                        for (uint c = 0; c < 4; c++, ++data)
                        {
                            *data = rndVal[c];
                        }
                    }
                }

                var copyCmd = Graphics.BeginPrimaryCmd();
                copyCmd.SetImageLayout(texture.image, VkImageAspectFlags.Color, ImageLayout.ShaderReadOnlyOptimal, ImageLayout.TransferDstOptimal, PipelineStageFlags.TopOfPipe, PipelineStageFlags.Transfer);
                var region = new VkBufferImageCopy();
                region.imageSubresource.aspectMask = VkImageAspectFlags.Color;
                region.imageSubresource.layerCount = 1;
                region.imageSubresource.mipLevel = i;
                region.imageOffset = VkOffset3D.Zero;
                region.imageExtent = new VkExtent3D(width, height, depth);
                copyCmd.CopyBufferToImage(imageBuffer, texture.image, ImageLayout.TransferDstOptimal, ref region);
                copyCmd.SetImageLayout(texture.image, VkImageAspectFlags.Color, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, PipelineStageFlags.Transfer, PipelineStageFlags.FragmentShader);

                Graphics.EndPrimaryCmd(copyCmd);

                imageBuffer.Release();
            }
        }

        void flushRandomPages()
        {
            Device.WaitIdle();

            for (int i = 0; i < texture.pages.Count; i++)
            {
                if (glm.random() < 0.5f)
                    texture.pages[i].Dispose();
            }

            // Update sparse queue binding
            texture.updateSparseBindInfo();

            Fence fence = new Fence(FenceCreateFlags.None);
            queue.BindSparse(texture.bindSparseInfo, fence);
            fence.Wait();
        }

        public override void OnGUI()
        {
            base.OnGUI();

            if (ImGui.Begin("HUD"))
            {

                //                 if (ImGui.DragFloat("LOD bias", &uboVS.lodBias, -(float)texture.mipLevels, (float)texture.mipLevels))
                //                 {
                //                     updateUniformBuffers();
                //                 }

                if (ImGui.Button("Fill random pages"))
                {
                    fillRandomPages();
                }
                if (ImGui.Button("Flush random pages"))
                {
                    flushRandomPages();
                }
                if (ImGui.Button("Fill mip tail"))
                {
                    fillMipTail();
                }
            }
        }
#if false
    // Clear all pages of the virtual texture
    // todo: just for testing
    void flushVirtualTexture()
        {
            Device.WaitIdle();

            for (int i = 0; i < texture.pages.Count; i++)
            {
                texture.pages[i].Dispose();
            }

            texture.updateSparseBindInfo();
            queue.BindSparse(texture.bindSparseInfo);

            //todo: use sparse bind semaphore
            queue.WaitIdle();
            lastFilledMip = (int)texture.mipTailStart - 1;
        }

        Texture textures_source;
        Vector<VkImageBlit> imageBlits = new Vector<VkImageBlit>();
        Timer timer = new Timer();
        // Fill a complete mip level
        void fillVirtualTexture(ref int mipLevel)
        {
            Device.WaitIdle();

            imageBlits.Clear();

            for (int i = 0; i < texture.pages.Count; i++)
            { 
                ref var page = ref texture.pages[i];
                if ((page.mipLevel == mipLevel) && /*(rndDist(rndEngine) < 0.5f) &&*/ (page.imageMemoryBind.memory == VkDeviceMemory.Null))
                {
                    // Allocate page memory
                    page.allocate(texture.memoryTypeIndex);

                    // Current mip level scaling
                    uint scale = texture.width / (texture.width >> (int)page.mipLevel);

                    for (uint x = 0; x < scale; x++)
                    {
                        for (uint y = 0; y < scale; y++)
                        {
                            // Image blit
                            VkImageBlit blit = new VkImageBlit();
                            // Source
                            blit.srcSubresource.aspectMask = VkImageAspectFlags.Color;
                            blit.srcSubresource.baseArrayLayer = 0;
                            blit.srcSubresource.layerCount = 1;
                            blit.srcSubresource.mipLevel = 0;
                            blit.srcOffsets_0 = new VkOffset3D(/*0, 0, 0*/);
                            blit.srcOffsets_1 = new VkOffset3D { x = (int)(textures_source.width), y = (int)(textures_source.height), z = 1 };
                            // Dest
                            blit.dstSubresource.aspectMask = VkImageAspectFlags.Color;
                            blit.dstSubresource.baseArrayLayer = 0;
                            blit.dstSubresource.layerCount = 1;
                            blit.dstSubresource.mipLevel = page.mipLevel;
                            blit.dstOffsets_0.x = (int)(page.offset.x + x * 128 / scale);
                            blit.dstOffsets_0.y = (int)(page.offset.y + y * 128 / scale);
                            blit.dstOffsets_0.z = 0;
                            blit.dstOffsets_1.x = (int)(blit.dstOffsets_0.x + page.extent.width / scale);
                            blit.dstOffsets_1.y = (int)(blit.dstOffsets_0.y + page.extent.height / scale);
                            blit.dstOffsets_1.z = 1;

                            imageBlits.Add(blit);
                        }
                    }
                }
            }

            // Update sparse queue binding
            texture.updateSparseBindInfo();
            queue.BindSparse(texture.bindSparseInfo);
            //todo: use sparse bind semaphore
            queue.WaitIdle();

            // Issue blit commands
            if (imageBlits.Count > 0)
            {
                //var tStart = std::chrono::high_resolution_clock::now();
                timer.Restart();

                unsafe
                {
                    Graphics.WithCommandBuffer((copyCmd) =>
                    {
                        copyCmd.BlitImage(
                            textures_source.image,
                            ImageLayout.TransferSrcOptimal,
                            texture.image,
                            ImageLayout.TransferDstOptimal,
                            new Span<ImageBlit>(imageBlits.DataPtr, (int)imageBlits.Count),
                            Filter.Linear
                        );

                    });
                }

                timer.Stop();
                Log.Info("Image blits took " + timer.ElapsedMilliseconds + " ms");
            }

            queue.WaitIdle();

            mipLevel--;
        }
#endif
    }


}
