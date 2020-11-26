using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGame
{
    using static Vulkan;
    public unsafe partial class Queue
    {
        internal VkQueue handle;
        public Queue(uint queueFamilyIndex, uint queueIndex)
        {
            handle = Device.GetDeviceQueue(queueFamilyIndex, queueIndex);
        }

        public void Submit(SubmitInfo[] submits, VkFence fence = default)
        {
            int count = submits?.Length ?? 0;
            VulkanUtil.CheckResult(vkQueueSubmit(handle, (uint)count, submits != null ? Utilities.AsPtr(ref submits[0].native) : null, fence));           
        }

        public void Submit(SubmitInfo submit, VkFence fence = default)
        {
            VulkanUtil.CheckResult(vkQueueSubmit(handle, 1, &submit.native, fence));
        }

        public void Submit(VkSemaphore waitSemaphore, VkPipelineStageFlags waitDstStageMask,
            CommandBuffer commandBuffer, VkSemaphore signalSemaphore, VkFence fence = default)
        {
            VkCommandBuffer commandBufferHandle = commandBuffer?.commandBuffer ?? VkCommandBuffer.Null;

            var nativeSubmit = new VkSubmitInfo
            {
                sType = VkStructureType.SubmitInfo
            };

            if (waitSemaphore != default)
            {
                nativeSubmit.waitSemaphoreCount = 1;
                nativeSubmit.pWaitSemaphores = &waitSemaphore;
                nativeSubmit.pWaitDstStageMask = (VkPipelineStageFlags*)&waitDstStageMask;
            }

            if (commandBuffer != null)
            {
                nativeSubmit.commandBufferCount = 1;
                nativeSubmit.pCommandBuffers = &commandBufferHandle;
            }

            if (signalSemaphore != default)
            {
                nativeSubmit.signalSemaphoreCount = 1;
                nativeSubmit.pSignalSemaphores = &signalSemaphore;
            }
            
            VulkanUtil.CheckResult(vkQueueSubmit(handle, 1, &nativeSubmit, fence));

            if (commandBuffer)
            {
                commandBuffer.NeedSubmit = false;
            }
        }

        public void WaitIdle()
        {
            VulkanUtil.CheckResult(vkQueueWaitIdle(handle));
        }

        public void BindSparse(BindSparseInfo bindInfo, VkFence fence = default)
        {
            VulkanUtil.CheckResult(vkQueueBindSparse(handle, 1, &bindInfo.native, fence));           
        }

        public void BindSparse(BindSparseInfo[] bindInfo, VkFence fence = default)
        {
            int count = bindInfo?.Length ?? 0;

            VulkanUtil.CheckResult(vkQueueBindSparse(handle, (uint)count, bindInfo != null ? Utilities.AsPtr(ref bindInfo[0].native) : null, fence));

        }

    }

    public struct SubmitInfo
    {
        internal VkSubmitInfo native;
        
        public SubmitInfo(VkSemaphore[] waitSemaphores = null, VkPipelineStageFlags[] waitDstStageMask = null,
            VkCommandBuffer[] commandBuffers = null, VkSemaphore[] signalSemaphores = null)
        {
            unsafe
            {
                native = new VkSubmitInfo
                {
                    sType = VkStructureType.SubmitInfo,
                    waitSemaphoreCount = (uint)(waitSemaphores?.Length ?? 0),
                    pWaitSemaphores = (VkSemaphore*)Utilities.AsPtr(ref waitSemaphores[0]),
                    pWaitDstStageMask = (VkPipelineStageFlags*)Utilities.AsPtr(ref waitDstStageMask[0]),
                    commandBufferCount = (uint)(commandBuffers?.Length ?? 0),
                    pCommandBuffers = Utilities.AsPtr(ref commandBuffers[0]),
                    signalSemaphoreCount = (uint)(signalSemaphores?.Length ?? 0),
                    pSignalSemaphores = (VkSemaphore*)Unsafe.AsPointer(ref waitSemaphores[0])
                };
            }
        }

    }


    public struct BindSparseInfo
    {
        internal VkBindSparseInfo native;

        public BindSparseInfo(VkSemaphore[] waitSemaphores, SparseBufferMemoryBindInfo[] bufferBinds,
            SparseImageOpaqueMemoryBindInfo[] imageOpaqueBinds, SparseImageMemoryBindInfo[] imageBinds,
            VkSemaphore[] signalSemaphores)
        {
            unsafe
            {
                native = new VkBindSparseInfo
                {
                    sType = VkStructureType.BindSparseInfo,
                    waitSemaphoreCount = (uint)(waitSemaphores?.Length ?? 0),
                    pWaitSemaphores = waitSemaphores != null ? (VkSemaphore*)Utilities.AsPtr(ref waitSemaphores[0]) : null,
                    bufferBindCount = (uint)(bufferBinds?.Length ?? 0),
                    pBufferBinds = bufferBinds != null ? (VkSparseBufferMemoryBindInfo*)Utilities.AsPtr(ref bufferBinds[0]) : null,
                    imageOpaqueBindCount = (uint)(imageOpaqueBinds?.Length ?? 0),
                    pImageOpaqueBinds = imageOpaqueBinds != null ? (VkSparseImageOpaqueMemoryBindInfo*)Utilities.AsPtr(ref imageOpaqueBinds[0]) : null,
                    imageBindCount = (uint)(imageBinds?.Length ?? 0),
                    pImageBinds = imageBinds != null ? (VkSparseImageMemoryBindInfo*)Utilities.AsPtr(ref imageBinds[0]) : null,
                    signalSemaphoreCount = (uint)(signalSemaphores?.Length ?? 0),
                    pSignalSemaphores = signalSemaphores != null ? (VkSemaphore*)Utilities.AsPtr(ref signalSemaphores[0]) : null
                };
            }
        }
       
    }

    public unsafe struct SparseBufferMemoryBindInfo
    {
        VkSparseBufferMemoryBindInfo native;
        public SparseBufferMemoryBindInfo(Buffer buffer, params VkSparseMemoryBind[] binds)
        {
            native.buffer = buffer.handle;
            native.bindCount = (uint)(binds?.Length ?? 0);
            native.pBinds = Utilities.AsPtr(ref binds[0]);
        }
    }

    public unsafe struct SparseImageOpaqueMemoryBindInfo
    {
        VkSparseImageOpaqueMemoryBindInfo native;
        public SparseImageOpaqueMemoryBindInfo(Image image, params VkSparseMemoryBind[] binds)
        {
            native.image = image.handle;
            native.bindCount = (uint)(binds?.Length ?? 0);
            native.pBinds = (VkSparseMemoryBind*)Unsafe.AsPointer(ref binds[0]);
        }

        public SparseImageOpaqueMemoryBindInfo(Image image, Vector<VkSparseMemoryBind> binds)
        {
            native.image = image.handle;
            native.bindCount = binds.Count;
            native.pBinds = binds.DataPtr;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SparseImageMemoryBindInfo
    {
        VkSparseImageMemoryBindInfo native;
        public SparseImageMemoryBindInfo(Image image, params VkSparseImageMemoryBind[] binds)
        {
            native.image = image.handle;
            native.bindCount = (uint)(binds?.Length ?? 0);
            native.pBinds = (VkSparseImageMemoryBind*)Unsafe.AsPointer(ref binds[0]);
        }

        public SparseImageMemoryBindInfo(Image image, Vector<VkSparseImageMemoryBind> binds)
        {
            native.image = image.handle;
            native.bindCount = binds.Count;
            native.pBinds = binds.DataPtr;
        }
    }

}
