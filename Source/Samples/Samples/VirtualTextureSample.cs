using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

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

            var allocInfo = VkMemoryAllocateInfo.New();
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
        //Image image;                                                      // Texture image handle
        public BindSparseInfo bindSparseInfo;                                    // Sparse queue binding information
        public Vector<VirtualTexturePage> pages = new Vector<VirtualTexturePage>();                              // Contains all virtual pages of the texture
        Vector<VkSparseImageMemoryBind> sparseImageMemoryBinds = new Vector<VkSparseImageMemoryBind>();   // Sparse image memory bindings of all memory-backed virtual tables
        public Vector<VkSparseMemoryBind> opaqueMemoryBinds = new Vector<VkSparseMemoryBind>();                  // Sparse ópaque memory bindings for the mip tail (if present)
        SparseImageMemoryBindInfo[] imageMemoryBindInfo;                    // Sparse image memory bind info
        SparseImageOpaqueMemoryBindInfo[] opaqueMemoryBindInfo;             // Sparse image opaque memory bind info (mip tail)
        public uint mipTailStart;                                              // First mip level in mip tail
        VkSparseImageMemoryRequirements sparseImageMemoryRequirements; 
        uint memoryTypeIndex;                                           

        // @todo: comment
        public struct MipTailInfo
        {
            public bool singleMipTail;
            public bool alingedMipSize;
        }

        public MipTailInfo mipTailInfo;

        public ref VirtualTexturePage addPage(VkOffset3D offset, VkExtent3D extent, ulong size, uint mipLevel, uint layer)
        {
            VirtualTexturePage newPage = new VirtualTexturePage();
            newPage.offset = offset;
            newPage.extent = extent;
            newPage.size = size;
            newPage.mipLevel = mipLevel;
            newPage.layer = layer;
            newPage.index = (uint)pages.Count;
            newPage.imageMemoryBind = new VkSparseImageMemoryBind();
            newPage.imageMemoryBind.offset = offset;
            newPage.imageMemoryBind.extent = extent;
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

    [SampleDesc(sortOrder = -10)]
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
                        FarClip = 100,
                    },

                },

            };

            camera = scene.GetComponent<Camera>(true);

            var importer = new AssimpModelReader
            {
                vertexComponents = new[]
                {
                    VertexComponent.Position,
                    VertexComponent.Texcoord
                }
            };

            {
                var node = scene.CreateChild("Plane");
                var staticModel = node.AddComponent<StaticModel>();
                var model = GeometryUtil.CreatePlaneModel(100, 100, 32, 32, true);
                staticModel.SetModel(model);
                var mat = Resources.Load<Material>("materials/Grass.material");
                mat.SetTexture("NormalMap", Texture.Blue);
                mat.SetTexture("SpecMap", Texture.Black);
                staticModel.SetMaterial(mat);
            }

            MainView.Attach(camera, scene);

        }


        Uint3 alignedDivision(in VkExtent3D extent, in VkExtent3D granularity)
        {
            Uint3 res = new Uint3();
            res.x = extent.width / granularity.width + ((extent.width % granularity.width != 0) ? 1u : 0u);
            res.y = extent.height / granularity.height + ((extent.height % granularity.height != 0) ? 1u : 0u);
            res.z = extent.depth / granularity.depth + ((extent.depth % granularity.depth != 0) ? 1u : 0u);
            return res;
        }

        uint memoryTypeIndex;
        int lastFilledMip = 0;
        unsafe void prepareSparseTexture(uint width, uint height, uint layerCount, Format format)
        {
            texture = new VirtualTexture();
            texture.width = width;
            texture.height = height;
            texture.mipLevels = (uint)Math.Floor(Math.Log2(Math.Max(width, height))) + 1;
            texture.layers = layerCount;
            texture.format = format;

            var physicalDevice = Device.PhysicalDevice;
            // Get device properites for the requested texture format
            VkFormatProperties formatProperties;
            Device.GetPhysicalDeviceFormatProperties(format, out formatProperties);

            // Get sparse image properties
            Vector<VkSparseImageFormatProperties> sparseProperties = new Vector<VkSparseImageFormatProperties>();
            // Sparse properties count for the desired format
            uint sparsePropertiesCount;
            VulkanNative.vkGetPhysicalDeviceSparseImageFormatProperties(
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
            VulkanNative.vkGetPhysicalDeviceSparseImageFormatProperties(
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
            ImageCreateInfo sparseImageCreateInfo = new ImageCreateInfo();
            sparseImageCreateInfo.imageType = ImageType.Image2D;
            sparseImageCreateInfo.format = texture.format;
            sparseImageCreateInfo.mipLevels = texture.mipLevels;
            sparseImageCreateInfo.arrayLayers = texture.layers;
            sparseImageCreateInfo.samples = SampleCountFlags.Count1;
            sparseImageCreateInfo.tiling = ImageTiling.Optimal;
            sparseImageCreateInfo.sharingMode = SharingMode.Exclusive;
            sparseImageCreateInfo.initialLayout = ImageLayout.Undefined;
            sparseImageCreateInfo.extent = new Extent3D(texture.width, texture.height, 1);
            sparseImageCreateInfo.usage = ImageUsageFlags.TransferDst | ImageUsageFlags.Sampled;
            sparseImageCreateInfo.flags = ImageCreateFlags.SparseBinding | ImageCreateFlags.SparseResidency;
            texture.image = new Image(ref sparseImageCreateInfo);

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
            VulkanNative.vkGetImageSparseMemoryRequirements(Device.Handle, texture.image.handle, &sparseMemoryReqsCount, sparseMemoryReqs.DataPtr);
            if (sparseMemoryReqsCount == 0)
            {
                Log.Error("No memory requirements for the sparse image!");
                return;
            }
            sparseMemoryReqs.Resize(sparseMemoryReqsCount);
            // Get actual requirements
            VulkanNative.vkGetImageSparseMemoryRequirements(Device.Handle, texture.image.handle, &sparseMemoryReqsCount, sparseMemoryReqs.DataPtr);

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

            lastFilledMip = (int)texture.mipTailStart - 1;

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
            memoryTypeIndex = Device.GetMemoryType(sparseImageMemoryReqs.memoryTypeBits, MemoryPropertyFlags.DeviceLocal);

            // Get sparse bindings
            uint sparseBindsCount = (uint)(sparseImageMemoryReqs.size / sparseImageMemoryReqs.alignment);
            Vector<VkSparseMemoryBind> sparseMemoryBinds = new Vector<VkSparseMemoryBind>(sparseBindsCount, sparseBindsCount);

            // Check if the format has a single mip tail for all layers or one mip tail for each layer
            // The mip tail contains all mip levels > sparseMemoryReq.imageMipTailFirstLod
            bool singleMipTail = ((sparseMemoryReq.formatProperties.flags & VkSparseImageFormatFlags.SingleMiptail) != 0);

            // Sparse bindings for each mip level of all layers outside of the mip tail
            for (uint layer = 0; layer < texture.layers; layer++)
            {
                // sparseMemoryReq.imageMipTailFirstLod is the first mip level that's stored inside the mip tail
                for (uint mipLevel = 0; mipLevel < sparseMemoryReq.imageMipTailFirstLod; mipLevel++)
                {
                    VkExtent3D extent;
                    extent.width = Math.Max(sparseImageCreateInfo.extent.width >> (int)mipLevel, 1u);
                    extent.height = Math.Max(sparseImageCreateInfo.extent.height >> (int)mipLevel, 1u);
                    extent.depth = Math.Max(sparseImageCreateInfo.extent.depth >> (int)mipLevel, 1u);

                    VkImageSubresource subResource = new VkImageSubresource();
                    subResource.aspectMask = VkImageAspectFlags.Color;
                    subResource.mipLevel = mipLevel;
                    subResource.arrayLayer = layer;

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
                                VkOffset3D offset;
                                offset.x = (int)(x * imageGranularity.width);
                                offset.y = (int)(y * imageGranularity.height);
                                offset.z = (int)(z * imageGranularity.depth);
                                // Size of the page
                                VkExtent3D extent1 = new VkExtent3D();
                                extent1.width = (x == sparseBindCounts.x - 1) ? lastBlockExtent.x : imageGranularity.width;
                                extent1.height = (y == sparseBindCounts.y - 1) ? lastBlockExtent.y : imageGranularity.height;
                                extent1.depth = (z == sparseBindCounts.z - 1) ? lastBlockExtent.z : imageGranularity.depth;

                                // Add new virtual page
                                ref VirtualTexturePage newPage = ref texture.addPage(offset, extent1, sparseImageMemoryReqs.alignment, mipLevel, layer);
                                newPage.imageMemoryBind.subresource = subResource;

                                if ((x % 2 == 1) || (y % 2 == 1))
                                {
                                    // Allocate memory for this virtual page
                                    //newPage->allocate(device, memoryTypeIndex);
                                }

                                index++;
                            }
                        }
                    }
                }

                // Check if format has one mip tail per layer
                if ((!singleMipTail) && (sparseMemoryReq.imageMipTailFirstLod < texture.mipLevels))
                {
                    // Allocate memory for the mip tail
                    VkMemoryAllocateInfo allocInfo = VkMemoryAllocateInfo.New();
                    allocInfo.allocationSize = sparseMemoryReq.imageMipTailSize;
                    allocInfo.memoryTypeIndex = memoryTypeIndex;

                    VkDeviceMemory deviceMemory = Device.AllocateMemory(ref allocInfo);

                    // (Opaque) sparse memory binding
                    VkSparseMemoryBind sparseMemoryBind = new VkSparseMemoryBind();
                    sparseMemoryBind.resourceOffset = sparseMemoryReq.imageMipTailOffset + layer * sparseMemoryReq.imageMipTailStride;
                    sparseMemoryBind.size = sparseMemoryReq.imageMipTailSize;
                    sparseMemoryBind.memory = deviceMemory;

                    texture.opaqueMemoryBinds.Add(sparseMemoryBind);
                }
            } // end layers and mips

            Log.Info("Texture info:");
            Log.Info("\tDim: " + texture.width + " x " + texture.height);
            Log.Info("\tVirtual pages: " + texture.pages.Count);

            // Check if format has one mip tail for all layers
            if (((sparseMemoryReq.formatProperties.flags & VkSparseImageFormatFlags.SingleMiptail) != 0 ) && (sparseMemoryReq.imageMipTailFirstLod < texture.mipLevels))
            {
                // Allocate memory for the mip tail
                VkMemoryAllocateInfo allocInfo = VkMemoryAllocateInfo.New();
                allocInfo.allocationSize = sparseMemoryReq.imageMipTailSize;
                allocInfo.memoryTypeIndex = memoryTypeIndex;

                VkDeviceMemory deviceMemory;
                deviceMemory = Device.AllocateMemory(ref allocInfo);

                // (Opaque) sparse memory binding
                VkSparseMemoryBind sparseMemoryBind = new VkSparseMemoryBind();
                sparseMemoryBind.resourceOffset = sparseMemoryReq.imageMipTailOffset;
                sparseMemoryBind.size = sparseMemoryReq.imageMipTailSize;
                sparseMemoryBind.memory = deviceMemory;

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
  /*
                     // Create sampler
                     VkSamplerCreateInfo sampler = vks::initializers::samplerCreateInfo();
                     sampler.magFilter = VK_FILTER_LINEAR;
                     sampler.minFilter = VK_FILTER_LINEAR;
                     sampler.mipmapMode = VK_SAMPLER_MIPMAP_MODE_LINEAR;
                     sampler.addressModeU = VK_SAMPLER_ADDRESS_MODE_REPEAT;
                     sampler.addressModeV = VK_SAMPLER_ADDRESS_MODE_REPEAT;
                     sampler.addressModeW = VK_SAMPLER_ADDRESS_MODE_REPEAT;
                     sampler.mipLodBias = 0.0f;
                     sampler.compareOp = VK_COMPARE_OP_NEVER;
                     sampler.minLod = 0.0f;
                     sampler.maxLod = static_cast<float>(texture.mipLevels);
                     sampler.maxAnisotropy = vulkanDevice->features.samplerAnisotropy ? vulkanDevice->properties.limits.maxSamplerAnisotropy : 1.0f;
                     sampler.anisotropyEnable = false;
                     sampler.borderColor = VK_BORDER_COLOR_FLOAT_OPAQUE_WHITE;
                     VK_CHECK_RESULT(vkCreateSampler(device, &sampler, nullptr, &texture.sampler));

                     // Create image view
                     VkImageViewCreateInfo view = vks::initializers::imageViewCreateInfo();
                     view.image = VK_NULL_HANDLE;
                     view.viewType = VK_IMAGE_VIEW_TYPE_2D;
                     view.format = format;
                     view.components = { VK_COMPONENT_SWIZZLE_R, VK_COMPONENT_SWIZZLE_G, VK_COMPONENT_SWIZZLE_B, VK_COMPONENT_SWIZZLE_A };
                     view.subresourceRange.aspectMask = VK_IMAGE_ASPECT_COLOR_BIT;
                     view.subresourceRange.baseMipLevel = 0;
                     view.subresourceRange.baseArrayLayer = 0;
                     view.subresourceRange.layerCount = 1;
                     view.subresourceRange.levelCount = texture.mipLevels;
                     view.image = texture.image;
                     VK_CHECK_RESULT(vkCreateImageView(device, &view, nullptr, &texture.view));

                     // Fill image descriptor image info that can be used during the descriptor set setup
                     texture.descriptor.imageLayout = VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL;
                     texture.descriptor.imageView = texture.view;
                     texture.descriptor.sampler = texture.sampler;

                     // Fill smallest (non-tail) mip map level
                     fillVirtualTexture(lastFilledMip);*/
        }

        // Clear all pages of the virtual texture
        // todo: just for testing
        void flushVirtualTexture()
        {
            Device.WaitIdle();

//             for (ref var page in texture.pages)
//             {
//                 page.release(device);
//             }

            texture.updateSparseBindInfo();
            queue.BindSparse(texture.bindSparseInfo);

            //todo: use sparse bind semaphore
            queue.WaitIdle();
            lastFilledMip = (int)texture.mipTailStart - 1;
        }

        Texture textures_source;
        Vector<VkImageBlit> imageBlits = new Vector<VkImageBlit>();
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
                    page.allocate(memoryTypeIndex);

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


//                 auto tEnd = std::chrono::high_resolution_clock::now();
//                 auto tDiff = std::chrono::duration<double, std::milli>(tEnd - tStart).count();
//                 std::cout << "Image blits took " << tDiff << " ms" << std::endl;
            }

            queue.WaitIdle();

            mipLevel--;
        }
    }


}
