using System.Runtime.CompilerServices;

namespace SharpGame
{
    public unsafe partial struct VkBindSparseInfo
    {
        public VkBindSparseInfo(VkSemaphore[] waitSemaphores, VkSparseBufferMemoryBindInfo[] bufferBinds,
            VkSparseImageOpaqueMemoryBindInfo[] imageOpaqueBinds, VkSparseImageMemoryBindInfo[] imageBinds,
            VkSemaphore[] signalSemaphores)
        {
            sType = VkStructureType.BindSparseInfo;
            pNext = null;
            waitSemaphoreCount = (uint)(waitSemaphores?.Length ?? 0);
            pWaitSemaphores = waitSemaphores != null ? (VkSemaphore*)Unsafe.AsPointer(ref waitSemaphores[0]) : null;
            bufferBindCount = (uint)(bufferBinds?.Length ?? 0);
            pBufferBinds = bufferBinds != null ? (VkSparseBufferMemoryBindInfo*)Unsafe.AsPointer(ref bufferBinds[0]) : null;
            imageOpaqueBindCount = (uint)(imageOpaqueBinds?.Length ?? 0);
            pImageOpaqueBinds = imageOpaqueBinds != null ? (VkSparseImageOpaqueMemoryBindInfo*)Unsafe.AsPointer(ref imageOpaqueBinds[0]) : null;
            imageBindCount = (uint)(imageBinds?.Length ?? 0);
            pImageBinds = imageBinds != null ? (VkSparseImageMemoryBindInfo*)Unsafe.AsPointer(ref imageBinds[0]) : null;
            signalSemaphoreCount = (uint)(signalSemaphores?.Length ?? 0);
            pSignalSemaphores = signalSemaphores != null ? (VkSemaphore*)Unsafe.AsPointer(ref signalSemaphores[0]) : null;

        }

    }

}
