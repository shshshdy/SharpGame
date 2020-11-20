using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    using System.Runtime.CompilerServices;
    using global::System.Runtime.InteropServices;
    using Vulkan;

    public struct Semaphore : IDisposable
    {
        internal VkSemaphore native;

        public static readonly Semaphore Null;

        public Semaphore(uint flags)
        {
            native = Device.CreateSemaphore(flags);
        }

        public void Dispose()
        {
            Device.Destroy(native);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(in Semaphore sem)
        {
            return sem.native != 0;
        }

    }

    [Flags]
    public enum FenceCreateFlags
    {
        None = 0,
        Signaled = 1 << 0
    }

    public struct Fence : IDisposable
    {
        internal VkFence handle;
        public Fence(FenceCreateFlags flags)
        {
            VkFenceCreateInfo createInfo = VkFenceCreateInfo.New();
            createInfo.flags = (VkFenceCreateFlags)flags;
            handle = Device.CreateFence(ref createInfo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(in Fence sem)
        {
            return sem.handle != 0;
        }

        public VkResult GetStatus()
        {
            VkResult result = Device.GetFenceStatus(handle);
            return result;
        }

        public void Reset()
        {
            Device.ResetFences(1, ref handle);
        }

        public void Wait(ulong timeout = ~0ul)
        {
            Device.WaitForFences(1, ref handle, false, timeout);
        }

        internal unsafe static void Reset(Fence[] fences)
        {
            int count = fences?.Length ?? 0;
            Span<VkFence> handles = stackalloc VkFence[count];
            for (int i = 0; i < count; i++)
                handles[i] = fences[i].handle;

            Device.ResetFences((uint)count, ref handles[0]);
        }

        internal unsafe static void Wait(Device parent, Fence[] fences, bool waitAll, ulong timeout)
        {
            int count = fences?.Length ?? 0;
            Span<VkFence> handles = stackalloc VkFence[count];
            for (int i = 0; i < count; i++)
                handles[i] = fences[i].handle;

            Device.WaitForFences((uint)count, ref handles[0], waitAll, timeout);

        }

        public void Dispose()
        {
            Device.Destroy(handle);
        }
    }

    [Flags]
    public enum EventCreateFlags
    {
        None = 0
    }

    public struct Event : IDisposable
    {
        internal VkEvent handle;
        internal Event(EventCreateFlags flags)
        {
            var createInfo = VkEventCreateInfo.New();
            createInfo.flags = (uint)flags;
            handle = Device.CreateEvent(ref createInfo);            
        }

        public VkResult GetStatus()
        {
            VkResult result = Device.GetEventStatus(handle);
            if (result != VkResult.EventSet && result != VkResult.EventReset)
                VulkanUtil.CheckResult(result);
            return result;
        }

        public void Set()
        {
            Device.SetEvent(handle);
        }

        public void Reset()
        {
            Device.ResetEvent(handle);
        }

        public void Dispose()
        {
            Device.Destroy(handle);
        }
    }

}
