using System;
using System.Runtime.CompilerServices;
using Vulkan;
using static Vulkan.VulkanNative;

namespace SharpGame
{
    public unsafe static class Builder
    {
        public static VkSemaphoreCreateInfo SemaphoreCreateInfo()
        {
            VkSemaphoreCreateInfo semaphoreCreateInfo = new VkSemaphoreCreateInfo();
            semaphoreCreateInfo.sType = VkStructureType.SemaphoreCreateInfo;
            return semaphoreCreateInfo;
        }

        public static VkSubmitInfo SubmitInfo()
        {
            VkSubmitInfo submitInfo = new VkSubmitInfo();
            submitInfo.sType = VkStructureType.SubmitInfo;
            return submitInfo;
        }

        public static VkCommandBufferAllocateInfo CommandBufferAllocateInfo(
            VkCommandPool commandPool,
            VkCommandBufferLevel level,
            uint bufferCount)
        {
            VkCommandBufferAllocateInfo commandBufferAllocateInfo = new VkCommandBufferAllocateInfo();
            commandBufferAllocateInfo.sType = VkStructureType.CommandBufferAllocateInfo;
            commandBufferAllocateInfo.commandPool = commandPool;
            commandBufferAllocateInfo.level = level;
            commandBufferAllocateInfo.commandBufferCount = bufferCount;
            return commandBufferAllocateInfo;
        }

        public static VkCommandBufferBeginInfo CommandBufferBeginInfo()
        {
            VkCommandBufferBeginInfo cmdBufferBeginInfo = new VkCommandBufferBeginInfo();
            cmdBufferBeginInfo.sType = VkStructureType.CommandBufferBeginInfo;
            return cmdBufferBeginInfo;
        }

        public static VkRenderPassBeginInfo RenderPassBeginInfo()
        {
            VkRenderPassBeginInfo renderPassBeginInfo = VkRenderPassBeginInfo.New();
            return renderPassBeginInfo;
        }

        public static VkPipelineInputAssemblyStateCreateInfo InputAssemblyStateCreateInfo(
            VkPrimitiveTopology topology,
            uint flags = 0,
            uint primitiveRestartEnable = False)
        {
            VkPipelineInputAssemblyStateCreateInfo pipelineInputAssemblyStateCreateInfo = VkPipelineInputAssemblyStateCreateInfo.New();
            pipelineInputAssemblyStateCreateInfo.topology = topology;
            pipelineInputAssemblyStateCreateInfo.flags = flags;
            pipelineInputAssemblyStateCreateInfo.primitiveRestartEnable = primitiveRestartEnable;
            return pipelineInputAssemblyStateCreateInfo;
        }

        public static VkPipelineRasterizationStateCreateInfo RasterizationStateCreateInfo(
            VkPolygonMode polygonMode,
            VkCullModeFlags cullMode,
            VkFrontFace frontFace,
            uint flags = 0)
        {
            VkPipelineRasterizationStateCreateInfo pipelineRasterizationStateCreateInfo = VkPipelineRasterizationStateCreateInfo.New();
            pipelineRasterizationStateCreateInfo.polygonMode = polygonMode;
            pipelineRasterizationStateCreateInfo.cullMode = cullMode;
            pipelineRasterizationStateCreateInfo.frontFace = frontFace;
            pipelineRasterizationStateCreateInfo.flags = flags;
            pipelineRasterizationStateCreateInfo.depthClampEnable = False;
            pipelineRasterizationStateCreateInfo.lineWidth = 1.0f;
            return pipelineRasterizationStateCreateInfo;
        }

        public static VkPipelineColorBlendAttachmentState ColorBlendAttachmentState(
            VkColorComponentFlags colorWriteMask,
            bool blendEnable)
        {
            VkPipelineColorBlendAttachmentState pipelineColorBlendAttachmentState = new VkPipelineColorBlendAttachmentState();
            pipelineColorBlendAttachmentState.colorWriteMask = colorWriteMask;
            pipelineColorBlendAttachmentState.blendEnable = blendEnable;
            return pipelineColorBlendAttachmentState;
        }

        public static VkPipelineColorBlendStateCreateInfo ColorBlendStateCreateInfo(
            uint attachmentCount,
             ref VkPipelineColorBlendAttachmentState pAttachments)
        {

            VkPipelineColorBlendStateCreateInfo pipelineColorBlendStateCreateInfo = VkPipelineColorBlendStateCreateInfo.New();
            pipelineColorBlendStateCreateInfo.attachmentCount = attachmentCount;
            pipelineColorBlendStateCreateInfo.pAttachments = (VkPipelineColorBlendAttachmentState*)Unsafe.AsPointer(ref pAttachments);
            return pipelineColorBlendStateCreateInfo;
        }

        public static VkPipelineDepthStencilStateCreateInfo DepthStencilStateCreateInfo(
            bool depthTestEnable,
            bool depthWriteEnable,
            VkCompareOp depthCompareOp)
        {
            VkPipelineDepthStencilStateCreateInfo pipelineDepthStencilStateCreateInfo = VkPipelineDepthStencilStateCreateInfo.New();
            pipelineDepthStencilStateCreateInfo.depthTestEnable = depthTestEnable;
            pipelineDepthStencilStateCreateInfo.depthWriteEnable = depthWriteEnable;
            pipelineDepthStencilStateCreateInfo.depthCompareOp = depthCompareOp;
            pipelineDepthStencilStateCreateInfo.front = pipelineDepthStencilStateCreateInfo.back;
            pipelineDepthStencilStateCreateInfo.back.compareOp = VkCompareOp.Always;
            return pipelineDepthStencilStateCreateInfo;
        }

        public static VkPipelineViewportStateCreateInfo ViewportStateCreateInfo(
            uint viewportCount,
            uint scissorCount,
            uint flags = 0)
        {
            VkPipelineViewportStateCreateInfo pipelineViewportStateCreateInfo = VkPipelineViewportStateCreateInfo.New();
            pipelineViewportStateCreateInfo.viewportCount = viewportCount;
            pipelineViewportStateCreateInfo.scissorCount = scissorCount;
            pipelineViewportStateCreateInfo.flags = flags;
            return pipelineViewportStateCreateInfo;
        }

        public static VkPipelineMultisampleStateCreateInfo MultisampleStateCreateInfo(
            VkSampleCountFlags rasterizationSamples,
            uint flags = 0)
        {
            VkPipelineMultisampleStateCreateInfo pipelineMultisampleStateCreateInfo = VkPipelineMultisampleStateCreateInfo.New();
            pipelineMultisampleStateCreateInfo.rasterizationSamples = rasterizationSamples;
            pipelineMultisampleStateCreateInfo.flags = flags;
            return pipelineMultisampleStateCreateInfo;
        }

        public static VkPipelineDynamicStateCreateInfo DynamicStateCreateInfo(
            VkDynamicState* pDynamicStates,
            uint dynamicStateCount,
            uint flags = 0)
        {
            VkPipelineDynamicStateCreateInfo pipelineDynamicStateCreateInfo = VkPipelineDynamicStateCreateInfo.New();
            pipelineDynamicStateCreateInfo.pDynamicStates = pDynamicStates;
            pipelineDynamicStateCreateInfo.dynamicStateCount = dynamicStateCount;
            pipelineDynamicStateCreateInfo.flags = flags;
            return pipelineDynamicStateCreateInfo;
        }

        public static VkPipelineDynamicStateCreateInfo DynamicStateCreateInfo(
            NativeList<VkDynamicState> pDynamicStates,
            uint flags = 0)
        {
            VkPipelineDynamicStateCreateInfo pipelineDynamicStateCreateInfo = VkPipelineDynamicStateCreateInfo.New();
            pipelineDynamicStateCreateInfo.pDynamicStates = (VkDynamicState*)pDynamicStates.Data;
            pipelineDynamicStateCreateInfo.dynamicStateCount = pDynamicStates.Count;
            pipelineDynamicStateCreateInfo.flags = flags;
            return pipelineDynamicStateCreateInfo;
        }

        public static VkPipelineTessellationStateCreateInfo TessellationStateCreateInfo(uint patchControlPoints)
        {
            VkPipelineTessellationStateCreateInfo pipelineTessellationStateCreateInfo = VkPipelineTessellationStateCreateInfo.New();
            pipelineTessellationStateCreateInfo.patchControlPoints = patchControlPoints;
            return pipelineTessellationStateCreateInfo;
        }

        public static VkGraphicsPipelineCreateInfo GraphicsPipelineCreateInfo(
            VkPipelineLayout layout,
            VkRenderPass renderPass,
            VkPipelineCreateFlags flags = 0)
        {
            VkGraphicsPipelineCreateInfo pipelineCreateInfo = VkGraphicsPipelineCreateInfo.New();
            pipelineCreateInfo.layout = layout;
            pipelineCreateInfo.renderPass = renderPass;
            pipelineCreateInfo.flags = flags;
            pipelineCreateInfo.basePipelineIndex = -1;
            pipelineCreateInfo.basePipelineHandle = new VkPipeline();
            return pipelineCreateInfo;
        }

        public static VkVertexInputBindingDescription VertexInputBindingDescription(
            uint binding, uint stride, VkVertexInputRate inputRate)
        {
            VkVertexInputBindingDescription vInputBindDescription = new VkVertexInputBindingDescription
            {
                binding = binding,
                stride = stride,
                inputRate = inputRate
            };
            return vInputBindDescription;
        }

        public static VkVertexInputAttributeDescription VertexInputAttributeDescription(
            uint binding, uint location, VkFormat format, uint offset)
        {
            VkVertexInputAttributeDescription vInputAttribDescription = new VkVertexInputAttributeDescription
            {
                location = location,
                binding = binding,
                format = format,
                offset = offset
            };
            return vInputAttribDescription;
        }

        public static VkWriteDescriptorSet WriteDescriptorSet(
            VkDescriptorSet dstSet,
            VkDescriptorType type,
            uint binding,
            ref VkDescriptorBufferInfo bufferInfo,
            uint descriptorCount = 1)
        {
            VkWriteDescriptorSet writeDescriptorSet = VkWriteDescriptorSet.New();
            writeDescriptorSet.dstSet = dstSet;
            writeDescriptorSet.descriptorType = type;
            writeDescriptorSet.dstBinding = binding;
            writeDescriptorSet.pBufferInfo = (VkDescriptorBufferInfo*)Unsafe.AsPointer(ref bufferInfo);
            writeDescriptorSet.descriptorCount = descriptorCount;
            return writeDescriptorSet;
        }

        public static VkWriteDescriptorSet WriteDescriptorSet(
            VkDescriptorSet dstSet,
            VkDescriptorType type,
            uint binding,
            ref VkDescriptorImageInfo imageInfo,
            uint descriptorCount = 1)
        {
            VkWriteDescriptorSet writeDescriptorSet = VkWriteDescriptorSet.New();
            writeDescriptorSet.dstSet = dstSet;
            writeDescriptorSet.descriptorType = type;
            writeDescriptorSet.dstBinding = binding;
            writeDescriptorSet.pImageInfo = (VkDescriptorImageInfo*)Unsafe.AsPointer(ref imageInfo);
            writeDescriptorSet.descriptorCount = descriptorCount;
            return writeDescriptorSet;
        }


        /** @brief Initialize an image memory barrier with no image transfer ownership */
        public static VkImageMemoryBarrier ImageMemoryBarrier()
        {
            VkImageMemoryBarrier imageMemoryBarrier = VkImageMemoryBarrier.New();
            imageMemoryBarrier.srcQueueFamilyIndex = QueueFamilyIgnored;
            imageMemoryBarrier.dstQueueFamilyIndex = QueueFamilyIgnored;
            return imageMemoryBarrier;
        }

        public static VkImageCreateInfo ImageCreateInfo()
        {
            VkImageCreateInfo imageCreateInfo = VkImageCreateInfo.New();
            return imageCreateInfo;
        }

        public static VkMemoryAllocateInfo MemoryAllocateInfo()
        {
            VkMemoryAllocateInfo memAllocInfo = new VkMemoryAllocateInfo();
            memAllocInfo.sType = VkStructureType.MemoryAllocateInfo;
            return memAllocInfo;
        }


        public static VkBufferCreateInfo BufferCreateInfo()
        {
            VkBufferCreateInfo bufCreateInfo = VkBufferCreateInfo.New();
            return bufCreateInfo;
        }

        public static VkBufferCreateInfo BufferCreateInfo(
            VkBufferUsageFlags usage,
            ulong size)
        {
            VkBufferCreateInfo bufCreateInfo = VkBufferCreateInfo.New();
            bufCreateInfo.usage = usage;
            bufCreateInfo.size = size;
            return bufCreateInfo;
        }

        public static VkSamplerCreateInfo SamplerCreateInfo()
        {
            VkSamplerCreateInfo samplerCreateInfo = VkSamplerCreateInfo.New();
            return samplerCreateInfo;
        }

        public static VkImageViewCreateInfo ImageViewCreateInfo()
        {
            VkImageViewCreateInfo imageViewCreateInfo = VkImageViewCreateInfo.New();
            return imageViewCreateInfo;
        }

        public static VkViewport Viewport(
            float width,
            float height,
            float minDepth,
            float maxDepth)
        {
            VkViewport viewport = new VkViewport();
            viewport.width = width;
            viewport.height = height;
            viewport.minDepth = minDepth;
            viewport.maxDepth = maxDepth;
            return viewport;
        }

        public static VkRect2D Rect2D(
            int offsetX,
            int offsetY,
            uint width,
            uint height)
        {
            VkRect2D rect2D = new VkRect2D();
            rect2D.extent.width = width;
            rect2D.extent.height = height;
            rect2D.offset.x = offsetX;
            rect2D.offset.y = offsetY;
            return rect2D;
        }

        public static VkPipelineVertexInputStateCreateInfo VertexInputStateCreateInfo()
        {
            VkPipelineVertexInputStateCreateInfo pipelineVertexInputStateCreateInfo = VkPipelineVertexInputStateCreateInfo.New();
            return pipelineVertexInputStateCreateInfo;
        }

        public static VkDescriptorPoolCreateInfo DescriptorPoolCreateInfo(
            uint poolSizeCount,
            VkDescriptorPoolSize* pPoolSizes,
            uint maxSets)
        {
            VkDescriptorPoolCreateInfo descriptorPoolInfo = VkDescriptorPoolCreateInfo.New();
            descriptorPoolInfo.poolSizeCount = poolSizeCount;
            descriptorPoolInfo.pPoolSizes = pPoolSizes;
            descriptorPoolInfo.maxSets = maxSets;
            return descriptorPoolInfo;
        }

        public static VkDescriptorPoolSize DescriptorPoolSize(
            VkDescriptorType type,
            uint descriptorCount)
        {
            VkDescriptorPoolSize descriptorPoolSize = new VkDescriptorPoolSize
            {
                type = type,
                descriptorCount = descriptorCount
            };
            return descriptorPoolSize;
        }

        public static VkDescriptorSetLayoutBinding DescriptorSetLayoutBinding(
            VkDescriptorType type,
            VkShaderStageFlags stageFlags,
            uint binding,
            uint descriptorCount = 1)
        {
            VkDescriptorSetLayoutBinding setLayoutBinding = new VkDescriptorSetLayoutBinding
            {
                descriptorType = type,
                stageFlags = stageFlags,
                binding = binding,
                descriptorCount = descriptorCount
            };
            return setLayoutBinding;
        }

        public static VkFramebufferCreateInfo FramebufferCreateInfo()
        {
            VkFramebufferCreateInfo framebufferCreateInfo = VkFramebufferCreateInfo.New();
            return framebufferCreateInfo;
        }

        public static VkDescriptorSetLayoutCreateInfo DescriptorSetLayoutCreateInfo(
            VkDescriptorSetLayoutBinding[] bindings)
        {
            return DescriptorSetLayoutCreateInfo((VkDescriptorSetLayoutBinding*)Unsafe.AsPointer(ref bindings[0]), (uint)bindings.Length);
        }

        public static VkDescriptorSetLayoutCreateInfo DescriptorSetLayoutCreateInfo(
            VkDescriptorSetLayoutBinding* pBindings,
            uint bindingCount)
        {
            VkDescriptorSetLayoutCreateInfo descriptorSetLayoutCreateInfo = VkDescriptorSetLayoutCreateInfo.New();
            descriptorSetLayoutCreateInfo.pBindings = pBindings;
            descriptorSetLayoutCreateInfo.bindingCount = bindingCount;
            return descriptorSetLayoutCreateInfo;
        }

        public static VkPipelineLayoutCreateInfo PipelineLayoutCreateInfo(
            ref VkDescriptorSetLayout pSetLayouts,
            uint setLayoutCount = 1)
        {
            VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo = VkPipelineLayoutCreateInfo.New();
            pipelineLayoutCreateInfo.setLayoutCount = setLayoutCount;
            pipelineLayoutCreateInfo.pSetLayouts = (VkDescriptorSetLayout*)Unsafe.AsPointer(ref pSetLayouts);
            return pipelineLayoutCreateInfo;
        }

        public static VkMappedMemoryRange MappedMemoryRange()
        {
            VkMappedMemoryRange mappedMemoryRange = VkMappedMemoryRange.New();
            return mappedMemoryRange;
        }

        public static VkDescriptorSetAllocateInfo DescriptorSetAllocateInfo(
            VkDescriptorPool descriptorPool,
            VkDescriptorSetLayout* pSetLayouts,
            uint descriptorSetCount)
        {
            VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = VkDescriptorSetAllocateInfo.New();
            descriptorSetAllocateInfo.descriptorPool = descriptorPool;
            descriptorSetAllocateInfo.pSetLayouts = pSetLayouts;
            descriptorSetAllocateInfo.descriptorSetCount = descriptorSetCount;
            return descriptorSetAllocateInfo;
        }

        public static VkDescriptorImageInfo DescriptorImageInfo(VkSampler sampler, VkImageView imageView, VkImageLayout imageLayout)
        {
            VkDescriptorImageInfo descriptorImageInfo = new VkDescriptorImageInfo
            {
                sampler = sampler,
                imageView = imageView,
                imageLayout = imageLayout
            };
            return descriptorImageInfo;
        }

        public static VkPushConstantRange PushConstantRange(VkShaderStageFlags stageFlags, uint size, uint offset)
        {
            VkPushConstantRange pushConstantRange = new VkPushConstantRange
            {
                stageFlags = stageFlags,
                offset = offset,
                size = size
            };
            return pushConstantRange;
        }
    }
}
