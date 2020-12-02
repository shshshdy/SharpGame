using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpGame
{
    using static Vulkan;
    public unsafe partial struct VkQueue
    {
        public VkQueue(uint queueFamilyIndex, uint queueIndex)
        {
            Vulkan.vkGetDeviceQueue(Vulkan.device, queueFamilyIndex, queueIndex, out this);
        }

        public void Submit(VkSubmitInfo[] submits, VkFence fence = default)
        {
            int count = submits?.Length ?? 0;
            VulkanUtil.CheckResult(vkQueueSubmit(this, (uint)count, submits != null ? (VkSubmitInfo*)Unsafe.AsPointer(ref submits[0]) : null, fence));           
        }

        public void Submit(VkSubmitInfo submit, VkFence fence = default)
        {
            VulkanUtil.CheckResult(vkQueueSubmit(this, 1, &submit, fence));
        }

        public void Submit(VkSemaphore waitSemaphore, VkPipelineStageFlags waitDstStageMask,
            VkCommandBuffer commandBuffer, VkSemaphore signalSemaphore, VkFence fence = default)
        {
            VkCommandBuffer commandBufferHandle = commandBuffer;

            var nativeSubmit = new VkSubmitInfo
            {
                sType = VkStructureType.SubmitInfo
            };

            if (waitSemaphore != default)
            {
                nativeSubmit.waitSemaphoreCount = 1;
                nativeSubmit.pWaitSemaphores = &waitSemaphore;
                nativeSubmit.pWaitDstStageMask = &waitDstStageMask;
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
            
            VulkanUtil.CheckResult(vkQueueSubmit(this, 1, &nativeSubmit, fence));

        }

        public void WaitIdle()
        {
            VulkanUtil.CheckResult(vkQueueWaitIdle(this));
        }

        public void BindSparse(VkBindSparseInfo bindInfo, VkFence fence = default)
        {
            VulkanUtil.CheckResult(vkQueueBindSparse(this, 1, &bindInfo, fence));           
        }

        public void BindSparse(VkBindSparseInfo[] bindInfo, VkFence fence = default)
        {
            int count = bindInfo?.Length ?? 0;

            VulkanUtil.CheckResult(vkQueueBindSparse(this, (uint)count, bindInfo != null ? (VkBindSparseInfo*)Unsafe.AsPointer(ref bindInfo[0]) : null, fence));

        }

    }

}
