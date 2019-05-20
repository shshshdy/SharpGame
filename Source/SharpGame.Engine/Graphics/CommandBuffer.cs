﻿using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using global::System.Runtime.InteropServices;
    using static VulkanNative;
    public enum PipelineBindPoint
    {
        Graphics = 0,
        Compute = 1
    }

    public enum CommandBufferLevel
    {
        Primary = 0,
        Secondary = 1
    }

    public struct RenderPassBeginInfo
    {
        public RenderPass renderPass;
        public Framebuffer framebuffer;
        public Rect2D renderArea;
        public ClearValue[] clearValues;

        public RenderPassBeginInfo(Framebuffer framebuffer, Rect2D renderArea, params ClearValue[] clearValues)
        {
            this.renderPass = framebuffer.renderPass;
            this.framebuffer = framebuffer;
            this.renderArea = renderArea;
            this.clearValues = clearValues;
        }


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


    /// <summary>
    /// Structure specifying a clear color value.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct ClearColorValue
    {
        /// <summary>
        /// Are the color clear values when the format of the image or attachment is one of the
        /// formats other than signed integer or unsigned integer. Floating point values are
        /// automatically converted to the format of the image, with the clear value being treated as
        /// linear if the image is sRGB.
        /// </summary>
        [FieldOffset(0)] public Color4 Float4;
        /// <summary>
        /// Are the color clear values when the format of the image or attachment is signed integer.
        /// Signed integer values are converted to the format of the image by casting to the smaller
        /// type (with negative 32-bit values mapping to negative values in the smaller type). If the
        /// integer clear value is not representable in the target type (e.g. would overflow in
        /// conversion to that type), the clear value is undefined.
        /// </summary>
        [FieldOffset(0)] public Int4 Int4;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearColorValue"/> structure.
        /// </summary>
        /// <param name="value">
        /// Are the color clear values when the format of the image or attachment is one of the
        /// formats other than signed integer or unsigned integer. Floating point values are
        /// automatically converted to the format of the image, with the clear value being treated as
        /// linear if the image is sRGB.
        /// </param>
        public ClearColorValue(Color4 value) : this()
        {
            Float4 = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearColorValue"/> structure.
        /// </summary>
        /// <param name="value">
        /// Are the color clear values when the format of the image or attachment is signed integer.
        /// Signed integer values are converted to the format of the image by casting to the smaller
        /// type (with negative 32-bit values mapping to negative values in the smaller type). If the
        /// integer clear value is not representable in the target type (e.g. would overflow in
        /// conversion to that type), the clear value is undefined.
        /// </param>
        public ClearColorValue(Int4 value) : this()
        {
            Int4 = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearColorValue"/> structure.
        /// </summary>
        /// <param name="r">The red clear value.</param>
        /// <param name="g">The green clear value.</param>
        /// <param name="b">The blue clear value.</param>
        /// <param name="a">The alpha clear value.</param>
        public ClearColorValue(float r, float g, float b, float a = 1.0f) : this()
        {
            Float4 = new Color4(r, g, b, a);
        }
    }

    /// <summary>
    /// Structure specifying a clear depth stencil value.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ClearDepthStencilValue
    {
        /// <summary>
        /// The clear value for the depth aspect of the depth/stencil attachment. It is a
        /// floating-point value which is automatically converted to the attachment’s format.
        /// <para>Must be between 0.0 and 1.0, inclusive.</para>
        /// </summary>
        public float Depth;
        /// <summary>
        /// The clear value for the stencil aspect of the depth/stencil attachment. It is a 32-bit
        /// integer value which is converted to the attachment's format by taking the appropriate
        /// number of LSBs.
        /// </summary>
        public int Stencil;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearDepthStencilValue"/> structure.
        /// </summary>
        /// <param name="depth">
        /// The clear value for the depth aspect of the depth/stencil attachment. It is a
        /// floating-point value which is automatically converted to the attachment’s format.
        /// </param>
        /// <param name="stencil">
        /// The clear value for the stencil aspect of the depth/stencil attachment. It is a 32-bit
        /// integer value which is converted to the attachment's format by taking the appropriate
        /// number of LSBs.
        /// </param>
        public ClearDepthStencilValue(float depth, int stencil)
        {
            Depth = depth;
            Stencil = stencil;
        }
    }

    /// <summary>
    /// Structure specifying a clear value.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct ClearValue
    {
        /// <summary>
        /// Specifies the color image clear values to use when clearing a color image or attachment.
        /// </summary>
        [FieldOffset(0)] public ClearColorValue Color;
        /// <summary>
        /// Specifies the depth and stencil clear values to use when clearing a depth/stencil image
        /// or attachment.
        /// </summary>
        [FieldOffset(0)] public ClearDepthStencilValue DepthStencil;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearValue"/> structure.
        /// </summary>
        /// <param name="color">
        /// Specifies the color image clear values to use when clearing a color image or attachment.
        /// </param>
        public ClearValue(ClearColorValue color) : this()
        {
            Color = color;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearValue"/> structure.
        /// </summary>
        /// <param name="depthStencil">
        /// Specifies the depth and stencil clear values to use when clearing a depth/stencil image
        /// or attachment.
        /// </param>
        public ClearValue(ClearDepthStencilValue depthStencil) : this()
        {
            DepthStencil = depthStencil;
        }

        /// <summary>
        /// Implicitly converts an instance of <see cref="ClearColorValue"/> to an instance of <see cref="ClearValue"/>.
        /// </summary>
        /// <param name="value">Instance to convert.</param>
        public static implicit operator ClearValue(ClearColorValue value) => new ClearValue(value);

        /// <summary>
        /// Implicitly converts an instance of <see cref="ClearDepthStencilValue"/> to an instance of
        /// <see cref="ClearValue"/>.
        /// </summary>
        /// <param name="value">Instance to convert.</param>
        public static implicit operator ClearValue(ClearDepthStencilValue value) => new ClearValue(value);
    }

    /// <summary>
    /// Specify how commands in the first subpass of a render pass are provided.
    /// </summary>
    public enum SubpassContents
    {
        /// <summary>
        /// Specifies that the contents of the subpass will be recorded inline in the primary command
        /// buffer, and secondary command buffers must not be executed within the subpass.
        /// </summary>
        Inline = 0,
        /// <summary>
        /// Specifies that the contents are recorded in secondary command buffers that will be called
        /// from the primary command buffer, and <see cref="CommandBuffer.CmdExecuteCommands"/> is
        /// the only valid command on the command buffer until <see
        /// cref="CommandBuffer.vkCmdNextSubpass"/> or <see cref="CommandBuffer.CmdEndRenderPass"/>.
        /// </summary>
        SecondaryCommandBuffers = 1
    }

}