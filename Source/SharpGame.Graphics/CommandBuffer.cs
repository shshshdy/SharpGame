﻿namespace SharpGame
{
    using global::System;
    using System.Collections.Generic;
    using global::System.Runtime.CompilerServices;
    using global::System.Runtime.InteropServices;
    using System.Threading;
    using static Vulkan;

    public ref struct RenderPassBeginInfo
    {
        public RenderPass renderPass;
        internal VkRenderPassBeginInfo native;
        public unsafe RenderPassBeginInfo(RenderPass renderPass, Framebuffer framebuffer, VkRect2D renderArea, Span<VkClearValue> clearValues)
        {
            this.renderPass = renderPass;

            fixed (VkClearValue* pClearValues = clearValues)
            {
                native = new VkRenderPassBeginInfo
                {
                    sType = VkStructureType.RenderPassBeginInfo,
                    renderPass = framebuffer.renderPass.handle,
                    framebuffer = framebuffer.handle,
                    renderArea = new VkRect2D(renderArea.offset, renderArea.extent),
                    pClearValues = pClearValues,
                    clearValueCount = (uint)clearValues.Length
                };
            }
        }

    }

    public ref struct CommandBufferInheritanceInfo
    {
        internal VkCommandBufferInheritanceInfo native;
        public unsafe CommandBufferInheritanceInfo(Framebuffer framebuffer, RenderPass renderPass, uint subpass, bool occlusionQueryEnable = false,
            VkQueryControlFlags queryFlags = VkQueryControlFlags.None, VkQueryPipelineStatisticFlags pipelineStatistics = VkQueryPipelineStatisticFlags.None)
        {
            native = new VkCommandBufferInheritanceInfo
            {
                sType = VkStructureType.CommandBufferInheritanceInfo,
                renderPass = renderPass.handle,
                subpass = subpass,
                framebuffer = framebuffer.handle,
                occlusionQueryEnable = occlusionQueryEnable,
                queryFlags = (VkQueryControlFlags)queryFlags,
                pipelineStatistics = (VkQueryPipelineStatisticFlags)pipelineStatistics
            };
        }
    }

    public unsafe partial class CommandBuffer : DisposeBase
    {
        internal VkCommandBuffer commandBuffer;
        public RenderPass renderPass;
        bool opened = false;
        public bool IsOpen => opened;

        VkPipeline currentPipeline;
        FixedArray8<VkDescriptorSet> descriptorSets = new FixedArray8<VkDescriptorSet>();
        FixedArray8<uint> dynamicOffsetCounts = new FixedArray8<uint>();
        FixedArray8<uint> dynamicOffsets = new FixedArray8<uint>();

        internal CommandBuffer(VkCommandBuffer cmdBuffer)
        {
            commandBuffer = cmdBuffer;
        }

        public static implicit operator VkCommandBuffer(CommandBuffer cmd) => cmd.commandBuffer;

        [MethodImpl((MethodImplOptions)0x100)]
        public void Begin(VkCommandBufferUsageFlags flags = VkCommandBufferUsageFlags.None)
        {
            var cmdBufInfo = new VkCommandBufferBeginInfo();
            cmdBufInfo.sType = VkStructureType.CommandBufferBeginInfo;
            cmdBufInfo.flags = (VkCommandBufferUsageFlags)flags;
            VulkanUtil.CheckResult(vkBeginCommandBuffer(commandBuffer, &cmdBufInfo));
            opened = true;
            ClearDescriptorSets();
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void Begin(VkCommandBufferUsageFlags flags, ref CommandBufferInheritanceInfo commandBufferInheritanceInfo)
        {
            unsafe
            {
                var cmdBufBeginInfo = new VkCommandBufferBeginInfo
                {
                    sType = VkStructureType.CommandBufferBeginInfo,
                    flags = (VkCommandBufferUsageFlags)flags,
                    pInheritanceInfo = (VkCommandBufferInheritanceInfo*)Unsafe.AsPointer(ref commandBufferInheritanceInfo.native)
                };
                VulkanUtil.CheckResult(vkBeginCommandBuffer(commandBuffer, &cmdBufBeginInfo));
            }
            opened = true;
            ClearDescriptorSets();
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void End()
        {
            VulkanUtil.CheckResult(vkEndCommandBuffer(commandBuffer));
            opened = false;
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void BeginRenderPass(ref RenderPassBeginInfo renderPassBeginInfo, VkSubpassContents contents)
        {
            vkCmdBeginRenderPass(commandBuffer, Utilities.AsPtr(ref renderPassBeginInfo.native), (VkSubpassContents)contents);
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

            if (pipe != currentPipeline)
            {
                ClearDescriptorSets();
                currentPipeline = pipe;
                vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Compute, pipe);
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

            BindResourceSet(VkPipelineBindPoint.Graphics, pipelineLayout, firstSet, resourceSet, dynamicOffsetCount, pDynamicOffsets);
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

            BindResourceSet(VkPipelineBindPoint.Compute, pipelineLayout, firstSet, resourceSet, dynamicOffsetCount, pDynamicOffsets);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void BindResourceSet(VkPipelineBindPoint pipelineBindPoint,
            PipelineLayout pipelineLayout, int set, DescriptorSet pDescriptorSets, uint dynamicOffsetCount = 0, uint* pDynamicOffsets = null)
        {
            if (descriptorSets[set] != pDescriptorSets.descriptorSet[Graphics.Instance.WorkContext]
                || dynamicOffsetCounts[set] != dynamicOffsetCount
                || (pDynamicOffsets != null && dynamicOffsets[set] != *pDynamicOffsets))
            {

                descriptorSets[set] = pDescriptorSets.descriptorSet[Graphics.Instance.WorkContext];
                dynamicOffsetCounts[set] = dynamicOffsetCount;
                if (dynamicOffsetCount > 0)
                    dynamicOffsets[set] = *pDynamicOffsets;

                var t = pDescriptorSets.descriptorSet[Graphics.Instance.WorkContext];

                vkCmdBindDescriptorSets(commandBuffer, (VkPipelineBindPoint)pipelineBindPoint, pipelineLayout.handle, (uint)set, 1, &t, dynamicOffsetCount, pDynamicOffsets);
            }
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void BindPipeline(VkPipelineBindPoint pipelineBindPoint, VkPipeline pipeline)
        {
            if (pipeline != currentPipeline)
            {
                ClearDescriptorSets();
                currentPipeline = pipeline;

                vkCmdBindPipeline(commandBuffer, (VkPipelineBindPoint)pipelineBindPoint, pipeline);
            }

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
            vkCmdBindVertexBuffers(commandBuffer, firstBinding, 1, Utilities.AsPtr(ref buffer.handle), &pOffsets);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void BindIndexBuffer(Buffer buffer, ulong offset, VkIndexType indexType)
        {
            vkCmdBindIndexBuffer(commandBuffer, buffer.handle, offset, (VkIndexType)indexType);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void PushConstants<T>(PipelineLayout pipelineLayout, VkShaderStageFlags shaderStage, int offset, ref T value) where T : struct
        {
            vkCmdPushConstants(commandBuffer, pipelineLayout.handle, (VkShaderStageFlags)shaderStage,
                (uint)offset, (uint)Utilities.SizeOf<T>(), Unsafe.AsPointer(ref value));
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void PushConstants(PipelineLayout pipelineLayout, VkShaderStageFlags shaderStage, int offset, int size, IntPtr value)
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
            BindPipeline(VkPipelineBindPoint.Graphics, pipe);

            material.Bind(pass.passIndex, this);

            geometry.Draw(this);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe void DrawGeometry(Geometry geometry, Pass pass, uint subPass, Span<DescriptorSet> resourceSet)
        {
            var pipe = pass.GetGraphicsPipeline(renderPass, subPass, geometry);
            BindPipeline(VkPipelineBindPoint.Graphics, pipe);
            for (int i = 0; i < resourceSet.Length; i++)
            {
                BindResourceSet(VkPipelineBindPoint.Graphics, pass.PipelineLayout, i, resourceSet[i]);
            }
            geometry.Draw(this);
        }

        public void DrawIndirect(Buffer buffer, ulong offset, uint drawCount, uint stride)
        {
            vkCmdDrawIndirect(commandBuffer, buffer.handle, offset, drawCount, stride);
            Interlocked.Increment(ref Stats.drawIndirect);
        }

        public void DrawIndexedIndirect(Buffer buffer, ulong offset, uint drawCount, uint stride)
        {
            vkCmdDrawIndexedIndirect(commandBuffer, buffer.handle, offset, drawCount, stride);
            Interlocked.Increment(ref Stats.drawIndirect);
        }

        public void DispatchIndirect(Buffer buffer, ulong offset)
        {
            vkCmdDispatchIndirect(commandBuffer, buffer.handle, offset);
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
        }

        public void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ref VkBufferCopy region)
        {
            vkCmdCopyBuffer(commandBuffer, srcBuffer.handle, dstBuffer.handle, 1, Utilities.AsPtr(ref region));
        }

        public void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, Span<VkBufferCopy> pRegions)
        {
            vkCmdCopyBuffer(commandBuffer, srcBuffer.handle, dstBuffer.handle, (uint)pRegions.Length, Utilities.AsPtr(ref pRegions[0]));
        }

        public void BlitImage(Image srcImage, VkImageLayout srcImageLayout, Image dstImage, VkImageLayout dstImageLayout, ref VkImageBlit pRegion, VkFilter filter)
        {
            vkCmdBlitImage(commandBuffer, srcImage.handle, (VkImageLayout)srcImageLayout, dstImage.handle, (VkImageLayout)dstImageLayout, 1, (VkImageBlit*)Unsafe.AsPointer(ref pRegion), filter);
        }

        public void BlitImage(Image srcImage, VkImageLayout srcImageLayout, Image dstImage, VkImageLayout dstImageLayout, Span<VkImageBlit> pRegions, VkFilter filter)
        {
            vkCmdBlitImage(commandBuffer, srcImage.handle, (VkImageLayout)srcImageLayout, dstImage.handle, (VkImageLayout)dstImageLayout, (uint)pRegions.Length, (VkImageBlit*)Unsafe.AsPointer(ref pRegions[0]), filter);
        }

        public void CopyImage(Image srcImage, VkImageLayout srcImageLayout, Image dstImage, VkImageLayout dstImageLayout, ref VkImageCopy region)
        {
            vkCmdCopyImage(commandBuffer, srcImage.handle, (VkImageLayout)srcImageLayout, dstImage.handle, (VkImageLayout)dstImageLayout, 1, Utilities.AsPtr(ref region));
        }

        public void CopyImage(Image srcImage, VkImageLayout srcImageLayout, Image dstImage, VkImageLayout dstImageLayout, Span<VkImageCopy> region)
        {
            vkCmdCopyImage(commandBuffer, srcImage.handle, (VkImageLayout)srcImageLayout, dstImage.handle, (VkImageLayout)dstImageLayout, (uint)region.Length, Utilities.AsPtr(ref region[0]));
        }

        public void CopyBufferToImage(Buffer srcBuffer, Image dstImage, VkImageLayout dstImageLayout, ref VkBufferImageCopy region)
        {
            vkCmdCopyBufferToImage(commandBuffer, srcBuffer.handle, dstImage.handle, (VkImageLayout)dstImageLayout, 1, Utilities.AsPtr(ref region));
        }

        public void CopyBufferToImage(Buffer srcBuffer, Image dstImage, VkImageLayout dstImageLayout, Span<VkBufferImageCopy> pRegions)
        {
            vkCmdCopyBufferToImage(commandBuffer, srcBuffer.handle, dstImage.handle, (VkImageLayout)dstImageLayout, (uint)pRegions.Length, Utilities.AsPtr(ref pRegions[0]));
        }

        public void FillBuffer(Buffer dstBuffer, ulong dstOffset, ulong size, uint data)
        {
            vkCmdFillBuffer(commandBuffer, dstBuffer.handle, dstOffset, size, data);
        }

        public void ResetQueryPool(VkQueryPool queryPool, uint firstQuery, uint queryCount)
        {
            vkCmdResetQueryPool(commandBuffer, queryPool, firstQuery, queryCount);
        }

        public void WriteTimestamp(VkPipelineStageFlags pipelineStage, VkQueryPool queryPool, uint query)
        {
            vkCmdWriteTimestamp(commandBuffer, (VkPipelineStageFlags)pipelineStage, queryPool, (uint)query);
        }

        public void NextSubpass(VkSubpassContents contents)
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
            uint memoryBarrierCount, VkMemoryBarrier* pMemoryBarriers, uint bufferMemoryBarrierCount, VkBufferMemoryBarrier* pBufferMemoryBarriers,
            uint imageMemoryBarrierCount, VkImageMemoryBarrier* pImageMemoryBarriers)
        {
            vkCmdPipelineBarrier(commandBuffer, (VkPipelineStageFlags)srcStageMask, (VkPipelineStageFlags)dstStageMask, (VkDependencyFlags)dependencyFlags, memoryBarrierCount, (VkMemoryBarrier*)pMemoryBarriers,
                bufferMemoryBarrierCount, (VkBufferMemoryBarrier*)pBufferMemoryBarriers,
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
                (uint)barriers.Length, (VkImageMemoryBarrier*)Unsafe.AsPointer(ref barriers[0]));
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


}
