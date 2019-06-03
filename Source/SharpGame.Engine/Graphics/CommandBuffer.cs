using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using global::System.Runtime.CompilerServices;
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

        public RenderPassBeginInfo(RenderPass renderPass, Framebuffer framebuffer, Rect2D renderArea, params ClearValue[] clearValues)
        {
            this.renderPass = renderPass;
            this.framebuffer = framebuffer;
            this.renderArea = renderArea;
            this.clearValues = clearValues;
        }

        public unsafe void ToNative(out VkRenderPassBeginInfo native)
        {
            native = VkRenderPassBeginInfo.New();
            native.renderPass = framebuffer.renderPass.handle;
            native.framebuffer = framebuffer.handle;
            native.renderArea = new VkRect2D(renderArea.x, renderArea.y, renderArea.width, renderArea.height);

            if (clearValues != null && clearValues.Length > 0)
            {
                native.clearValueCount = (uint)clearValues.Length;
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
        internal CommandBuffer(VkCommandBuffer cmdBuffer)
        {
            commandBuffer = cmdBuffer;
            owner = true;
        }

        protected override void Destroy()
        {
            base.Destroy();
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void Begin(CommandBufferUsageFlags flags = CommandBufferUsageFlags.None)
        {
            var cmdBufInfo = VkCommandBufferBeginInfo.New();
            cmdBufInfo.flags = (VkCommandBufferUsageFlags)flags;
            VulkanUtil.CheckResult(vkBeginCommandBuffer(commandBuffer, ref cmdBufInfo));
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
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void End()
        {
            VulkanUtil.CheckResult(vkEndCommandBuffer(commandBuffer));
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
        public void BindGraphicsPipeline(GraphicsPipeline pipeline)
        {
            var pipe = pipeline.GetGraphicsPipeline(renderPass, null, null);
            vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, pipeline.handle);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void BindComputePipeline(ComputePipeline pipeline)
        {
            var pipe = pipeline.GetComputePipeline(null);
            vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Compute, pipeline.handle);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void BindGraphicsResourceSet(GraphicsPipeline pipeline, int firstSet, ResourceSet resourceSet, uint[] dynamicOffsets = null)
        {
            uint dynamicOffsetCount = 0;
            uint* pDynamicOffsets = null;
            if (dynamicOffsets != null)
            {
                dynamicOffsetCount = 0;
                dynamicOffsetCount = (uint)dynamicOffsets.Length;
                pDynamicOffsets = (uint*)Unsafe.AsPointer(ref dynamicOffsets[0]);
            }

            BindResourceSet(PipelineBindPoint.Graphics, pipeline, firstSet, resourceSet, dynamicOffsetCount, pDynamicOffsets);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void BindComputeResourceSet(GraphicsPipeline pipeline, int firstSet, ResourceSet resourceSet, uint[] dynamicOffsets)
        {
            uint dynamicOffsetCount = 0;
            uint* pDynamicOffsets = null;
            if (dynamicOffsets != null)
            {
                dynamicOffsetCount = 0;
                dynamicOffsetCount = (uint)dynamicOffsets.Length;
                pDynamicOffsets = (uint*)Unsafe.AsPointer(ref dynamicOffsets[0]);
            }

            BindResourceSet(PipelineBindPoint.Compute, pipeline, firstSet, resourceSet, dynamicOffsetCount, pDynamicOffsets);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void BindResourceSet(PipelineBindPoint pipelineBindPoint,
            GraphicsPipeline pipeline, int set, ResourceSet pDescriptorSets, uint dynamicOffsetCount = 0, uint* pDynamicOffsets = null)
        {
            vkCmdBindDescriptorSets(commandBuffer, (VkPipelineBindPoint)pipelineBindPoint, pipeline.pipelineLayout, (uint)set, 1, ref pDescriptorSets.descriptorSet, dynamicOffsetCount, pDynamicOffsets);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void BindPipeline(PipelineBindPoint pipelineBindPoint, VkPipeline pipeline)
        {
            vkCmdBindPipeline(commandBuffer, (VkPipelineBindPoint)pipelineBindPoint, pipeline);
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
        public unsafe void PushConstants<T>(GraphicsPipeline pipeline, ShaderStage shaderStage, int offset, ref T value) where T : struct
        {
            vkCmdPushConstants(commandBuffer, pipeline.pipelineLayout, (VkShaderStageFlags)shaderStage,
                (uint)offset, (uint)Utilities.SizeOf<T>(), Unsafe.AsPointer(ref value));
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void PushConstants(GraphicsPipeline pipeline, ShaderStage shaderStage, int offset, int size, IntPtr value)
        {
            vkCmdPushConstants(commandBuffer, pipeline.pipelineLayout, (VkShaderStageFlags)shaderStage,
                (uint)offset, (uint)size, (void*)value);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void PushDescriptorSet(GraphicsPipeline pipeline, int set, ResourceSet resourceSet)
        {
            vkCmdPushDescriptorSetKHR(commandBuffer, VkPipelineBindPoint.Graphics, pipeline.pipelineLayout, (uint)set,
                (uint)resourceSet.writeDescriptorSets.Length, ref resourceSet.writeDescriptorSets[0]);
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
            Stats.drawCall++;
            Stats.triCount += indexCount / 2;
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void DrawGeometry(Geometry geometry, GraphicsPipeline pipeline, Material material)
        {
            var pipe = pipeline.GetGraphicsPipeline(renderPass, pipeline.Shader.Main, geometry);
            BindPipeline(PipelineBindPoint.Graphics, pipe);
            BindResourceSet(PipelineBindPoint.Graphics, pipeline, 0, material.ResourceSet);
            geometry.Draw(this);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void DrawGeometry(Geometry geometry, GraphicsPipeline pipeline, Pass shader, ResourceSet resourceSet)
        {
            var pipe = pipeline.GetGraphicsPipeline(renderPass, shader, geometry);
            BindPipeline(PipelineBindPoint.Graphics, pipe);
            BindResourceSet(PipelineBindPoint.Graphics, pipeline, 0, resourceSet);
            geometry.Draw(this);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void ExecuteCommand(CommandBuffer cmdBuffer)
        {
            vkCmdExecuteCommands(commandBuffer, 1, ref cmdBuffer.commandBuffer);
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
