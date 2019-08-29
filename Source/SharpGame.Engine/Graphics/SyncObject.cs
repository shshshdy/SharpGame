using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    using global::System.Runtime.InteropServices;
    using Vulkan;
    using static Vulkan.VulkanNative;

    public class Semaphore : DisposeBase
    {
        internal VkSemaphore native;
        internal Semaphore()
        {
            var createInfo = VkSemaphoreCreateInfo.New();

            VulkanUtil.CheckResult(vkCreateSemaphore(Device.LogicalDevice, ref createInfo, IntPtr.Zero, out native));
        }

        protected override void Destroy(bool disposing)
        {
            vkDestroySemaphore(Device.LogicalDevice, native, IntPtr.Zero);
        }

    }

    public class Fence : DisposeBase
    {
        internal VkFence native;
        internal Fence(FenceCreateFlags flags)
        {
            VkFenceCreateInfo createInfo = VkFenceCreateInfo.New();
            createInfo.flags = (VkFenceCreateFlags)flags;
            VulkanUtil.CheckResult(vkCreateFence(Device.LogicalDevice, ref createInfo, IntPtr.Zero, out native));
        }

        public VkResult GetStatus()
        {
            VkResult result = vkGetFenceStatus(Device.LogicalDevice, native);
            return result;
        }

        public void Reset()
        {
            VulkanUtil.CheckResult(vkResetFences(Device.LogicalDevice, 1, ref native));
        }

        public void Wait(ulong timeout = ~0ul)
        {
            VulkanUtil.CheckResult(vkWaitForFences(Device.LogicalDevice, 1, ref native, false, timeout));
        }

        protected override void Destroy(bool disposing)
        {
            vkDestroyFence(Device.LogicalDevice, native, IntPtr.Zero);

            base.Destroy(disposing);
        }

        internal unsafe static void Reset(Fence[] fences)
        {
            int count = fences?.Length ?? 0;
            VkFence* handles = stackalloc VkFence[count];
            for (int i = 0; i < count; i++)
                handles[i] = fences[i].native;

            VulkanUtil.CheckResult(vkResetFences(Device.LogicalDevice, (uint)count, handles));
        }

        internal unsafe static void Wait(Device parent, Fence[] fences, bool waitAll, ulong timeout)
        {
            int count = fences?.Length ?? 0;
            VkFence* handles = stackalloc VkFence[count];
            for (int i = 0; i < count; i++)
                handles[i] = fences[i].native;

            VulkanUtil.CheckResult(vkWaitForFences(Device.LogicalDevice, (uint)count, handles, waitAll, timeout));

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

           VulkanUtil.CheckResult(vkCreateEvent(Device.LogicalDevice, ref createInfo.native, null, out handle));
            
        }

        public VkResult GetStatus()
        {
            VkResult result = vkGetEventStatus(Device.LogicalDevice, handle);
            if (result != VkResult.EventSet && result != VkResult.EventReset)
                VulkanUtil.CheckResult(result);
            return result;
        }

        public void Set()
        {
            VulkanUtil.CheckResult(vkSetEvent(Device.LogicalDevice, handle));
        }

        public void Reset()
        {
            VulkanUtil.CheckResult(vkResetEvent(Device.LogicalDevice, handle));
        }

        protected override void Destroy(bool disposing)
        {
            vkDestroyEvent(Device.LogicalDevice, handle, null);

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
