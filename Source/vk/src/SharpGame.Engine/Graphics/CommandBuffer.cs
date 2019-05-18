using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using static VulkanNative;

    public class CommandBuffer : DisposeBase
    {
        public VkCommandBuffer commandBuffer;

        public CommandBuffer(VkCommandBuffer cmdBuffer)
        {
            commandBuffer = cmdBuffer;
        }

        public void Begin()
        {
            var cmdBufInfo = VkCommandBufferBeginInfo.New();
            Util.CheckResult(vkBeginCommandBuffer(commandBuffer, ref cmdBufInfo));
        }

        public void End()
        {
            Util.CheckResult(vkEndCommandBuffer(commandBuffer));
        }

        public void BeginRenderPass(ref VkRenderPassBeginInfo renderPassBeginInfo, VkSubpassContents contents)
        {
            vkCmdBeginRenderPass(commandBuffer, ref renderPassBeginInfo, contents);
        }

        public void EndRenderPass()
        {
            vkCmdEndRenderPass(commandBuffer);
        }

        public void SetScissor(ref Rect2D pScissors)
        {
            vkCmdSetScissor(commandBuffer, 0, 1, Utilities.AsPointer(ref pScissors));
        }

        public void SetViewport(ref Viewport pViewports)
        {
            vkCmdSetViewport(commandBuffer, 0, 1, Utilities.AsPointer(ref pViewports));
        }

        public unsafe void BindDescriptorSets(VkPipelineBindPoint pipelineBindPoint, VkPipelineLayout layout, uint firstSet, uint descriptorSetCount, ref VkDescriptorSet pDescriptorSets, uint dynamicOffsetCount, uint* pDynamicOffsets)
        {
            vkCmdBindDescriptorSets(commandBuffer, pipelineBindPoint, layout, firstSet, descriptorSetCount, ref pDescriptorSets, dynamicOffsetCount, pDynamicOffsets);
        }

        public void BindPipeline(VkPipelineBindPoint pipelineBindPoint, VkPipeline pipeline)
        {
            vkCmdBindPipeline(commandBuffer, pipelineBindPoint, pipeline);
        }

        public void BindVertexBuffers(uint firstBinding, uint bindingCount, IntPtr pBuffers, ref ulong pOffsets)
        {
            vkCmdBindVertexBuffers(commandBuffer, firstBinding, bindingCount, pBuffers, ref pOffsets);
        }

        public unsafe void BindVertexBuffer(uint firstBinding, GraphicsBuffer buffer, ulong* pOffsets = null)
        {
            vkCmdBindVertexBuffers(commandBuffer, firstBinding, 1, ref buffer.buffer, pOffsets);
        }

        public unsafe void BindIndexBuffer(GraphicsBuffer buffer, ulong offset, IndexType indexType)
        {
            vkCmdBindIndexBuffer(commandBuffer, buffer.buffer, offset, (VkIndexType)indexType);
        }

        public void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance)
        {
            vkCmdDraw(commandBuffer, vertexCount, instanceCount, firstVertex, firstInstance);
        }

        public void DrawIndexed(uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
        {
            vkCmdDrawIndexed(commandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
        }
    }
}
