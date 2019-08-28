using System;
using System.Runtime.InteropServices;
using Vulkan;
using static Vulkan.VulkanNative;

namespace SharpGame
{
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

}
