using System;
using System.Runtime.CompilerServices;
using Vulkan;
using static Vulkan.VulkanNative;

namespace SharpGame
{
    public unsafe static class Builder
    {
        public static VkPipelineInputAssemblyStateCreateInfo InputAssemblyStateCreateInfo(
            PrimitiveTopology topology, uint flags = 0, uint primitiveRestartEnable = False)
        {
            var pipelineInputAssemblyStateCreateInfo = VkPipelineInputAssemblyStateCreateInfo.New();
            pipelineInputAssemblyStateCreateInfo.topology = (VkPrimitiveTopology)topology;
            pipelineInputAssemblyStateCreateInfo.flags = flags;
            pipelineInputAssemblyStateCreateInfo.primitiveRestartEnable = primitiveRestartEnable;
            return pipelineInputAssemblyStateCreateInfo;
        }

        public static VkPipelineViewportStateCreateInfo ViewportStateCreateInfo(uint viewportCount, uint scissorCount, uint flags = 0)
        {
            var pipelineViewportStateCreateInfo = VkPipelineViewportStateCreateInfo.New();
            pipelineViewportStateCreateInfo.viewportCount = viewportCount;
            pipelineViewportStateCreateInfo.scissorCount = scissorCount;
            pipelineViewportStateCreateInfo.flags = flags;
            return pipelineViewportStateCreateInfo;
        }
                
        public static VkPipelineTessellationStateCreateInfo TessellationStateCreateInfo(uint patchControlPoints)
        {
            var pipelineTessellationStateCreateInfo = VkPipelineTessellationStateCreateInfo.New();
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

        public static VkImageMemoryBarrier ImageMemoryBarrier()
        {
            VkImageMemoryBarrier imageMemoryBarrier = VkImageMemoryBarrier.New();
            imageMemoryBarrier.srcQueueFamilyIndex = QueueFamilyIgnored;
            imageMemoryBarrier.dstQueueFamilyIndex = QueueFamilyIgnored;
            return imageMemoryBarrier;
        }
        
        public static VkBufferCreateInfo BufferCreateInfo(VkBufferUsageFlags usage, ulong size)
        {
            VkBufferCreateInfo bufCreateInfo = VkBufferCreateInfo.New();
            bufCreateInfo.usage = usage;
            bufCreateInfo.size = size;
            return bufCreateInfo;
        }

        public static VkPipelineLayoutCreateInfo PipelineLayoutCreateInfo(
            ref VkDescriptorSetLayout pSetLayouts, uint setLayoutCount = 1)
        {
            VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo = VkPipelineLayoutCreateInfo.New();
            pipelineLayoutCreateInfo.setLayoutCount = setLayoutCount;
            pipelineLayoutCreateInfo.pSetLayouts = (VkDescriptorSetLayout*)Unsafe.AsPointer(ref pSetLayouts);
            return pipelineLayoutCreateInfo;
        }

        public static VkDescriptorSetAllocateInfo DescriptorSetAllocateInfo(
            VkDescriptorPool descriptorPool, VkDescriptorSetLayout* pSetLayouts, uint descriptorSetCount)
        {
            VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = VkDescriptorSetAllocateInfo.New();
            descriptorSetAllocateInfo.descriptorPool = descriptorPool;
            descriptorSetAllocateInfo.pSetLayouts = pSetLayouts;
            descriptorSetAllocateInfo.descriptorSetCount = descriptorSetCount;
            return descriptorSetAllocateInfo;
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
