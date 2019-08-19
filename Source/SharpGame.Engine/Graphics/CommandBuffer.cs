﻿using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using global::System.Runtime.CompilerServices;
    using global::System.Runtime.InteropServices;
    using System.Threading;
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
        public int numClearValues;
        public RenderPassBeginInfo(RenderPass renderPass, Framebuffer framebuffer, Rect2D renderArea, params ClearValue[] clearValues)
        {
            this.renderPass = renderPass;
            this.framebuffer = framebuffer;
            this.renderArea = renderArea;
            this.clearValues = clearValues;
            numClearValues = clearValues.Length;
        }

        public RenderPassBeginInfo(RenderPass renderPass, Framebuffer framebuffer, Rect2D renderArea, ClearValue[] clearValues, int num)
        {
            this.renderPass = renderPass;
            this.framebuffer = framebuffer;
            this.renderArea = renderArea;
            this.clearValues = clearValues;
            numClearValues = num;
        }

        public unsafe void ToNative(out VkRenderPassBeginInfo native)
        {
            native = VkRenderPassBeginInfo.New();
            native.renderPass = framebuffer.renderPass.handle;
            native.framebuffer = framebuffer.handle;
            native.renderArea = new VkRect2D(renderArea.x, renderArea.y, renderArea.width, renderArea.height);

            if (clearValues != null && clearValues.Length > 0)
            {
                native.clearValueCount = (uint)numClearValues;
                native.pClearValues = (VkClearValue*)Unsafe.AsPointer(ref clearValues[0]);
            }
            else
            {
                native.clearValueCount = 0;
                native.pClearValues = null;
            }
        }

    }

    [Flags]
    public enum CommandBufferUsageFlags
    {
        None = 0,
        OneTimeSubmit = 1,
        RenderPassContinue = 2,
        SimultaneousUse = 4
    }

    public enum QueryControlFlags
    {
        None = 0,
        Precise = 1
    }

    public enum QueryPipelineStatisticFlags
    {
        None = 0,
        InputAssemblyVertices = 1,
        InputAssemblyPrimitives = 2,
        VertexShaderInvocations = 4,
        GeometryShaderInvocations = 8,
        GeometryShaderPrimitives = 16,
        ClippingInvocations = 32,
        ClippingPrimitives = 64,
        FragmentShaderInvocations = 128,
        TessellationControlShaderPatches = 256,
        TessellationEvaluationShaderInvocations = 512,
        ComputeShaderInvocations = 1024
    }

    public struct CommandBufferInheritanceInfo
    {
        public RenderPass renderPass;
        public uint subpass;
        public Framebuffer framebuffer;
        public bool occlusionQueryEnable;
        public QueryControlFlags queryFlags;
        public QueryPipelineStatisticFlags pipelineStatistics;

        public unsafe void ToNative(out VkCommandBufferInheritanceInfo native)
        {
            native = VkCommandBufferInheritanceInfo.New();
            native.renderPass = renderPass.handle;
            native.subpass = subpass;
            native.framebuffer = framebuffer.handle;
            native.occlusionQueryEnable = occlusionQueryEnable;
            native.queryFlags = (VkQueryControlFlags)queryFlags;
            native.pipelineStatistics = (VkQueryPipelineStatisticFlags)pipelineStatistics;
        }
    }

    public class CommandBuffer : DisposeBase
    {
        internal VkCommandBuffer commandBuffer;
        public RenderPass renderPass;
        bool owner = false;
        bool opened = false;
        public bool IsOpen => opened;

        internal CommandBuffer(VkCommandBuffer cmdBuffer)
        {
            commandBuffer = cmdBuffer;
            owner = false;
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void Begin(CommandBufferUsageFlags flags = CommandBufferUsageFlags.None)
        {
            var cmdBufInfo = VkCommandBufferBeginInfo.New();
            cmdBufInfo.flags = (VkCommandBufferUsageFlags)flags;
            VulkanUtil.CheckResult(vkBeginCommandBuffer(commandBuffer, ref cmdBufInfo));
            opened = true;
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void Begin(CommandBufferUsageFlags flags, ref CommandBufferInheritanceInfo commandBufferInheritanceInfo)
        {
            commandBufferInheritanceInfo.ToNative(out VkCommandBufferInheritanceInfo cmdBufInfo);
            var cmdBufBeginInfo = VkCommandBufferBeginInfo.New();
            cmdBufBeginInfo.flags = (VkCommandBufferUsageFlags)flags;
            unsafe
            {
                cmdBufBeginInfo.pInheritanceInfo = &cmdBufInfo;
                VulkanUtil.CheckResult(vkBeginCommandBuffer(commandBuffer, ref cmdBufBeginInfo));
            }
            opened = true;
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void End()
        {
            VulkanUtil.CheckResult(vkEndCommandBuffer(commandBuffer));
            opened = false;
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void BeginRenderPass(ref RenderPassBeginInfo renderPassBeginInfo, SubpassContents contents)
        {
            renderPassBeginInfo.ToNative(out VkRenderPassBeginInfo vkRenderPassBeginInfo);
            vkCmdBeginRenderPass(commandBuffer, ref vkRenderPassBeginInfo, (VkSubpassContents)contents);
            renderPass = renderPassBeginInfo.renderPass;
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void EndRenderPass()
        {
            vkCmdEndRenderPass(commandBuffer);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void SetScissor(Rect2D scissor)
        {
            SetScissor(ref scissor);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void SetScissor(ref Rect2D pScissors)
        {
            vkCmdSetScissor(commandBuffer, 0, 1, Utilities.AsPointer(ref pScissors));
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void SetViewport(Viewport viewport)
        {
            SetViewport(ref viewport);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void SetViewport(ref Viewport pViewports)
        {
            vkCmdSetViewport(commandBuffer, 0, 1, Utilities.AsPointer(ref pViewports));
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void BindComputePipeline(Pass pass)
        {
            var pipe = pass.GetComputePipeline();
            vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Compute, pass.computeHandle);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void BindGraphicsResourceSet(PipelineLayout pipelineLayout, int firstSet, ResourceSet resourceSet, uint? dynamicOffset = null)
        {
            uint dynamicOffsetCount = 0;
            uint val;
            uint* pDynamicOffsets = null;
            if (dynamicOffset != null)
            {
                dynamicOffsetCount = 1;
                val = dynamicOffset.Value;
                pDynamicOffsets = (uint*)Unsafe.AsPointer(ref val);
            }

            BindResourceSet(PipelineBindPoint.Graphics, pipelineLayout, firstSet, resourceSet, dynamicOffsetCount, pDynamicOffsets);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void BindComputeResourceSet(PipelineLayout pipelineLayout, int firstSet, ResourceSet resourceSet, uint? dynamicOffset)
        {
            uint dynamicOffsetCount = 0;
            uint val;
            uint* pDynamicOffsets = null;
            if (dynamicOffset != null)
            {
                dynamicOffsetCount = 1;
                val = dynamicOffset.Value;
                pDynamicOffsets = (uint*)Unsafe.AsPointer(ref val);
            }

            BindResourceSet(PipelineBindPoint.Compute, pipelineLayout, firstSet, resourceSet, dynamicOffsetCount, pDynamicOffsets);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void BindResourceSet(PipelineBindPoint pipelineBindPoint,
            PipelineLayout pipelineLayout, int set, ResourceSet pDescriptorSets, uint dynamicOffsetCount = 0, uint* pDynamicOffsets = null)
        {
            vkCmdBindDescriptorSets(commandBuffer, (VkPipelineBindPoint)pipelineBindPoint, pipelineLayout.handle, (uint)set, 1, ref pDescriptorSets.descriptorSet, dynamicOffsetCount, pDynamicOffsets);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void BindPipeline(PipelineBindPoint pipelineBindPoint, VkPipeline pass)
        {
            vkCmdBindPipeline(commandBuffer, (VkPipelineBindPoint)pipelineBindPoint, pass);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void BindVertexBuffers(uint firstBinding, uint bindingCount, IntPtr pBuffers, ref ulong pOffsets)
        {
            vkCmdBindVertexBuffers(commandBuffer, firstBinding, bindingCount, pBuffers, ref pOffsets);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void BindVertexBuffer(uint firstBinding, DeviceBuffer buffer)
        {
            ulong pOffsets = 0;
            vkCmdBindVertexBuffers(commandBuffer, firstBinding, 1, ref buffer.buffer, ref pOffsets);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void BindIndexBuffer(DeviceBuffer buffer, ulong offset, IndexType indexType)
        {
            vkCmdBindIndexBuffer(commandBuffer, buffer.buffer, offset, (VkIndexType)indexType);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void PushConstants<T>(PipelineLayout pipelineLayout, ShaderStage shaderStage, int offset, ref T value) where T : struct
        {
            vkCmdPushConstants(commandBuffer, pipelineLayout.handle, (VkShaderStageFlags)shaderStage,
                (uint)offset, (uint)Utilities.SizeOf<T>(), Unsafe.AsPointer(ref value));
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void PushConstants(PipelineLayout pipelineLayout, ShaderStage shaderStage, int offset, int size, IntPtr value)
        {
            vkCmdPushConstants(commandBuffer, pipelineLayout.handle, (VkShaderStageFlags)shaderStage, (uint)offset, (uint)size, (void*)value);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void PushDescriptorSet(PipelineLayout pipelineLayout, ResourceSet resourceSet)
        {
            //vkCmdPushDescriptorSetKHR(commandBuffer, VkPipelineBindPoint.Graphics, pass.pipelineLayout, (uint)resourceSet.Set,
            //    (uint)resourceSet.writeDescriptorSets.Length, ref resourceSet.writeDescriptorSets[0]);

            Device.CmdPushDescriptorSetKHR(commandBuffer, VkPipelineBindPoint.Graphics, pipelineLayout.handle, (uint)resourceSet.Set,
                (uint)resourceSet.writeDescriptorSets.Length, (VkWriteDescriptorSet*)Unsafe.AsPointer(ref resourceSet.writeDescriptorSets[0]));
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance)
        {
            vkCmdDraw(commandBuffer, vertexCount, instanceCount, firstVertex, firstInstance);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void DrawIndexed(uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
        {
            vkCmdDrawIndexed(commandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
            Interlocked.Increment( ref Stats.drawCall);
            Interlocked.Add(ref Stats.triCount, (int)indexCount / 2);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void DrawGeometry(Geometry geometry, Pass pass, Material material)
        {
            var pipe = pass.GetGraphicsPipeline(renderPass, geometry);
            BindPipeline(PipelineBindPoint.Graphics, pipe);

            foreach(var rs in material.ResourceSet)
            {
                if (rs.Updated)
                {
                    BindResourceSet(PipelineBindPoint.Graphics, pass.PipelineLayout, rs.Set, rs);
                }
            }

            geometry.Draw(this);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void DrawGeometry(Geometry geometry, Pass pass, ResourceSet resourceSet)
        {
            var pipe = pass.GetGraphicsPipeline(renderPass, geometry);
            BindPipeline(PipelineBindPoint.Graphics, pipe);
            BindResourceSet(PipelineBindPoint.Graphics, pass.PipelineLayout, 0, resourceSet);
            geometry.Draw(this);
        }

        public void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
            vkCmdDispatch(commandBuffer, groupCountX, groupCountY, groupCountZ);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void ExecuteCommand(CommandBuffer cmdBuffer)
        {
            vkCmdExecuteCommands(commandBuffer, 1, ref cmdBuffer.commandBuffer);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void ExecuteCommand(CommandBufferPool cmdBuffer)
        {
            vkCmdExecuteCommands(commandBuffer, (uint)cmdBuffer.currentIndex, (VkCommandBuffer*)cmdBuffer.GetAddress(0));
        }

        public void CopyImage(Image srcImage, ImageLayout srcImageLayout, Image dstImage, ImageLayout dstImageLayout, ref ImageCopy region)
        {
            vkCmdCopyImage(commandBuffer, srcImage.handle, (VkImageLayout)srcImageLayout, dstImage.handle, (VkImageLayout)dstImageLayout, 1, ref Unsafe.As<ImageCopy, VkImageCopy>(ref region));
        }

        public void CopyImage(Image srcImage, ImageLayout srcImageLayout, Image dstImage, ImageLayout dstImageLayout, Span<ImageCopy> region)
        {
            vkCmdCopyImage(commandBuffer, srcImage.handle, (VkImageLayout)srcImageLayout, dstImage.handle, (VkImageLayout)dstImageLayout, (uint)region.Length, ref Unsafe.As<ImageCopy, VkImageCopy>(ref region[0]));
        }

        public void PipelineBarrier(PipelineStageFlags srcStageMask, PipelineStageFlags dstStageMask, DependencyFlags dependencyFlags,
            uint memoryBarrierCount, ref VkMemoryBarrier pMemoryBarriers, uint bufferMemoryBarrierCount, IntPtr pBufferMemoryBarriers,
            uint imageMemoryBarrierCount, ref VkImageMemoryBarrier pImageMemoryBarriers)
        {
            vkCmdPipelineBarrier(commandBuffer, (VkPipelineStageFlags)srcStageMask, (VkPipelineStageFlags)dstStageMask, (VkDependencyFlags)dependencyFlags, memoryBarrierCount, ref pMemoryBarriers, bufferMemoryBarrierCount, pBufferMemoryBarriers,
                imageMemoryBarrierCount, ref pImageMemoryBarriers);
        }

        public unsafe void PipelineBarrier(PipelineStageFlags srcStageMask, PipelineStageFlags dstStageMask, System.Span<ImageMemoryBarrier> barriers)
        {
            vkCmdPipelineBarrier(commandBuffer, (VkPipelineStageFlags)srcStageMask, (VkPipelineStageFlags)dstStageMask, 0, 0, null, 0, null, 
                (uint)barriers.Length, (VkImageMemoryBarrier*)Unsafe.AsPointer(ref  barriers[0]));
        }

        public void Reset(bool releaseRes)
        {
            vkResetCommandBuffer(commandBuffer, releaseRes ? VkCommandBufferResetFlags.ReleaseResources : VkCommandBufferResetFlags.None);
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
