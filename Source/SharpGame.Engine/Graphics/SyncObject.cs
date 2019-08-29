using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    using global::System.Runtime.CompilerServices;
    using global::System.Runtime.InteropServices;
    using Vulkan;
    using static Vulkan.VulkanNative;

    public class Semaphore : DisposeBase
    {
        internal VkSemaphore native;
        internal Semaphore()
        {
            native = Device.CreateSemaphore();
        }

        protected override void Destroy(bool disposing)
        {
            Device.Destroy(native);
        }

    }

    public class Fence : DisposeBase
    {
        internal VkFence native;
        internal Fence(FenceCreateFlags flags)
        {
            VkFenceCreateInfo createInfo = VkFenceCreateInfo.New();
            createInfo.flags = (VkFenceCreateFlags)flags;
            native = Device.CreateFence(ref createInfo);
        }

        public VkResult GetStatus()
        {
            VkResult result = Device.GetFenceStatus(native);
            return result;
        }

        public void Reset()
        {
            Device.ResetFences(1, ref native);
        }

        public void Wait(ulong timeout = ~0ul)
        {
            Device.WaitForFences(1, ref native, false, timeout);
        }

        protected override void Destroy(bool disposing)
        {
            Device.Destroy(native);

            base.Destroy(disposing);
        }

        internal unsafe static void Reset(Fence[] fences)
        {
            int count = fences?.Length ?? 0;
            Span<VkFence> handles = stackalloc VkFence[count];
            for (int i = 0; i < count; i++)
                handles[i] = fences[i].native;

            Device.ResetFences((uint)count, ref handles[0]);
        }

        internal unsafe static void Wait(Device parent, Fence[] fences, bool waitAll, ulong timeout)
        {
            int count = fences?.Length ?? 0;
            Span<VkFence> handles = stackalloc VkFence[count];
            for (int i = 0; i < count; i++)
                handles[i] = fences[i].native;

            Device.WaitForFences((uint)count, ref handles[0], waitAll, timeout);

        }

    }

    /// <summary>
    /// Bitmask specifying initial state and behavior of a fence.
    /// </summary>
    [Flags]
    public enum FenceCreateFlags
    {
        /// <summary>
        /// Specifies that the fence object is created in the unsignaled state.
        /// </summary>
        None = 0,
        /// <summary>
        /// Specifies that the fence object is created in the signaled state. Otherwise, it is
        /// created in the unsignaled state.
        /// </summary>
        Signaled = 1 << 0
    }

    public unsafe class Event : DisposeBase
    {
        VkEvent handle;
        internal Event()
        {
            var createInfo = new EventCreateInfo();
            handle = Device.CreateEvent(ref createInfo.native);            
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

        protected override void Destroy(bool disposing)
        {
            Device.Destroy(handle);

            base.Destroy(disposing);
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct EventCreateInfo
    {
        internal VkEventCreateInfo native;
        public EventCreateInfo(EventCreateFlags flags)
        {
            native = VkEventCreateInfo.New();
            native.flags = (uint)flags;
        }
        
    }

    // Is reserved for future use.
    [Flags]
    internal enum EventCreateFlags
    {
        None = 0
    }
}
