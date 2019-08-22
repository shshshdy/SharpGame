using System;
using System.Runtime.InteropServices;
using Vulkan;
using static Vulkan.VulkanNative;

namespace SharpGame
{
    /// <summary>
    /// Opaque handle to a semaphore object.
    /// <para>
    /// Semaphores are a synchronization primitive that can be used to insert a dependency between
    /// batches submitted to queues. Semaphores have two states - signaled and unsignaled. The state
    /// of a semaphore can be signaled after execution of a batch of commands is completed. A batch
    /// can wait for a semaphore to become signaled before it begins execution, and the semaphore is
    /// also unsignaled before the batch begins execution.
    /// </para>
    /// </summary>
    public unsafe class Semaphore : DisposeBase
    {
        internal VkSemaphore native;
        internal Semaphore()
        {
            var createInfo = new SemaphoreCreateInfo();

            VulkanUtil.CheckResult(vkCreateSemaphore(Device.LogicalDevice, ref createInfo.native, null, out native));
        }

        /// <summary>
        /// Destroy a semaphore object.
        /// </summary>
        protected override void Destroy()
        {
            if (!IsDisposed)
                vkDestroySemaphore(Device.LogicalDevice, native, null);

            base.Destroy();
        }

    }

    /// <summary>
    /// Structure specifying parameters of a newly created semaphore.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SemaphoreCreateInfo
    {
        internal VkSemaphoreCreateInfo native;

        public SemaphoreCreateInfo(uint flags)
        {
            native = VkSemaphoreCreateInfo.New();
            native.flags = flags;
        }
    }

    // Is reserved for future use.
    [Flags]
    internal enum SemaphoreCreateFlags
    {
        None = 0
    }
}
