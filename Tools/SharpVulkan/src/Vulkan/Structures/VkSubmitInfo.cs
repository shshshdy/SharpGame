using System.Runtime.CompilerServices;

namespace SharpGame
{
    public partial struct VkSubmitInfo
    {        
        public VkSubmitInfo(VkSemaphore[] waitSemaphores = null, VkPipelineStageFlags[] waitDstStageMask = null,
            VkCommandBuffer[] commandBuffers = null, VkSemaphore[] signalSemaphores = null)
        {
            unsafe
            {
                sType = VkStructureType.SubmitInfo;
                pNext = null;
                waitSemaphoreCount = (uint)(waitSemaphores?.Length ?? 0);
                pWaitSemaphores = (VkSemaphore*)Unsafe.AsPointer(ref waitSemaphores[0]);
                pWaitDstStageMask = (VkPipelineStageFlags*)Unsafe.AsPointer(ref waitDstStageMask[0]);
                commandBufferCount = (uint)(commandBuffers?.Length ?? 0);
                pCommandBuffers = (VkCommandBuffer*)Unsafe.AsPointer(ref commandBuffers[0]);
                signalSemaphoreCount = (uint)(signalSemaphores?.Length ?? 0);
                pSignalSemaphores = (VkSemaphore*)Unsafe.AsPointer(ref waitSemaphores[0]);

            }
        }

    }

}
