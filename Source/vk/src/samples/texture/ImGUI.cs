// This code has been adapted from the "Vulkan" C++ example repository, by Sascha Willems: https://github.com/SaschaWillems/Vulkan
// It is a direct translation from the original C++ code and style, with as little transformation as possible.

// Original file: texture/texture.cpp, 

/*
* Vulkan Example - Texture loading (and display) example (including mip maps)
*
* Copyright (C) 2016 by Sascha Willems - www.saschawillems.de
*
* This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
*/

using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;
using Veldrid.Sdl2;
using Vulkan;
using static Vulkan.VulkanNative;

namespace SharpGame
{
    // ImVertex layout for this example
    public struct ImVertex
    {
        public Vector2 pos;
        public Vector2 uv;
        public uint color;

        public const uint PositionOffset = 0;
        public const uint UvOffset = 8;
        public const uint ColorOffset = 16;
    };

    public unsafe class ImGUI : Application, IDisposable
    {
        // Contains all Vulkan objects that are required to store and use a texture
        // Note that this repository contains a texture class (VulkanTexture.hpp) that encapsulates texture loading functionality in a class that is used in subsequent demos
        public struct Texture
        {
            public VkSampler sampler;
            public VkImage image;
            public VkImageLayout imageLayout;
            public VkDeviceMemory DeviceMemory;
            public VkImageView view;
            public uint width, height;
            public uint mipLevels;
        }

        Texture texture;

        public class Vertices
        {
            public VkPipelineVertexInputStateCreateInfo inputState;
            public NativeList<VkVertexInputBindingDescription> bindingDescriptions = new NativeList<VkVertexInputBindingDescription>();
            public NativeList<VkVertexInputAttributeDescription> attributeDescriptions = new NativeList<VkVertexInputAttributeDescription>();
        }

        Vertices vertices = new Vertices();

        GraphicsBuffer vertexBuffer = new GraphicsBuffer();
        GraphicsBuffer indexBuffer = new GraphicsBuffer();

        GraphicsBuffer uniformBufferVS = new GraphicsBuffer();

        public struct UboVS
        {
            public Matrix4x4 projection;
        }

        UboVS uboVS;
        VkPipeline pipelines_solid;

        VkPipelineLayout pipelineLayout;
        VkDescriptorSet descriptorSet;
        VkDescriptorSetLayout descriptorSetLayout;
        private const uint VERTEX_BUFFER_BIND_ID = 0;

        ImGUI()
        {
            zoom = -2.5f;
            rotation = new Vector3(0.0f, 15.0f, 0.0f);
            Title = "Vulkan Example - Texture loading";
            // enableTextOverlay = true;
        }

        public void Dispose()
        {
            // Clean up used Vulkan resources 
            // Note : Inherited destructor cleans up resources stored in base class

            destroyTextureImage(texture);

            vkDestroyPipeline(device, pipelines_solid, null);

            vkDestroyPipelineLayout(device, pipelineLayout, null);
            vkDestroyDescriptorSetLayout(device, descriptorSetLayout, null);

            vertexBuffer.destroy();
            indexBuffer.destroy();
            uniformBufferVS.destroy();
        }

        Texture createTexture(uint w, uint h, uint bytesPerPixel, byte* tex2DDataPtr)
        {
            VkFormat format = VkFormat.R8g8b8a8Unorm;
            VkFormatProperties formatProperties;
            texture = new Texture
            {
                width = w,
                height = h,
                mipLevels = 1
            };

            uint totalBytes = bytesPerPixel * w * h;

            // Get Device properites for the requested texture format
            vkGetPhysicalDeviceFormatProperties(physicalDevice, format, &formatProperties);

            // Only use linear tiling if requested (and supported by the Device)
            // Support for linear tiling is mostly limited, so prefer to use
            // optimal tiling instead
            // On most implementations linear tiling will only support a very
            // limited amount of formats and features (mip maps, cubemaps, arrays, etc.)
            uint useStaging = 1;
            
            VkMemoryAllocateInfo memAllocInfo = Initializers.memoryAllocateInfo();
            VkMemoryRequirements memReqs = new VkMemoryRequirements();

            if (useStaging == 1)
            {
                // Create a host-visible staging buffer that contains the raw image data
                VkBuffer stagingBuffer;
                VkDeviceMemory stagingMemory;

                VkBufferCreateInfo bufferCreateInfo = Initializers.bufferCreateInfo();
                bufferCreateInfo.size = totalBytes;
                // This buffer is used as a transfer source for the buffer copy
                bufferCreateInfo.usage = VkBufferUsageFlags.TransferSrc;
                bufferCreateInfo.sharingMode = VkSharingMode.Exclusive;

                Util.CheckResult(vkCreateBuffer(device, &bufferCreateInfo, null, &stagingBuffer));

                // Get memory requirements for the staging buffer (alignment, memory type bits)
                vkGetBufferMemoryRequirements(device, stagingBuffer, &memReqs);

                memAllocInfo.allocationSize = memReqs.size;
                // Get memory type index for a host visible buffer
                memAllocInfo.memoryTypeIndex = vulkanDevice.getMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);

                Util.CheckResult(vkAllocateMemory(device, &memAllocInfo, null, &stagingMemory));
                Util.CheckResult(vkBindBufferMemory(device, stagingBuffer, stagingMemory, 0));

                // Copy texture data into staging buffer
                byte* data;
                Util.CheckResult(vkMapMemory(device, stagingMemory, 0, memReqs.size, 0, (void**)&data));
                Unsafe.CopyBlock(data, tex2DDataPtr, totalBytes);               
                vkUnmapMemory(device, stagingMemory);

                // Setup buffer copy regions for each mip level
                NativeList<VkBufferImageCopy> bufferCopyRegions = new NativeList<VkBufferImageCopy>();
                uint offset = 0;

                for (uint i = 0; i < texture.mipLevels; i++)
                {
                    VkBufferImageCopy bufferCopyRegion = new VkBufferImageCopy();
                    bufferCopyRegion.imageSubresource.aspectMask = VkImageAspectFlags.Color;
                    bufferCopyRegion.imageSubresource.mipLevel = i;
                    bufferCopyRegion.imageSubresource.baseArrayLayer = 0;
                    bufferCopyRegion.imageSubresource.layerCount = 1;
                    bufferCopyRegion.imageExtent.width = w;// tex2D.Faces[0].Mipmaps[i].Width;
                    bufferCopyRegion.imageExtent.height = h;// tex2D.Faces[0].Mipmaps[i].Height;
                    bufferCopyRegion.imageExtent.depth = 1;
                    bufferCopyRegion.bufferOffset = offset;

                    bufferCopyRegions.Add(bufferCopyRegion);

                 //   offset += tex2D.Faces[0].Mipmaps[i].SizeInBytes;
                }

                // Create optimal tiled target image
                VkImageCreateInfo imageCreateInfo = Initializers.imageCreateInfo();
                imageCreateInfo.imageType = VkImageType.Image2D;
                imageCreateInfo.format = format;
                imageCreateInfo.mipLevels = texture.mipLevels;
                imageCreateInfo.arrayLayers = 1;
                imageCreateInfo.samples = VkSampleCountFlags.Count1;
                imageCreateInfo.tiling = VkImageTiling.Optimal;
                imageCreateInfo.sharingMode = VkSharingMode.Exclusive;
                // Set initial layout of the image to undefined
                imageCreateInfo.initialLayout = VkImageLayout.Undefined;
                imageCreateInfo.extent = new VkExtent3D { width = texture.width, height = texture.height, depth = 1 };
                imageCreateInfo.usage = VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled;

                Util.CheckResult(vkCreateImage(device, &imageCreateInfo, null, out texture.image));

                vkGetImageMemoryRequirements(device, texture.image, &memReqs);

                memAllocInfo.allocationSize = memReqs.size;
                memAllocInfo.memoryTypeIndex = vulkanDevice.getMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

                Util.CheckResult(vkAllocateMemory(device, &memAllocInfo, null, out texture.DeviceMemory));
                Util.CheckResult(vkBindImageMemory(device, texture.image, texture.DeviceMemory, 0));

                VkCommandBuffer copyCmd = base.createCommandBuffer(VkCommandBufferLevel.Primary, true);

                // Image barrier for optimal image

                // The sub resource range describes the regions of the image we will be transition
                VkImageSubresourceRange subresourceRange = new VkImageSubresourceRange();
                // Image only contains color data
                subresourceRange.aspectMask = VkImageAspectFlags.Color;
                // Start at first mip level
                subresourceRange.baseMipLevel = 0;
                // We will transition on all mip levels
                subresourceRange.levelCount = texture.mipLevels;
                // The 2D texture only has one layer
                subresourceRange.layerCount = 1;

                // Optimal image will be used as destination for the copy, so we must transfer from our
                // initial undefined image layout to the transfer destination layout
                setImageLayout(
                    copyCmd,
                    texture.image,
                     VkImageAspectFlags.Color,
                     VkImageLayout.Undefined,
                     VkImageLayout.TransferDstOptimal,
                    subresourceRange);

                // Copy mip levels from staging buffer
                vkCmdCopyBufferToImage(
                    copyCmd,
                    stagingBuffer,
                    texture.image,
                     VkImageLayout.TransferDstOptimal,
                    bufferCopyRegions.Count,
                    bufferCopyRegions.Data);

                // Change texture image layout to shader read after all mip levels have been copied
                texture.imageLayout = VkImageLayout.ShaderReadOnlyOptimal;
                setImageLayout(
                    copyCmd,
                    texture.image,
                    VkImageAspectFlags.Color,
                    VkImageLayout.TransferDstOptimal,
                    texture.imageLayout,
                    subresourceRange);

                flushCommandBuffer(copyCmd, queue, true);

                // Clean up staging resources
                vkFreeMemory(device, stagingMemory, null);
                vkDestroyBuffer(device, stagingBuffer, null);
            }
            
            // Create sampler
            // In Vulkan textures are accessed by samplers
            // This separates all the sampling information from the 
            // texture data
            // This means you could have multiple sampler objects
            // for the same texture with different settings
            // Similar to the samplers available with OpenGL 3.3
            VkSamplerCreateInfo sampler = Initializers.samplerCreateInfo();
            sampler.magFilter = VkFilter.Linear;
            sampler.minFilter = VkFilter.Linear;
            sampler.mipmapMode = VkSamplerMipmapMode.Linear;
            sampler.addressModeU = VkSamplerAddressMode.Repeat;
            sampler.addressModeV = VkSamplerAddressMode.Repeat;
            sampler.addressModeW = VkSamplerAddressMode.Repeat;
            sampler.mipLodBias = 0.0f;
            sampler.compareOp = VkCompareOp.Never;
            sampler.minLod = 0.0f;
            // Set max level-of-detail to mip level count of the texture
            sampler.maxLod = (useStaging == 1) ? (float)texture.mipLevels : 0.0f;
            // Enable anisotropic filtering
            // This feature is optional, so we must check if it's supported on the Device
            if (vulkanDevice.features.samplerAnisotropy == 1)
            {
                // Use max. level of anisotropy for this example
                sampler.maxAnisotropy = vulkanDevice.properties.limits.maxSamplerAnisotropy;
                sampler.anisotropyEnable = True;
            }
            else
            {
                // The Device does not support anisotropic filtering
                sampler.maxAnisotropy = 1.0f;
                sampler.anisotropyEnable = False;
            }
            sampler.borderColor = VkBorderColor.FloatOpaqueWhite;
            Util.CheckResult(vkCreateSampler(device, ref sampler, null, out texture.sampler));

            // Create image view
            // Textures are not directly accessed by the shaders and
            // are abstracted by image views containing additional
            // information and sub resource ranges
            VkImageViewCreateInfo view = Initializers.imageViewCreateInfo();
            view.viewType = VkImageViewType.Image2D;
            view.format = format;
            view.components = new VkComponentMapping { r = VkComponentSwizzle.R, g = VkComponentSwizzle.G, b = VkComponentSwizzle.B, a = VkComponentSwizzle.A };
            // The subresource range describes the set of mip levels (and array layers) that can be accessed through this image view
            // It's possible to create multiple image views for a single image referring to different (and/or overlapping) ranges of the image
            view.subresourceRange.aspectMask = VkImageAspectFlags.Color;
            view.subresourceRange.baseMipLevel = 0;
            view.subresourceRange.baseArrayLayer = 0;
            view.subresourceRange.layerCount = 1;
            // Linear tiling usually won't support mip maps
            // Only set mip map count if optimal tiling is used
            view.subresourceRange.levelCount = (useStaging == 1) ? texture.mipLevels : 1;
            // The view will be based on the texture's image
            view.image = texture.image;
            Util.CheckResult(vkCreateImageView(device, &view, null, out texture.view));
            return texture;
        }

        // Create an image memory barrier for changing the layout of
        // an image and put it into an active command buffer
        void setImageLayout(
            VkCommandBuffer cmdBuffer,
            VkImage image,
            VkImageAspectFlags aspectMask,
            VkImageLayout oldImageLayout,
            VkImageLayout newImageLayout,
            VkImageSubresourceRange subresourceRange)
        {
            // Create an image barrier object
            VkImageMemoryBarrier imageMemoryBarrier = Initializers.imageMemoryBarrier(); ;
            imageMemoryBarrier.oldLayout = oldImageLayout;
            imageMemoryBarrier.newLayout = newImageLayout;
            imageMemoryBarrier.image = image;
            imageMemoryBarrier.subresourceRange = subresourceRange;

            // Only sets masks for layouts used in this example
            // For a more complete version that can be used with other layouts see vks::tools::setImageLayout

            // Source layouts (old)
            switch (oldImageLayout)
            {
                case VkImageLayout.Undefined:
                    // Only valid as initial layout, memory contents are not preserved
                    // Can be accessed directly, no source dependency required
                    imageMemoryBarrier.srcAccessMask = 0;
                    break;
                case VkImageLayout.Preinitialized:
                    // Only valid as initial layout for linear images, preserves memory contents
                    // Make sure host writes to the image have been finished
                    imageMemoryBarrier.srcAccessMask = VkAccessFlags.HostWrite;
                    break;
                case VkImageLayout.TransferDstOptimal:
                    // Old layout is transfer destination
                    // Make sure any writes to the image have been finished
                    imageMemoryBarrier.srcAccessMask = VkAccessFlags.TransferWrite;
                    break;
            }

            // Target layouts (new)
            switch (newImageLayout)
            {
                case VkImageLayout.TransferSrcOptimal:
                    // Transfer source (copy, blit)
                    // Make sure any reads from the image have been finished
                    imageMemoryBarrier.dstAccessMask = VkAccessFlags.TransferRead;
                    break;
                case VkImageLayout.TransferDstOptimal:
                    // Transfer destination (copy, blit)
                    // Make sure any writes to the image have been finished
                    imageMemoryBarrier.dstAccessMask = VkAccessFlags.TransferWrite;
                    break;
                case VkImageLayout.ShaderReadOnlyOptimal:
                    // Shader read (sampler, input attachment)
                    imageMemoryBarrier.dstAccessMask = VkAccessFlags.ShaderRead;
                    break;
            }

            // Put barrier on top of pipeline
            VkPipelineStageFlags srcStageFlags = VkPipelineStageFlags.TopOfPipe;
            VkPipelineStageFlags destStageFlags = VkPipelineStageFlags.TopOfPipe;

            // Put barrier inside setup command buffer
            vkCmdPipelineBarrier(
                cmdBuffer,
                srcStageFlags,
                destStageFlags,
                VkDependencyFlags.None,
                0, null,
                0, null,
                1, &imageMemoryBarrier);
        }

        // Free all Vulkan resources used a texture object
        void destroyTextureImage(Texture texture)
        {
            vkDestroyImageView(device, texture.view, null);
            vkDestroyImage(device, texture.image, null);
            vkDestroySampler(device, texture.sampler, null);
            vkFreeMemory(device, texture.DeviceMemory, null);
        }

        void generateQuad()
        {
            /*
            // Setup vertices for a single uv-mapped quad made from two triangles
            NativeList<ImVertex> vertices = new NativeList<ImVertex>()
            {
                new ImVertex() { pos = new Vector3(1.0f,  1.0f, 0.0f), uv = new Vector2(1.0f, 1.0f), normal = new Vector3(0.0f, 0.0f, 1.0f) },
                new ImVertex() { pos = new Vector3(-1.0f,  1.0f, 0.0f), uv = new Vector2(0.0f, 1.0f), normal = new Vector3(0.0f, 0.0f, 1.0f) },
                new ImVertex() { pos = new Vector3(-1.0f, -1.0f, 0.0f), uv = new Vector2(0.0f, 0.0f), normal = new Vector3(0.0f, 0.0f, 1.0f) },
                new ImVertex() { pos = new Vector3(1.0f, -1.0f, 0.0f), uv = new Vector2(1.0f, 0.0f), normal = new Vector3(0.0f, 0.0f, 1.0f) },
            };

            // Setup indices
            NativeList<uint> indices = new NativeList<uint> { 0, 1, 2, 2, 3, 0 };
            indexCount = indices.Count;*/

            // Create buffers
            // For the sake of simplicity we won't stage the vertex data to the gpu memory
            // ImVertex buffer
            Util.CheckResult(vulkanDevice.createBuffer(
                VkBufferUsageFlags.VertexBuffer,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                vertexBuffer,
                (ulong)(4096 * sizeof(ImVertex)),
                null));
            // Index buffer
            Util.CheckResult(vulkanDevice.createBuffer(
                VkBufferUsageFlags.IndexBuffer,
                VkMemoryPropertyFlags.HostCoherent | VkMemoryPropertyFlags.HostCoherent,
                indexBuffer,
                4096 * sizeof(ushort),
                null));
        }

        void setupVertexDescriptions()
        {
            // Binding description
            vertices.bindingDescriptions.Count = 1;
            vertices.bindingDescriptions[0] =
                Initializers.vertexInputBindingDescription(
                    VERTEX_BUFFER_BIND_ID,
                    (uint)sizeof(ImVertex),
                    VkVertexInputRate.Vertex);

            // Attribute descriptions
            // Describes memory layout and shader positions
            vertices.attributeDescriptions.Count = 3;
            // Location 0 : Position
            vertices.attributeDescriptions[0] =
                Initializers.vertexInputAttributeDescription(
                    VERTEX_BUFFER_BIND_ID,
                    0,
                    VkFormat.R32g32Sfloat,
                    ImVertex.PositionOffset);
            // Location 1 : Texture coordinates
            vertices.attributeDescriptions[1] =
                Initializers.vertexInputAttributeDescription(
                    VERTEX_BUFFER_BIND_ID,
                    1,
                    VkFormat.R32g32Sfloat,
                    ImVertex.UvOffset);
            // Location 1 : ImVertex normal
            vertices.attributeDescriptions[2] =
                Initializers.vertexInputAttributeDescription(
                    VERTEX_BUFFER_BIND_ID,
                    2,
                    VkFormat.R8g8b8a8Unorm,
                    ImVertex.ColorOffset);

            vertices.inputState = Initializers.pipelineVertexInputStateCreateInfo();
            vertices.inputState.vertexBindingDescriptionCount = vertices.bindingDescriptions.Count;
            vertices.inputState.pVertexBindingDescriptions = (VkVertexInputBindingDescription*)vertices.bindingDescriptions.Data;
            vertices.inputState.vertexAttributeDescriptionCount = vertices.attributeDescriptions.Count;
            vertices.inputState.pVertexAttributeDescriptions = (VkVertexInputAttributeDescription*)vertices.attributeDescriptions.Data;
        }

        void setupDescriptorPool()
        {
            // Example uses one ubo and one image sampler
            FixedArray2<VkDescriptorPoolSize> poolSizes = new FixedArray2<VkDescriptorPoolSize>(
                    Initializers.descriptorPoolSize(VkDescriptorType.UniformBuffer, 1),
                    Initializers.descriptorPoolSize(VkDescriptorType.CombinedImageSampler, 1)
            );

            VkDescriptorPoolCreateInfo descriptorPoolInfo =
                Initializers.descriptorPoolCreateInfo(
                    poolSizes.Count,
                    (VkDescriptorPoolSize*)Unsafe.AsPointer(ref poolSizes),
                    2);

            Util.CheckResult(vkCreateDescriptorPool(device, &descriptorPoolInfo, null, out descriptorPool));
        }

        void setupDescriptorSetLayout()
        {
            FixedArray2<VkDescriptorSetLayoutBinding> setLayoutBindings = new FixedArray2<VkDescriptorSetLayoutBinding>(
                // Binding 0 : ImVertex shader uniform buffer
                Initializers.descriptorSetLayoutBinding(
                    VkDescriptorType.UniformBuffer,
                    VkShaderStageFlags.Vertex,
                    0),
                // Binding 1 : Fragment shader image sampler
                Initializers.descriptorSetLayoutBinding(
                    VkDescriptorType.CombinedImageSampler,
                    VkShaderStageFlags.Fragment,
                    1)
            );

            VkDescriptorSetLayoutCreateInfo descriptorLayout =
                Initializers.descriptorSetLayoutCreateInfo(
                    (VkDescriptorSetLayoutBinding*)Unsafe.AsPointer(ref setLayoutBindings),
                    setLayoutBindings.Count);

            Util.CheckResult(vkCreateDescriptorSetLayout(device, &descriptorLayout, null, out descriptorSetLayout));

            var layout = descriptorSetLayout;
            VkPipelineLayoutCreateInfo pPipelineLayoutCreateInfo =
                Initializers.pipelineLayoutCreateInfo(
                    &layout,
                    1);

            Util.CheckResult(vkCreatePipelineLayout(device, &pPipelineLayoutCreateInfo, null, out pipelineLayout));
        }

        void setupDescriptorSet()
        {
            var layout = descriptorSetLayout;
            VkDescriptorSetAllocateInfo allocInfo =
                Initializers.descriptorSetAllocateInfo(
                    descriptorPool,
                    &layout,
                    1);

            Util.CheckResult(vkAllocateDescriptorSets(device, &allocInfo, out descriptorSet));

            // Setup a descriptor image info for the current texture to be used as a combined image sampler
            VkDescriptorImageInfo textureDescriptor;
            textureDescriptor.imageView = texture.view;             // The image's view (images are never directly accessed by the shader, but rather through views defining subresources)
            textureDescriptor.sampler = texture.sampler;            //	The sampler (Telling the pipeline how to sample the texture, including repeat, border, etc.)
            textureDescriptor.imageLayout = texture.imageLayout;    //	The current layout of the image (Note: Should always fit the actual use, e.g. shader read)

            var descriptor = uniformBufferVS.descriptor;
            FixedArray2<VkWriteDescriptorSet> writeDescriptorSets = new FixedArray2<VkWriteDescriptorSet>(
                    // Binding 0 : ImVertex shader uniform buffer
                    Initializers.writeDescriptorSet(
                        descriptorSet,
                        VkDescriptorType.UniformBuffer,
                        0,
                        &descriptor),
                    // Binding 1 : Fragment shader texture sampler
                    //	Fragment shader: layout (binding = 1) uniform sampler2D samplerColor;
                    Initializers.writeDescriptorSet(
                        descriptorSet,
                        VkDescriptorType.CombinedImageSampler,          // The descriptor set will use a combined image sampler (sampler and image could be split)
                        1,                                                  // Shader binding point 1
                        &textureDescriptor)								// Pointer to the descriptor image for our texture
            );

            vkUpdateDescriptorSets(device, writeDescriptorSets.Count, ref writeDescriptorSets.First, 0, null);
        }

        void preparePipelines()
        {
            VkPipelineInputAssemblyStateCreateInfo inputAssemblyState =
                Initializers.pipelineInputAssemblyStateCreateInfo(
                    VkPrimitiveTopology.TriangleList,
                    0,
                    False);

            VkPipelineRasterizationStateCreateInfo rasterizationState =
                Initializers.pipelineRasterizationStateCreateInfo(
                    VkPolygonMode.Fill,
                    VkCullModeFlags.None,
                    VkFrontFace.CounterClockwise,
                    0);

            VkPipelineColorBlendAttachmentState blendAttachmentState =
                Initializers.pipelineColorBlendAttachmentState(
                    (VkColorComponentFlags)0xf, True);
            blendAttachmentState.alphaBlendOp = VkBlendOp.Add;
            blendAttachmentState.colorBlendOp = VkBlendOp.Add;
            blendAttachmentState.srcColorBlendFactor = VkBlendFactor.SrcAlpha;
            blendAttachmentState.dstColorBlendFactor = VkBlendFactor.OneMinusDstAlpha;
            blendAttachmentState.srcAlphaBlendFactor = VkBlendFactor.One;
            blendAttachmentState.dstAlphaBlendFactor = VkBlendFactor.Zero;

            VkPipelineColorBlendStateCreateInfo colorBlendState =
                Initializers.pipelineColorBlendStateCreateInfo(
                    1,
                    &blendAttachmentState);

            VkPipelineDepthStencilStateCreateInfo depthStencilState =
                Initializers.pipelineDepthStencilStateCreateInfo(
                    False,
                    False,
                    VkCompareOp.LessOrEqual);

            VkPipelineViewportStateCreateInfo viewportState =
                Initializers.pipelineViewportStateCreateInfo(1, 1, 0);

            VkPipelineMultisampleStateCreateInfo multisampleState =
                Initializers.pipelineMultisampleStateCreateInfo(
                    VkSampleCountFlags.Count1,
                    0);

            FixedArray2<VkDynamicState> dynamicStateEnables = new FixedArray2<VkDynamicState>(
                VkDynamicState.Viewport,
                VkDynamicState.Scissor);
            VkPipelineDynamicStateCreateInfo dynamicState =
                Initializers.pipelineDynamicStateCreateInfo(
                    (VkDynamicState*)Unsafe.AsPointer(ref dynamicStateEnables),
                    dynamicStateEnables.Count,
                    0);

            // Load shaders
            FixedArray2<VkPipelineShaderStageCreateInfo> shaderStages = new FixedArray2<VkPipelineShaderStageCreateInfo>();

            shaderStages.First = loadShader(getAssetPath() + "shaders/texture/ImGui.vert.spv", VkShaderStageFlags.Vertex);
            shaderStages.Second = loadShader(getAssetPath() + "shaders/texture/ImGui.frag.spv", VkShaderStageFlags.Fragment);

            VkGraphicsPipelineCreateInfo pipelineCreateInfo =
                Initializers.pipelineCreateInfo(
                    pipelineLayout,
                    renderPass,
                    0);

            var vertexInputState = vertices.inputState;
            pipelineCreateInfo.pVertexInputState = &vertexInputState;
            pipelineCreateInfo.pInputAssemblyState = &inputAssemblyState;
            pipelineCreateInfo.pRasterizationState = &rasterizationState;
            pipelineCreateInfo.pColorBlendState = &colorBlendState;
            pipelineCreateInfo.pMultisampleState = &multisampleState;
            pipelineCreateInfo.pViewportState = &viewportState;
            pipelineCreateInfo.pDepthStencilState = &depthStencilState;
            pipelineCreateInfo.pDynamicState = &dynamicState;
            pipelineCreateInfo.stageCount = shaderStages.Count;
            pipelineCreateInfo.pStages = (VkPipelineShaderStageCreateInfo*)Unsafe.AsPointer(ref shaderStages);

            Util.CheckResult(vkCreateGraphicsPipelines(device, pipelineCache, 1, &pipelineCreateInfo, null, out pipelines_solid));
        }

        // Prepare and initialize uniform buffer containing shader uniforms
        void prepareUniformBuffers()
        {
            var localUboVS = uboVS;
            // ImVertex shader uniform buffer block
            Util.CheckResult(vulkanDevice.createBuffer(
                VkBufferUsageFlags.UniformBuffer,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                uniformBufferVS,
                (uint)sizeof(UboVS),
                &localUboVS));

            updateUniformBuffers();
        }

        void updateUniformBuffers()
        {
            uboVS.projection = Matrix4x4.CreateOrthographicOffCenter(
                     0f,
                     width,
                     height,
                     0.0f,
                     -1.0f,
                     1.0f);

            Util.CheckResult(uniformBufferVS.map());
            var local = uboVS;
            Unsafe.CopyBlock(uniformBufferVS.mapped, &local, (uint)sizeof(UboVS));
            uniformBufferVS.unmap();
        }


        public override void Prepare()
        {
            base.Prepare();

            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);
            ImGui.GetIO().Fonts.AddFontDefault();
           

            //loadTextures();
            generateQuad();
            setupVertexDescriptions();
            prepareUniformBuffers();
            setupDescriptorSetLayout();
            preparePipelines();
            setupDescriptorPool();


            RecreateFontDeviceTexture();

            setupDescriptorSet();
            //buildCommandBuffers();

            ImGuiStylePtr style = ImGui.GetStyle();

            SetOpenTKKeyMappings();

            SetPerFrameImGuiData(1f / 60f);

            ImGui.NewFrame();

            prepared = true;
        }

        private IntPtr _fontAtlasID = (IntPtr)1;
      

        private unsafe void RecreateFontDeviceTexture()
        {
            var io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out byte* out_pixels, out int out_width, out int out_height, out int out_bytes_per_pixel);
            this.texture = this.createTexture((uint)out_width, (uint)out_height, (uint)out_bytes_per_pixel, out_pixels);
            io.Fonts.SetTexID(_fontAtlasID);
            io.Fonts.ClearTexData();
        }

        private static unsafe void SetOpenTKKeyMappings()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
            io.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
            io.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
            io.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.BackSpace;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
            io.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
            io.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
            io.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
            io.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
            io.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
            io.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;
        }

        private unsafe void SetPerFrameImGuiData(float deltaSeconds)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = new Vector2(
                this.width,
                this.height);
            io.DisplayFramebufferScale = Vector2.One;// window.ScaleFactor;
            io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
        }

        protected override void buildCommandBuffers()
        {
            VkCommandBufferBeginInfo cmdBufInfo = Initializers.commandBufferBeginInfo();

            FixedArray2<VkClearValue> clearValues = new FixedArray2<VkClearValue>();
            clearValues.First.color = defaultClearColor;
            clearValues.Second.depthStencil = new VkClearDepthStencilValue() { depth = 1.0f, stencil = 0 };

            VkRenderPassBeginInfo renderPassBeginInfo = Initializers.renderPassBeginInfo();
            renderPassBeginInfo.renderPass = renderPass;
            renderPassBeginInfo.renderArea.offset.x = 0;
            renderPassBeginInfo.renderArea.offset.y = 0;
            renderPassBeginInfo.renderArea.extent.width = width;
            renderPassBeginInfo.renderArea.extent.height = height;
            renderPassBeginInfo.clearValueCount = 2;
            renderPassBeginInfo.pClearValues = (VkClearValue*)Unsafe.AsPointer(ref clearValues);

            //for (int i = 0; i < drawCmdBuffers.Count; ++i)
            uint i = this.currentBuffer;
            {
                // Set target frame buffer
                renderPassBeginInfo.framebuffer = frameBuffers[i];

                Util.CheckResult(vkBeginCommandBuffer(drawCmdBuffers[i], &cmdBufInfo));

                vkCmdBeginRenderPass(drawCmdBuffers[i], &renderPassBeginInfo, VkSubpassContents.Inline);

                VkViewport viewport = Initializers.viewport((float)width, (float)height, 0.0f, 1.0f);
                vkCmdSetViewport(drawCmdBuffers[i], 0, 1, &viewport);

                VkRect2D scissor = Initializers.rect2D(width, height, 0, 0);
                vkCmdSetScissor(drawCmdBuffers[i], 0, 1, &scissor);

                RenderImDrawData(ImGui.GetDrawData());

                vkCmdEndRenderPass(drawCmdBuffers[i]);

                Util.CheckResult(vkEndCommandBuffer(drawCmdBuffers[i]));
            }
        }

        void draw()
        {
            base.prepareFrame();

            buildCommandBuffers();

            // Command buffer to be sumitted to the queue
            submitInfo.commandBufferCount = 1;
            submitInfo.pCommandBuffers = (VkCommandBuffer*)drawCmdBuffers.GetAddress(currentBuffer);

            // Submit to queue
            Util.CheckResult(vkQueueSubmit(queue, 1, ref submitInfo, VkFence.Null));

            submitFrame();
        }

        protected override void render()
        {
            if (!prepared)
                return;

            UpdateImGuiInput();

            ImGui.NewFrame();

            ImGui.ShowDemoWindow();

            ImGui.Render();

            draw();
        }

        protected override void viewChanged()
        {
            updateUniformBuffers();
        }

        protected override void keyPressed(Key keyCode)
        {
            switch (keyCode)
            {
                case Key.KeypadAdd:
                    //changeLodBias(0.1f);
                    break;
                case Key.KeypadSubtract:
                    //changeLodBias(-0.1f);
                    break;
            }
        }

        /*
        virtual void getOverlayText(VulkanTextOverlay* textOverlay)
        {
            std::stringstream ss;
            ss << std::setprecision(2) << std::fixed << uboVS.lodBias;
            textOverlay->addText("LOD bias: " + ss.str() + " (numpad +/- to change)", 5.0f, 85.0f, VulkanTextOverlay::alignLeft);
        }
        */


        private unsafe void RenderImDrawData(ImDrawDataPtr draw_data)
        {
            var io = ImGui.GetIO();

            float width = io.DisplaySize.X;
            float height = io.DisplaySize.Y;
            
            if (draw_data.CmdListsCount == 0)
            {
                return;
            }
            /*
            if (draw_data.TotalVtxCount*sizeof(ImDrawVert) > (int)vertexBuffer.size)
            {
                vertexBuffer.destroy();
                //vertexBuffer = GraphicsBuffer.CreateDynamic<ImDrawVert>(BufferUsages.VertexBuffer, (int)(1.5f * draw_data.TotalVtxCount));
            }

            if (draw_data.TotalIdxCount * sizeof(ushort) > (int)indexBuffer.size)
            {
                indexBuffer.destroy();
                //indexBuffer = GraphicsBuffer.CreateDynamic<ushort>(BufferUsages.IndexBuffer, (int)(1.5f * draw_data.TotalIdxCount));
            }*/

            updateUniformBuffers();

            uint vertexOffsetInVertices = 0;
            uint indexOffsetInElements = 0;

            for (int i = 0; i < draw_data.CmdListsCount; i++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

                vertexBuffer.SetData((void*)cmd_list.VtxBuffer.Data,
                    vertexOffsetInVertices * (uint)sizeof(ImDrawVert), (uint)cmd_list.VtxBuffer.Size * (uint)sizeof(ImDrawVert));

                indexBuffer.SetData((void*)cmd_list.IdxBuffer.Data,
                    indexOffsetInElements * sizeof(ushort), (uint)cmd_list.IdxBuffer.Size * sizeof(ushort));

                vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
                indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
            }


            vkCmdBindDescriptorSets(drawCmdBuffers[currentBuffer], VkPipelineBindPoint.Graphics, pipelineLayout, 0, 1, ref descriptorSet, 0, null);
            vkCmdBindPipeline(drawCmdBuffers[currentBuffer], VkPipelineBindPoint.Graphics, pipelines_solid);

            ulong offsets = 0;
            vkCmdBindVertexBuffers(drawCmdBuffers[currentBuffer], VERTEX_BUFFER_BIND_ID, 1, ref vertexBuffer.buffer, &offsets);
            vkCmdBindIndexBuffer(drawCmdBuffers[currentBuffer], indexBuffer.buffer, 0, VkIndexType.Uint16);

            draw_data.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

            int vtx_offset = 0;
            int idx_offset = 0;
            for (int n = 0; n < draw_data.CmdListsCount; n++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];
                for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
                {
                    ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                    if (pcmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        if (pcmd.TextureId != IntPtr.Zero)
                        {
                            if (pcmd.TextureId == _fontAtlasID)
                            {
                                //    cl.SetGraphicsResourceSet(1, _fontTextureResourceSet);
                            }
                            else
                            {
                                //    cl.SetGraphicsResourceSet(1, GetImageResourceSet(pcmd.TextureId));
                            }
                        }
                        

                        VkRect2D scissor = Initializers.rect2D((uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                            (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y), (int)pcmd.ClipRect.X, (int)pcmd.ClipRect.Y);
                        vkCmdSetScissor(drawCmdBuffers[currentBuffer], 0, 1, &scissor);

                        vkCmdDrawIndexed(drawCmdBuffers[currentBuffer], pcmd.ElemCount, 1, (uint)idx_offset, vtx_offset, 0);

                    }

                    idx_offset += (int)pcmd.ElemCount;
                }

                vtx_offset += cmd_list.VtxBuffer.Size;
            }

        }


        private bool _controlDown;
        private bool _shiftDown;
        private bool _altDown;
        private bool _winKeyDown;
        private unsafe void UpdateImGuiInput()
        {

            ImGuiIOPtr io = ImGui.GetIO();

            var mousePosition = snapshot.MousePosition;

            // Determine if any of the mouse buttons were pressed during this snapshot period, even if they are no longer held.
            bool leftPressed = false;
            bool middlePressed = false;
            bool rightPressed = false;
            foreach (MouseEvent me in snapshot.MouseEvents)
            {
                if (me.Down)
                {
                    switch (me.MouseButton)
                    {
                        case MouseButton.Left:
                            leftPressed = true;
                            break;
                        case MouseButton.Middle:
                            middlePressed = true;
                            break;
                        case MouseButton.Right:
                            rightPressed = true;
                            break;
                    }
                }
            }

            io.MouseDown[0] = leftPressed || snapshot.IsMouseDown(MouseButton.Left);
            io.MouseDown[1] = rightPressed || snapshot.IsMouseDown(MouseButton.Right);
            io.MouseDown[2] = middlePressed || snapshot.IsMouseDown(MouseButton.Middle);
            io.MousePos = mousePosition;
            io.MouseWheel = snapshot.WheelDelta;

            IReadOnlyList<char> keyCharPresses = snapshot.KeyCharPresses;
            for (int i = 0; i < keyCharPresses.Count; i++)
            {
                char c = keyCharPresses[i];
                io.AddInputCharacter(c);
            }

            IReadOnlyList<KeyEvent> keyEvents = snapshot.KeyEvents;
            for (int i = 0; i < keyEvents.Count; i++)
            {
                KeyEvent keyEvent = keyEvents[i];
                io.KeysDown[(int)keyEvent.Key] = keyEvent.Down;
                if (keyEvent.Key == Key.ControlLeft)
                {
                    _controlDown = keyEvent.Down;
                }
                if (keyEvent.Key == Key.ShiftLeft)
                {
                    _shiftDown = keyEvent.Down;
                }
                if (keyEvent.Key == Key.AltLeft)
                {
                    _altDown = keyEvent.Down;
                }
                if (keyEvent.Key == Key.WinLeft)
                {
                    _winKeyDown = keyEvent.Down;
                }
            }

            io.KeyCtrl = _controlDown;
            io.KeyAlt = _altDown;
            io.KeyShift = _shiftDown;
            io.KeySuper = _winKeyDown;
        }

        public static void Main() => new ImGUI().Run();
    }
}
