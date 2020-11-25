using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGame
{
    using static Vulkan;
    public unsafe class Queue
    {
        internal VkQueue native;
        public uint FamilyIndex { get; }
        public uint Index { get; }

        internal Queue(VkQueue handle, uint familyIndex, uint index)
        {
            native = handle;
            FamilyIndex = familyIndex;
            Index = index;
        }

        public static Queue GetDeviceQueue(uint queueFamilyIndex, uint queueIndex)
        {
            VkQueue pQueue = Device.GetDeviceQueue(queueFamilyIndex, queueIndex);
            return new Queue(pQueue, queueFamilyIndex, queueIndex);
        }

        public void Submit(SubmitInfo[] submits, Fence fence = default)
        {
            int count = submits?.Length ?? 0;
            VulkanUtil.CheckResult(vkQueueSubmit(native, (uint)count, submits != null ? Utilities.AsPtr(ref submits[0].native) : null, fence.handle));           
        }

        public void Submit(SubmitInfo submit, Fence fence = default)
        {
            VulkanUtil.CheckResult(vkQueueSubmit(native, 1, &submit.native, fence.handle));
        }

        public void Submit(Semaphore waitSemaphore, PipelineStageFlags waitDstStageMask,
            CommandBuffer commandBuffer, Semaphore signalSemaphore, Fence fence = default)
        {
            VkCommandBuffer commandBufferHandle = commandBuffer?.commandBuffer ?? VkCommandBuffer.Null;

            var nativeSubmit = new VkSubmitInfo
            {
                sType = VkStructureType.SubmitInfo
            };

            if (waitSemaphore != default)
            {
                nativeSubmit.waitSemaphoreCount = 1;
                nativeSubmit.pWaitSemaphores = &waitSemaphore.native;
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
                nativeSubmit.pSignalSemaphores = &signalSemaphore.native;
            }
            
            VulkanUtil.CheckResult(vkQueueSubmit(native, 1, &nativeSubmit, fence.handle));

            if (commandBuffer)
            {
                commandBuffer.NeedSubmit = false;
            }
        }

        public void WaitIdle()
        {
            VulkanUtil.CheckResult(vkQueueWaitIdle(native));
        }

        public void BindSparse(BindSparseInfo bindInfo, Fence fence = default)
        {
            VulkanUtil.CheckResult(vkQueueBindSparse(native, 1, &bindInfo.native, fence.handle));           
        }

        public void BindSparse(BindSparseInfo[] bindInfo, Fence fence = default)
        {
            int count = bindInfo?.Length ?? 0;

            VulkanUtil.CheckResult(vkQueueBindSparse(native, (uint)count, bindInfo != null ? Utilities.AsPtr(ref bindInfo[0].native) : null, fence.handle));

        }

    }

    public struct SubmitInfo
    {
        internal VkSubmitInfo native;
        
        public SubmitInfo(Semaphore[] waitSemaphores = null, PipelineStageFlags[] waitDstStageMask = null,
            VkCommandBuffer[] commandBuffers = null, Semaphore[] signalSemaphores = null)
        {
            native = new VkSubmitInfo
            {
                sType = VkStructureType.SubmitInfo
            };
            unsafe
            {
                native.waitSemaphoreCount = (uint)(waitSemaphores?.Length ?? 0);
                native.pWaitSemaphores = (VkSemaphore*)Utilities.AsPtr(ref waitSemaphores[0]);
                native.pWaitDstStageMask = (VkPipelineStageFlags*)Utilities.AsPtr(ref waitDstStageMask[0]);
                native.commandBufferCount = (uint)(commandBuffers?.Length ?? 0);
                native.pCommandBuffers = Utilities.AsPtr(ref commandBuffers[0]);
                native.signalSemaphoreCount = (uint)(signalSemaphores?.Length ?? 0);
                native.pSignalSemaphores = (VkSemaphore*)Unsafe.AsPointer(ref waitSemaphores[0]);
            }
        }

    }


    public struct BindSparseInfo
    {
        internal VkBindSparseInfo native;

        public BindSparseInfo(Semaphore[] waitSemaphores, SparseBufferMemoryBindInfo[] bufferBinds,
            SparseImageOpaqueMemoryBindInfo[] imageOpaqueBinds, SparseImageMemoryBindInfo[] imageBinds,
            Semaphore[] signalSemaphores)
        {
            unsafe
            {
                native = new VkBindSparseInfo
                {
                    sType = VkStructureType.BindSparseInfo
                };
                native.waitSemaphoreCount = (uint)(waitSemaphores?.Length ?? 0);
                native.pWaitSemaphores = waitSemaphores != null ?(VkSemaphore*)Utilities.AsPtr(ref waitSemaphores[0]): null;
                native.bufferBindCount = (uint)(bufferBinds?.Length ?? 0);
                native.pBufferBinds = bufferBinds != null ? (VkSparseBufferMemoryBindInfo*)Utilities.AsPtr(ref bufferBinds[0]) : null;
                native.imageOpaqueBindCount = (uint)(imageOpaqueBinds?.Length ?? 0);
                native.pImageOpaqueBinds = imageOpaqueBinds != null ? (VkSparseImageOpaqueMemoryBindInfo*)Utilities.AsPtr(ref imageOpaqueBinds[0]) : null;
                native.imageBindCount = (uint)(imageBinds?.Length ?? 0);
                native.pImageBinds = imageBinds != null ? (VkSparseImageMemoryBindInfo*)Utilities.AsPtr(ref imageBinds[0]) : null;
                native.signalSemaphoreCount = (uint)(signalSemaphores?.Length ?? 0);
                native.pSignalSemaphores = signalSemaphores != null ? (VkSemaphore*)Utilities.AsPtr(ref signalSemaphores[0]) : null;
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
