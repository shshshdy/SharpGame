using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vulkan;
using static Vulkan.VulkanNative;

namespace SharpGame
{
    /// <summary>
    /// Opaque handle to a queue object.
    /// </summary>
    public unsafe class Queue
    {
        internal VkQueue native;
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

        /// <summary>
        /// Gets the queue family index.
        /// </summary>
        public uint FamilyIndex { get; }

        /// <summary>
        /// Gets the queue index.
        /// </summary>
        public uint Index { get; }

        public void Submit(SubmitInfo[] submits, Fence fence = null)
        {
            int count = submits?.Length ?? 0;
            VulkanUtil.CheckResult(vkQueueSubmit(native, (uint)count, submits != null ? (VkSubmitInfo*)Unsafe.AsPointer(ref submits[0].native) : null, fence?.native ?? VkFence.Null));           
        }

        public void Submit(SubmitInfo submit, Fence fence = null)
        {
            VulkanUtil.CheckResult(vkQueueSubmit(native, 1, &submit.native, fence?.native ?? VkFence.Null));
        }

        public void Submit(Semaphore waitSemaphore, PipelineStageFlags waitDstStageMask,
            CommandBuffer commandBuffer, Semaphore signalSemaphore, Fence fence = null)
        {
            VkCommandBuffer commandBufferHandle = commandBuffer?.commandBuffer ?? VkCommandBuffer.Null;

            var nativeSubmit = VkSubmitInfo.New();

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
            
            VulkanUtil.CheckResult(vkQueueSubmit(native, 1, &nativeSubmit, fence != null ? fence.native : VkFence.Null));

            if (commandBuffer)
            {
                commandBuffer.NeedSubmit = false;
            }
        }

        public void WaitIdle()
        {
            VulkanUtil.CheckResult(vkQueueWaitIdle(native));
        }

        /// <summary>
        /// Bind device memory to a sparse resource object.
        /// </summary>
        /// <param name="bindInfo">Specifying a sparse binding submission batch.</param>
        /// <param name="fence">
        /// An optional handle to a fence to be signaled. If fence is not <c>null</c>, it defines a
        /// fence signal operation.
        /// </param>
        /// <exception cref="VulkanException">Vulkan returns an error code.</exception>
        public void BindSparse(BindSparseInfo bindInfo, Fence fence = null)
        {/*
            bindInfo.ToNative(out BindSparseInfo.Native nativeBindInfo);
            Result result = vkQueueBindSparse(this, 1, &nativeBindInfo, fence);
            nativeBindInfo.Free();
            VulkanException.ThrowForInvalidResult(result);*/
        }

        /// <summary>
        /// Bind device memory to a sparse resource object.
        /// </summary>
        /// <param name="bindInfo">
        /// An array of <see cref="BindSparseInfo"/> structures, each specifying a sparse binding
        /// submission batch.
        /// </param>
        /// <param name="fence">
        /// An optional handle to a fence to be signaled. If fence is not <c>null</c>, it defines a
        /// fence signal operation.
        /// </param>
        /// <exception cref="VulkanException">Vulkan returns an error code.</exception>
        public void BindSparse(BindSparseInfo[] bindInfo, Fence fence = null)
        {/*
            int count = bindInfo?.Length ?? 0;

            var nativeBindInfo = stackalloc BindSparseInfo.Native[count];
            for (int i = 0; i < count; i++)
                bindInfo[i].ToNative(out nativeBindInfo[i]);

            Result result = vkQueueBindSparse(this, count, nativeBindInfo, fence);

            for (int i = 0; i < count; i++)
                nativeBindInfo[i].Free();

            VulkanException.ThrowForInvalidResult(result);*/
        }

    }

    /// <summary>
    /// Structure specifying a queue submit operation.
    /// </summary>
    public struct SubmitInfo
    {
        internal VkSubmitInfo native;        
        public SubmitInfo(Semaphore[] waitSemaphores = null, PipelineStageFlags[] waitDstStageMask = null,
            CommandBuffer[] commandBuffers = null, Semaphore[] signalSemaphores = null)
        {
            native = VkSubmitInfo.New();
            unsafe
            {
                native.waitSemaphoreCount = (uint)(waitSemaphores?.Length ?? 0);
                native.pWaitSemaphores = (VkSemaphore*)Unsafe.AsPointer(ref waitSemaphores[0]);
                native.pWaitDstStageMask = (VkPipelineStageFlags*)Unsafe.AsPointer(ref waitDstStageMask[0]);
                native.commandBufferCount = (uint)(commandBuffers?.Length ?? 0);
                //native.pCommandBuffers = Interop.Struct.AllocToPointer(CommandBuffers);
                native.signalSemaphoreCount = (uint)(signalSemaphores?.Length ?? 0);
                native.pSignalSemaphores = (VkSemaphore*)Unsafe.AsPointer(ref waitSemaphores[0]);
            }
        }

    }

    /// <summary>
    /// Structure specifying a sparse binding operation.
    /// </summary>
    public unsafe struct BindSparseInfo
    {
        /// <summary>
        /// Semaphores upon which to wait on before the sparse binding operations for this batch
        /// begin execution. If semaphores to wait on are provided, they define a semaphore wait operation.
        /// </summary>
        public long[] WaitSemaphores;
        /// <summary>
        /// An array of <see cref="SparseBufferMemoryBindInfo"/> structures.
        /// </summary>
        public SparseBufferMemoryBindInfo[] BufferBinds;
        /// <summary>
        /// An array of <see cref="SparseImageOpaqueMemoryBindInfo"/> structures, indicating opaque
        /// sparse image bindings to perform.
        /// </summary>
        public SparseImageOpaqueMemoryBindInfo[] ImageOpaqueBinds;
        /// <summary>
        /// An array of <see cref="SparseImageMemoryBindInfo"/> structures, indicating sparse image
        /// bindings to perform.
        /// </summary>
        public SparseImageMemoryBindInfo[] ImageBinds;
        /// <summary>
        /// Semaphores which will be signaled when the sparse binding operations for this batch have
        /// completed execution. If semaphores to be signaled are provided, they define a semaphore
        /// signal operation.
        /// </summary>
        public long[] SignalSemaphores;
        /*
        /// <summary>
        /// Initializes a new instance of the <see cref="BindSparseInfo"/> structure.
        /// </summary>
        /// <param name="waitSemaphores">
        /// Semaphores upon which to wait on before the sparse binding operations for this batch
        /// begin execution. If semaphores to wait on are provided, they define a semaphore wait operation.
        /// </param>
        /// <param name="bufferBinds">An array of <see cref="SparseBufferMemoryBindInfo"/> structures.</param>
        /// <param name="imageOpaqueBinds">
        /// An array of <see cref="SparseImageOpaqueMemoryBindInfo"/> structures, indicating opaque
        /// sparse image bindings to perform.
        /// </param>
        /// <param name="imageBinds">
        /// An array of <see cref="SparseImageMemoryBindInfo"/> structures, indicating sparse image
        /// bindings to perform.
        /// </param>
        /// <param name="signalSemaphores">
        /// Semaphores which will be signaled when the sparse binding operations for this batch have
        /// completed execution. If semaphores to be signaled are provided, they define a semaphore
        /// signal operation.
        /// </param>
        public BindSparseInfo(Semaphore[] waitSemaphores, SparseBufferMemoryBindInfo[] bufferBinds,
            SparseImageOpaqueMemoryBindInfo[] imageOpaqueBinds, SparseImageMemoryBindInfo[] imageBinds,
            Semaphore[] signalSemaphores)
        {
            WaitSemaphores = waitSemaphores?.ToHandleArray();
            BufferBinds = bufferBinds;
            ImageOpaqueBinds = imageOpaqueBinds;
            ImageBinds = imageBinds;
            SignalSemaphores = signalSemaphores?.ToHandleArray();
        }
       
        [StructLayout(LayoutKind.Sequential)]
        internal struct Native
        {
            public StructureType Type;
            public IntPtr Next;
            public int WaitSemaphoreCount;
            public IntPtr WaitSemaphores;
            public int BufferBindCount;
            public SparseBufferMemoryBindInfo.Native* BufferBinds;
            public int ImageOpaqueBindCount;
            public SparseImageOpaqueMemoryBindInfo.Native* ImageOpaqueBinds;
            public int ImageBindCount;
            public SparseImageMemoryBindInfo.Native* ImageBinds;
            public int SignalSemaphoreCount;
            public IntPtr SignalSemaphores;

            public void Free()
            {
                Interop.Free(WaitSemaphores);
                for (int i = 0; i < BufferBindCount; i++)
                    BufferBinds[i].Free();
                Interop.Free(BufferBinds);
                for (int i = 0; i < ImageOpaqueBindCount; i++)
                    ImageOpaqueBinds[i].Free();
                Interop.Free(ImageOpaqueBinds);
                for (int i = 0; i < ImageBindCount; i++)
                    ImageBinds[i].Free();
                Interop.Free(ImageBinds);
                Interop.Free(SignalSemaphores);
            }
        }

        internal void ToNative(out VkBindSparseInfo native)
        {
            int bufferBindCount = BufferBinds?.Length ?? 0;
            int imageOpaqueBindCount = ImageOpaqueBinds?.Length ?? 0;
            int imageBindCount = ImageBinds?.Length ?? 0;

            var bufferBinds = (SparseBufferMemoryBindInfo.Native*)
                Interop.Alloc<SparseBufferMemoryBindInfo.Native>(bufferBindCount);
            for (int i = 0; i < bufferBindCount; i++)
                BufferBinds[i].ToNative(&bufferBinds[i]);
            var imageOpaqueBinds = (SparseImageOpaqueMemoryBindInfo.Native*)
                Interop.Alloc<SparseImageOpaqueMemoryBindInfo.Native>(bufferBindCount);
            for (int i = 0; i < imageOpaqueBindCount; i++)
                ImageOpaqueBinds[i].ToNative(&imageOpaqueBinds[i]);
            var imageBinds = (SparseImageMemoryBindInfo.Native*)
                Interop.Alloc<SparseImageMemoryBindInfo.Native>(bufferBindCount);
            for (int i = 0; i < imageBindCount; i++)
                ImageBinds[i].ToNative(&imageBinds[i]);

            native.Type = StructureType.BindSparseInfo;
            native.Next = IntPtr.Zero;
            native.WaitSemaphoreCount = WaitSemaphores?.Length ?? 0;
            native.WaitSemaphores = Interop.Struct.AllocToPointer(WaitSemaphores);
            native.BufferBindCount = bufferBindCount;
            native.BufferBinds = bufferBinds;
            native.ImageOpaqueBindCount = imageOpaqueBindCount;
            native.ImageOpaqueBinds = imageOpaqueBinds;
            native.ImageBindCount = imageBindCount;
            native.ImageBinds = imageBinds;
            native.SignalSemaphoreCount = SignalSemaphores?.Length ?? 0;
            native.SignalSemaphores = Interop.Struct.AllocToPointer(SignalSemaphores);
        }*/
    }

    public unsafe struct SparseBufferMemoryBindInfo
    {
        VkSparseBufferMemoryBindInfo native;
        public SparseBufferMemoryBindInfo(Buffer buffer, params VkSparseMemoryBind[] binds)
        {
            native.buffer = buffer.buffer;
            native.bindCount = (uint)(binds?.Length ?? 0);
            native.pBinds = (VkSparseMemoryBind*)Unsafe.AsPointer(ref binds[0]);
        }
    }

    [Flags]
    public enum SparseMemoryBindFlags
    {
        None = 0,
        Metadata = 1 << 0
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

    }

}
