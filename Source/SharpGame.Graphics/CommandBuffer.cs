﻿namespace SharpGame
{
    using global::System;
    using System.Collections.Generic;
    using global::System.Runtime.CompilerServices;
    using global::System.Runtime.InteropServices;
    using System.Threading;
    using static Vulkan;

    public unsafe partial class CommandBuffer : DisposeBase
    {
        internal VkCommandBuffer commandBuffer;
        public RenderPass renderPass;
        bool opened = false;
        public bool IsOpen => opened;
        public bool NeedSubmit { get; set; }

        VkPipeline currentPipeline;
        FixedArray8<VkDescriptorSet> descriptorSets = new FixedArray8<VkDescriptorSet>();
        FixedArray8<uint> dynamicOffsetCounts = new FixedArray8<uint>();
        FixedArray8<uint> dynamicOffsets = new FixedArray8<uint>();

        internal CommandBuffer(VkCommandBuffer cmdBuffer)
        {
            commandBuffer = cmdBuffer;
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void Begin(CommandBufferUsageFlags flags = CommandBufferUsageFlags.None)
        {
            var cmdBufInfo = new VkCommandBufferBeginInfo();
            cmdBufInfo.sType = VkStructureType.CommandBufferBeginInfo;
            cmdBufInfo.flags = (VkCommandBufferUsageFlags)flags;
            VulkanUtil.CheckResult(vkBeginCommandBuffer(commandBuffer, &cmdBufInfo));
            opened = true;
            NeedSubmit = true;
            ClearDescriptorSets();
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void Begin(CommandBufferUsageFlags flags, ref CommandBufferInheritanceInfo commandBufferInheritanceInfo)
        {
            commandBufferInheritanceInfo.ToNative(out VkCommandBufferInheritanceInfo cmdBufInfo);
            var cmdBufBeginInfo = new VkCommandBufferBeginInfo();
            cmdBufBeginInfo.sType = VkStructureType.CommandBufferBeginInfo;
            cmdBufBeginInfo.flags = (VkCommandBufferUsageFlags)flags;
            unsafe
            {
                cmdBufBeginInfo.pInheritanceInfo = &cmdBufInfo;
                VulkanUtil.CheckResult(vkBeginCommandBuffer(commandBuffer, &cmdBufBeginInfo));
            }
            opened = true;
            NeedSubmit = true;
            ClearDescriptorSets();
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void End()
        {
            VulkanUtil.CheckResult(vkEndCommandBuffer(commandBuffer));
            opened = false;
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void BeginRenderPass(in RenderPassBeginInfo renderPassBeginInfo, SubpassContents contents)
        {
            renderPassBeginInfo.ToNative(out VkRenderPassBeginInfo vkRenderPassBeginInfo);
            vkCmdBeginRenderPass(commandBuffer, &vkRenderPassBeginInfo, (VkSubpassContents)contents);
            renderPass = renderPassBeginInfo.renderPass;
            ClearDescriptorSets();
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void EndRenderPass()
        {
            vkCmdEndRenderPass(commandBuffer);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void SetScissor(in VkRect2D pScissors)
        {
            vkCmdSetScissor(commandBuffer, 0, 1, Utilities.InToPtr(in pScissors));
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void SetViewport(in VkViewport pViewports)
        {
            vkCmdSetViewport(commandBuffer, 0, 1, Utilities.InToPtr(in pViewports));
        }

        void ClearDescriptorSets()
        {
            currentPipeline = default;
            descriptorSets.Clear();
            dynamicOffsetCounts.Clear();
            dynamicOffsets.Clear();
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void BindComputePipeline(Pass pass)
        {
            var pipe = pass.GetComputePipeline();

            if(pipe.handle != currentPipeline)
            {
                ClearDescriptorSets();
                currentPipeline = pipe.handle;
                vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Compute, pipe.handle);
            }
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void BindGraphicsResourceSet(PipelineLayout pipelineLayout, int firstSet, DescriptorSet resourceSet, int dynamicOffset = -1)
        {
            uint dynamicOffsetCount = 0;
            uint val;
            uint* pDynamicOffsets = null;
            if (dynamicOffset >= 0)
            {
                dynamicOffsetCount = 1;
                val = (uint)dynamicOffset;
                pDynamicOffsets = (uint*)Unsafe.AsPointer(ref val);
            }

            BindResourceSet(PipelineBindPoint.Graphics, pipelineLayout, firstSet, resourceSet, dynamicOffsetCount, pDynamicOffsets);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void BindComputeResourceSet(PipelineLayout pipelineLayout, int firstSet, DescriptorSet resourceSet, int dynamicOffset = -1)
        {
            uint dynamicOffsetCount = 0;
            uint val;
            uint* pDynamicOffsets = null;
            if (dynamicOffset >= 0)
            {
                dynamicOffsetCount = 1;
                val = (uint)dynamicOffset;
                pDynamicOffsets = (uint*)Unsafe.AsPointer(ref val);
            }

            BindResourceSet(PipelineBindPoint.Compute, pipelineLayout, firstSet, resourceSet, dynamicOffsetCount, pDynamicOffsets);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void BindResourceSet(PipelineBindPoint pipelineBindPoint,
            PipelineLayout pipelineLayout, int set, DescriptorSet pDescriptorSets, uint dynamicOffsetCount = 0, uint* pDynamicOffsets = null)
        {
            if(descriptorSets[set] != pDescriptorSets.descriptorSet[Graphics.Instance.WorkContext]
                || dynamicOffsetCounts[set] != dynamicOffsetCount
                || (pDynamicOffsets != null && dynamicOffsets[set] != *pDynamicOffsets))
            {

                descriptorSets[set] = pDescriptorSets.descriptorSet[Graphics.Instance.WorkContext];
                dynamicOffsetCounts[set] = dynamicOffsetCount;
                if(dynamicOffsetCount > 0)
                dynamicOffsets[set] = *pDynamicOffsets;

                var t = pDescriptorSets.descriptorSet[Graphics.Instance.WorkContext];

                vkCmdBindDescriptorSets(commandBuffer, (VkPipelineBindPoint)pipelineBindPoint, pipelineLayout.handle, (uint)set, 1, &t, dynamicOffsetCount, pDynamicOffsets);
            }
        }

        /*
        [MethodImpl((MethodImplOptions)0x100)]
        public void BindPipeline(PipelineBindPoint pipelineBindPoint, Pass pass, Geometry geometry)
        {
            var pipeline = pass.GetGraphicsPipeline(renderPass, geometry);           
            vkCmdBindPipeline(commandBuffer, (VkPipelineBindPoint)pipelineBindPoint, pipeline.handle);
        }*/

        [MethodImpl((MethodImplOptions)0x100)]
        public void BindPipeline(PipelineBindPoint pipelineBindPoint, Pipeline pipeline)
        {
            if (pipeline.handle != currentPipeline)
            {
                ClearDescriptorSets();
                currentPipeline = pipeline.handle;

                vkCmdBindPipeline(commandBuffer, (VkPipelineBindPoint)pipelineBindPoint, pipeline.handle);
            }

            //vkCmdBindPipeline(commandBuffer, (VkPipelineBindPoint)pipelineBindPoint, pipeline.handle);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void BindVertexBuffers(uint firstBinding, uint bindingCount, IntPtr pBuffers, ref ulong pOffsets)
        {
            vkCmdBindVertexBuffers(commandBuffer, firstBinding, bindingCount, (VkBuffer*)pBuffers, Utilities.AsPtr(ref pOffsets));
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void BindVertexBuffers(uint firstBinding, Span<VkBuffer> pBuffers, ref ulong pOffsets)
        {
            vkCmdBindVertexBuffers(commandBuffer, firstBinding, (uint)pBuffers.Length, Utilities.AsPtr(ref pBuffers[0]), Utilities.AsPtr(ref pOffsets));
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void BindVertexBuffer(uint firstBinding, Buffer buffer, ulong pOffsets = 0)
        {
            vkCmdBindVertexBuffers(commandBuffer, firstBinding, 1, Utilities.AsPtr(ref buffer.buffer), &pOffsets);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void BindIndexBuffer(Buffer buffer, ulong offset, IndexType indexType)
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
        public void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance)
        {
            vkCmdDraw(commandBuffer, vertexCount, instanceCount, firstVertex, firstInstance);
            Interlocked.Increment(ref Stats.drawCall);
            Interlocked.Add(ref Stats.triCount, (int)vertexCount / 3);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void DrawIndexed(uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance)
        {
            vkCmdDrawIndexed(commandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
            Interlocked.Increment(ref Stats.drawCall);
            Interlocked.Add(ref Stats.triCount, (int)indexCount / 3);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void DrawGeometry(Geometry geometry, Pass pass, uint subPass, Material material)
        {
            var pipe = pass.GetGraphicsPipeline(renderPass, subPass, geometry);
            BindPipeline(PipelineBindPoint.Graphics, pipe);

            material.Bind(pass.passIndex, this);

            geometry.Draw(this);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void DrawGeometry(Geometry geometry, Pass pass, uint subPass, Span<DescriptorSet> resourceSet)
        {
            var pipe = pass.GetGraphicsPipeline(renderPass, subPass, geometry);
            BindPipeline(PipelineBindPoint.Graphics, pipe);
            for (int i = 0; i < resourceSet.Length; i++)
            {
                BindResourceSet(PipelineBindPoint.Graphics, pass.PipelineLayout, i, resourceSet[i]);
            }
            geometry.Draw(this);
        }

        public void DrawIndirect(Buffer buffer, ulong offset, uint drawCount, uint stride)
        {
            vkCmdDrawIndirect(commandBuffer, buffer.buffer, offset, drawCount, stride); 
            Interlocked.Increment(ref Stats.drawIndirect);
        }

        public void DrawIndexedIndirect(Buffer buffer, ulong offset, uint drawCount, uint stride)
        {
            vkCmdDrawIndexedIndirect(commandBuffer, buffer.buffer, offset, drawCount, stride);
            Interlocked.Increment(ref Stats.drawIndirect);
        }

        public void DispatchIndirect(Buffer buffer, ulong offset)
        {
            vkCmdDispatchIndirect(commandBuffer, buffer.buffer, offset); 
            Interlocked.Increment(ref Stats.dispatchIndirect);
        }

        public void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        {
            vkCmdDispatch(commandBuffer, groupCountX, groupCountY, groupCountZ);
            Interlocked.Increment(ref Stats.dispatch);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void ExecuteCommand(CommandBuffer cmdBuffer)
        {
            vkCmdExecuteCommands(commandBuffer, 1, Utilities.AsPtr(ref cmdBuffer.commandBuffer));

            cmdBuffer.NeedSubmit = false;
        }

        public void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ref VkBufferCopy region)
        {
            vkCmdCopyBuffer(commandBuffer, srcBuffer.buffer, dstBuffer.buffer, 1, Utilities.AsPtr(ref region));
        }

        public void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, Span<VkBufferCopy> pRegions)
        {
            vkCmdCopyBuffer(commandBuffer, srcBuffer.buffer, dstBuffer.buffer, (uint)pRegions.Length, Utilities.AsPtr(ref pRegions[0]));
        }

        public void BlitImage(Image srcImage, ImageLayout srcImageLayout, Image dstImage, ImageLayout dstImageLayout, ref VkImageBlit pRegion, VkFilter filter)
        {
            vkCmdBlitImage(commandBuffer, srcImage.handle, (VkImageLayout)srcImageLayout, dstImage.handle, (VkImageLayout)dstImageLayout, 1, (VkImageBlit*)Unsafe.AsPointer(ref pRegion), filter);
        }

        public void BlitImage(Image srcImage, ImageLayout srcImageLayout, Image dstImage, ImageLayout dstImageLayout, Span<VkImageBlit> pRegions, VkFilter filter)
        {
            vkCmdBlitImage(commandBuffer, srcImage.handle, (VkImageLayout)srcImageLayout, dstImage.handle, (VkImageLayout)dstImageLayout, (uint)pRegions.Length, (VkImageBlit*)Unsafe.AsPointer(ref pRegions[0]), filter);
        }

        public void CopyImage(Image srcImage, ImageLayout srcImageLayout, Image dstImage, ImageLayout dstImageLayout, ref VkImageCopy region)
        {
            vkCmdCopyImage(commandBuffer, srcImage.handle, (VkImageLayout)srcImageLayout, dstImage.handle, (VkImageLayout)dstImageLayout, 1, Utilities.AsPtr(ref region));
        }

        public void CopyImage(Image srcImage, ImageLayout srcImageLayout, Image dstImage, ImageLayout dstImageLayout, Span<VkImageCopy> region)
        {
            vkCmdCopyImage(commandBuffer, srcImage.handle, (VkImageLayout)srcImageLayout, dstImage.handle, (VkImageLayout)dstImageLayout, (uint)region.Length, Utilities.AsPtr(ref region[0]));
        }

        public void CopyBufferToImage(Buffer srcBuffer, Image dstImage, ImageLayout dstImageLayout, ref VkBufferImageCopy region)
        {
            vkCmdCopyBufferToImage(commandBuffer, srcBuffer.buffer, dstImage.handle, (VkImageLayout)dstImageLayout, 1, Utilities.AsPtr(ref region));
        }

        public void CopyBufferToImage(Buffer srcBuffer, Image dstImage, ImageLayout dstImageLayout, Span<VkBufferImageCopy> pRegions)
        {
            vkCmdCopyBufferToImage(commandBuffer, srcBuffer.buffer, dstImage.handle, (VkImageLayout)dstImageLayout, (uint)pRegions.Length, Utilities.AsPtr(ref pRegions[0]));
        }

        public void FillBuffer(Buffer dstBuffer, ulong dstOffset, ulong size, uint data)
        {
            vkCmdFillBuffer(commandBuffer, dstBuffer.buffer, dstOffset, size, data);
        }

        public void ResetQueryPool(QueryPool queryPool, uint firstQuery, uint queryCount)
        {
            vkCmdResetQueryPool(commandBuffer, queryPool.handle, firstQuery, queryCount);
        }

        public void WriteTimestamp(PipelineStageFlags pipelineStage, QueryPool queryPool, uint query)
        {
            vkCmdWriteTimestamp(commandBuffer, (VkPipelineStageFlags)pipelineStage, queryPool.handle, (uint)query);
        }

        public void NextSubpass(SubpassContents contents)
        {
            vkCmdNextSubpass(commandBuffer, (VkSubpassContents)contents);
        }

        public void PipelineBarrier(VkPipelineStageFlags srcStageMask, VkPipelineStageFlags dstStageMask, VkDependencyFlags dependencyFlags,
            uint memoryBarrierCount, ref VkMemoryBarrier pMemoryBarriers, uint bufferMemoryBarrierCount, IntPtr pBufferMemoryBarriers,
            uint imageMemoryBarrierCount, ref VkImageMemoryBarrier pImageMemoryBarriers)
        {
            vkCmdPipelineBarrier(commandBuffer, (VkPipelineStageFlags)srcStageMask, (VkPipelineStageFlags)dstStageMask, (VkDependencyFlags)dependencyFlags, memoryBarrierCount, Utilities.AsPtr(ref pMemoryBarriers), bufferMemoryBarrierCount, (VkBufferMemoryBarrier*)pBufferMemoryBarriers,
                imageMemoryBarrierCount, Utilities.AsPtr(ref pImageMemoryBarriers));
        }

        public unsafe void PipelineBarrier(VkPipelineStageFlags srcStageMask, VkPipelineStageFlags dstStageMask, VkDependencyFlags dependencyFlags,
            uint memoryBarrierCount, MemoryBarrier* pMemoryBarriers, uint bufferMemoryBarrierCount, BufferMemoryBarrier* pBufferMemoryBarriers,
            uint imageMemoryBarrierCount, VkImageMemoryBarrier* pImageMemoryBarriers)
        {
            vkCmdPipelineBarrier(commandBuffer, (VkPipelineStageFlags)srcStageMask, (VkPipelineStageFlags)dstStageMask, (VkDependencyFlags)dependencyFlags, memoryBarrierCount, (VkMemoryBarrier*)pMemoryBarriers,
                bufferMemoryBarrierCount, (VkBufferMemoryBarrier*) pBufferMemoryBarriers,
                imageMemoryBarrierCount, (VkImageMemoryBarrier*)pImageMemoryBarriers);
        }

        public unsafe void PipelineBarrier(VkPipelineStageFlags srcStageMask, VkPipelineStageFlags dstStageMask, ref VkBufferMemoryBarrier barrier, uint bufferMemoryBarrierCount = 1)
        {
            vkCmdPipelineBarrier(commandBuffer, (VkPipelineStageFlags)srcStageMask, (VkPipelineStageFlags)dstStageMask,
                0, 0, null, bufferMemoryBarrierCount, (VkBufferMemoryBarrier*)Unsafe.AsPointer(ref barrier), 0, null);
        }

        public unsafe void PipelineBarrier(VkPipelineStageFlags srcStageMask, VkPipelineStageFlags dstStageMask, Span<VkBufferMemoryBarrier> barrier)
        {
            vkCmdPipelineBarrier(commandBuffer, (VkPipelineStageFlags)srcStageMask, (VkPipelineStageFlags)dstStageMask,
                0, 0, null, (uint)barrier.Length, (VkBufferMemoryBarrier*)Unsafe.AsPointer(ref barrier[0]), 0, null);
        }

        public unsafe void PipelineBarrier(VkPipelineStageFlags srcStageMask, VkPipelineStageFlags dstStageMask, ref VkImageMemoryBarrier barrier)
        {
            vkCmdPipelineBarrier(commandBuffer, (VkPipelineStageFlags)srcStageMask, (VkPipelineStageFlags)dstStageMask,
                0, 0, null, 0, null, 1, (VkImageMemoryBarrier*)Unsafe.AsPointer(ref barrier));
        }

        public unsafe void PipelineBarrier(VkPipelineStageFlags srcStageMask, VkPipelineStageFlags dstStageMask, Span<VkImageMemoryBarrier> barriers)
        {
            vkCmdPipelineBarrier(commandBuffer, (VkPipelineStageFlags)srcStageMask, (VkPipelineStageFlags)dstStageMask, 0, 0, null, 0, null, 
                (uint)barriers.Length, (VkImageMemoryBarrier*)Unsafe.AsPointer(ref  barriers[0]));
        }

        // Fixed sub resource on first mip level and layer
        public void SetImageLayout(
            Image image,
            VkImageAspectFlags aspectMask,
            VkImageLayout oldImageLayout,
            VkImageLayout newImageLayout,
            VkPipelineStageFlags srcStageMask = VkPipelineStageFlags.AllCommands,
            VkPipelineStageFlags dstStageMask = VkPipelineStageFlags.AllCommands)
        {
            var subresourceRange = new VkImageSubresourceRange
            {
                aspectMask = aspectMask,
                baseMipLevel = 0,
                levelCount = 1,
                layerCount = 1
            };

            SetImageLayout(image, aspectMask, oldImageLayout, newImageLayout, subresourceRange);
        }

        public void SetImageLayout(
            Image image,
            VkImageAspectFlags aspectMask,
            VkImageLayout oldImageLayout,
            VkImageLayout newImageLayout,
            VkImageSubresourceRange subresourceRange,
            VkPipelineStageFlags srcStageMask = VkPipelineStageFlags.AllCommands,
            VkPipelineStageFlags dstStageMask = VkPipelineStageFlags.AllCommands)
        {
            // Create an image barrier object
            var imageMemoryBarrier = new VkImageMemoryBarrier
            {
                sType = VkStructureType.ImageMemoryBarrier,
                image = image.handle,
                oldLayout = oldImageLayout,
                newLayout = newImageLayout,
                subresourceRange = subresourceRange
            };

            // Source layouts (old)
            // Source access mask controls actions that have to be finished on the old layout
            // before it will be transitioned to the new layout
            switch (oldImageLayout)
            {
                case VkImageLayout.Undefined:
                    // Image layout is undefined (or does not matter)
                    // Only valid as initial layout
                    // No flags required, listed only for completeness
                    imageMemoryBarrier.srcAccessMask = 0;
                    break;

                case VkImageLayout.Preinitialized:
                    // Image is preinitialized
                    // Only valid as initial layout for linear images, preserves memory contents
                    // Make sure host writes have been finished
                    imageMemoryBarrier.srcAccessMask = VkAccessFlags.HostWrite;
                    break;

                case VkImageLayout.ColorAttachmentOptimal:
                    // Image is a color attachment
                    // Make sure any writes to the color buffer have been finished
                    imageMemoryBarrier.srcAccessMask = VkAccessFlags.ColorAttachmentWrite;
                    break;

                case VkImageLayout.DepthStencilAttachmentOptimal:
                    // Image is a depth/stencil attachment
                    // Make sure any writes to the depth/stencil buffer have been finished
                    imageMemoryBarrier.srcAccessMask = VkAccessFlags.DepthStencilAttachmentWrite;
                    break;

                case VkImageLayout.TransferSrcOptimal:
                    // Image is a transfer source 
                    // Make sure any reads from the image have been finished
                    imageMemoryBarrier.srcAccessMask = VkAccessFlags.TransferRead;
                    break;

                case VkImageLayout.TransferDstOptimal:
                    // Image is a transfer destination
                    // Make sure any writes to the image have been finished
                    imageMemoryBarrier.srcAccessMask = VkAccessFlags.TransferWrite;
                    break;

                case VkImageLayout.ShaderReadOnlyOptimal:
                    // Image is read by a shader
                    // Make sure any shader reads from the image have been finished
                    imageMemoryBarrier.srcAccessMask = VkAccessFlags.ShaderRead;
                    break;
            }

            // Target layouts (new)
            // Destination access mask controls the dependency for the new image layout
            switch (newImageLayout)
            {
                case VkImageLayout.TransferDstOptimal:
                    // Image will be used as a transfer destination
                    // Make sure any writes to the image have been finished
                    imageMemoryBarrier.dstAccessMask = VkAccessFlags.TransferWrite;
                    break;

                case VkImageLayout.TransferSrcOptimal:
                    // Image will be used as a transfer source
                    // Make sure any reads from and writes to the image have been finished
                    imageMemoryBarrier.srcAccessMask = imageMemoryBarrier.srcAccessMask | VkAccessFlags.TransferRead;
                    imageMemoryBarrier.dstAccessMask = VkAccessFlags.TransferRead;
                    break;

                case VkImageLayout.ColorAttachmentOptimal:
                    // Image will be used as a color attachment
                    // Make sure any writes to the color buffer have been finished
                    imageMemoryBarrier.srcAccessMask = VkAccessFlags.TransferRead;
                    imageMemoryBarrier.dstAccessMask = VkAccessFlags.ColorAttachmentWrite;
                    break;

                case VkImageLayout.DepthStencilAttachmentOptimal:
                    // Image layout will be used as a depth/stencil attachment
                    // Make sure any writes to depth/stencil buffer have been finished
                    imageMemoryBarrier.dstAccessMask = imageMemoryBarrier.dstAccessMask | VkAccessFlags.DepthStencilAttachmentWrite;
                    break;

                case VkImageLayout.ShaderReadOnlyOptimal:
                    // Image will be read in a shader (sampler, input attachment)
                    // Make sure any writes to the image have been finished
                    if (imageMemoryBarrier.srcAccessMask == 0)
                    {
                        imageMemoryBarrier.srcAccessMask = VkAccessFlags.HostWrite | VkAccessFlags.TransferWrite;
                    }
                    imageMemoryBarrier.dstAccessMask = VkAccessFlags.ShaderRead;
                    break;
            }

            // Put barrier inside setup command buffer
            PipelineBarrier(srcStageMask, dstStageMask, ref imageMemoryBarrier);
        }

        public void Reset(bool releaseRes)
        {
            vkResetCommandBuffer(commandBuffer, releaseRes ? VkCommandBufferResetFlags.ReleaseResources : VkCommandBufferResetFlags.None);
        }
    }

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
        public VkRect2D renderArea;
        public ClearValue[] clearValues;
        public int numClearValues;

        public RenderPassBeginInfo(RenderPass renderPass, Framebuffer framebuffer, VkRect2D renderArea, params ClearValue[] clearValues)
        {
            this.renderPass = renderPass;
            this.framebuffer = framebuffer;
            this.renderArea = renderArea;
            this.clearValues = clearValues;
            numClearValues = clearValues.Length;
        }

        public RenderPassBeginInfo(RenderPass renderPass, Framebuffer framebuffer, VkRect2D renderArea, ClearValue[] clearValues, int num)
        {
            this.renderPass = renderPass;
            this.framebuffer = framebuffer;
            this.renderArea = renderArea;
            this.clearValues = clearValues;
            numClearValues = num;
        }

        public unsafe void ToNative(out VkRenderPassBeginInfo native)
        {
            native = new VkRenderPassBeginInfo();
            native.sType = VkStructureType.RenderPassBeginInfo;
            native.renderPass = framebuffer.renderPass.handle;
            native.framebuffer = framebuffer.handle;
            native.renderArea = new VkRect2D(renderArea.offset, renderArea.extent);

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
            native = new VkCommandBufferInheritanceInfo();
            native.sType = VkStructureType.CommandBufferInheritanceInfo;
            native.renderPass = renderPass.handle;
            native.subpass = subpass;
            native.framebuffer = framebuffer.handle;
            native.occlusionQueryEnable = occlusionQueryEnable;
            native.queryFlags = (VkQueryControlFlags)queryFlags;
            native.pipelineStatistics = (VkQueryPipelineStatisticFlags)pipelineStatistics;
        }
    }


}
