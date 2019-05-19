﻿using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using static VulkanNative;
    public enum PipelineBindPoint
    {
        Graphics = 0,
        Compute = 1
    }

    public class CommandBuffer : DisposeBase
    {
        public VkCommandBuffer commandBuffer;
        private VkRenderPass renderPass;
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
            renderPass = renderPassBeginInfo.renderPass;
        }

        public void EndRenderPass()
        {
            vkCmdEndRenderPass(commandBuffer);
        }

        public void SetScissor(Rect2D scissor)
        {
            SetScissor(ref scissor);
        }

        public void SetScissor(ref Rect2D pScissors)
        {
            vkCmdSetScissor(commandBuffer, 0, 1, Utilities.AsPointer(ref pScissors));
        }

        public void SetViewport(Viewport viewport)
        {
            SetViewport(ref viewport);
        }

        public void SetViewport(ref Viewport pViewports)
        {
            vkCmdSetViewport(commandBuffer, 0, 1, Utilities.AsPointer(ref pViewports));
        }

        public unsafe void BindDescriptorSets(PipelineBindPoint pipelineBindPoint, VkPipelineLayout layout, uint firstSet, uint descriptorSetCount, ref VkDescriptorSet pDescriptorSets, uint dynamicOffsetCount, uint* pDynamicOffsets)
        {
            vkCmdBindDescriptorSets(commandBuffer, (VkPipelineBindPoint)pipelineBindPoint, layout, firstSet, descriptorSetCount, ref pDescriptorSets, dynamicOffsetCount, pDynamicOffsets);
        }

        public unsafe void BindResourceSet(PipelineBindPoint pipelineBindPoint,
            VkPipelineLayout layout, ResourceSet pDescriptorSets, uint dynamicOffsetCount, uint* pDynamicOffsets)
        {
            vkCmdBindDescriptorSets(commandBuffer, (VkPipelineBindPoint)pipelineBindPoint, layout, 0, 1, ref pDescriptorSets.descriptorSet, dynamicOffsetCount, pDynamicOffsets);
        }

        public void BindPipeline(PipelineBindPoint pipelineBindPoint, VkPipeline pipeline)
        {
            vkCmdBindPipeline(commandBuffer, (VkPipelineBindPoint)pipelineBindPoint, pipeline);
        }

        public void BindGraphicsPipeline(VkPipeline pipeline)
        {
            vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, pipeline);
        }

        public unsafe void BindVertexBuffers(uint firstBinding, uint bindingCount, IntPtr pBuffers, ref ulong pOffsets)
        {
            vkCmdBindVertexBuffers(commandBuffer, firstBinding, bindingCount, pBuffers, ref pOffsets);
        }

        public void BindVertexBuffer(uint firstBinding, GraphicsBuffer buffer)
        {
            ulong pOffsets = 0;
            vkCmdBindVertexBuffers(commandBuffer, firstBinding, 1, ref buffer.buffer, ref pOffsets);
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

        public unsafe void DrawGeometry(Geometry geometry, Pipeline pipeline, Material material)
        {
            var pipe = pipeline.GetGraphicsPipeline(renderPass, material.Shader.Main, geometry);
            BindPipeline(PipelineBindPoint.Graphics, pipe);
            BindResourceSet(PipelineBindPoint.Graphics, pipeline.pipelineLayout, material.ResourceSet, 0, null);
            geometry.Draw(this);
        }

        public unsafe void DrawGeometry(Geometry geometry, Pipeline pipeline, Pass shader, ResourceSet resourceSet)
        {
            var pipe = pipeline.GetGraphicsPipeline(renderPass, shader, geometry);
            BindPipeline(PipelineBindPoint.Graphics, pipe);
            BindResourceSet(PipelineBindPoint.Graphics, pipeline.pipelineLayout, resourceSet, 0, null);
            geometry.Draw(this);
        }

    }
}
