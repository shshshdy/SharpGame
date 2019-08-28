using System;
using System.Runtime.InteropServices;
using Vulkan;
using static Vulkan.VulkanNative;

namespace SharpGame
{
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
}
