﻿using System;
using System.Runtime.InteropServices;
using Vulkan;
using static Vulkan.VulkanNative;

namespace SharpGame
{
    /// <summary>
    /// Opaque handle to a fence object.
    /// <para>
    /// Fences are a synchronization primitive that can be used to insert a dependency from a queue
    /// to the host. Fences have two states - signaled and unsignaled. A fence can be signaled as
    /// part of the execution of a queue submission command. Fences can be unsignaled on the host
    /// with <see cref="Device.ResetFences"/>. Fences can be waited on by the host with the <see
    /// cref="Device.WaitFences"/> command, and the current state can be queried with <see cref="GetStatus"/>.
    /// </para>
    /// </summary>
    public unsafe class Fence : DisposeBase
    {
        internal VkFence native;
        internal Fence(ref FenceCreateInfo createInfo)
        {
            VulkanUtil.CheckResult(vkCreateFence(Device.LogicalDevice, ref createInfo.native, null, out native));           
        }

        /// <summary>
        /// Return the status of a fence. Upon success, returns the status of the fence object, with
        /// the following return codes:
        /// <para>* <see cref="Result.Success"/> - The fence is signaled</para>
        /// <para>* <see cref="Result.NotReady"/> - The fence is unsignaled</para>
        /// </summary>
        /// <returns><see cref="Result.Success"/> if the fence is signaled; otherwise <see cref="Result.NotReady"/>.</returns>
        /// <exception cref="VulkanException">Vulkan returns an error code.</exception>
        public VkResult GetStatus()
        {
            VkResult result = vkGetFenceStatus(Device.LogicalDevice, native);
            return result;
        }

        /// <summary>
        /// Resets the fence object.
        /// <para>Defines a fence unsignal operation, which resets the fence to the unsignaled state.</para>
        /// <para>
        /// If fence is already in the unsignaled state, then the command has no effect on that fence.
        /// </para>
        /// </summary>
        /// <exception cref="VulkanException">Vulkan returns an error code.</exception>
        public void Reset()
        {
            VulkanUtil.CheckResult(vkResetFences(Device.LogicalDevice, 1, ref native));
        }

        /// <summary>
        /// Wait for the fence to become signaled.
        /// <para>
        /// If the condition is satisfied when the command is called, then the command returns
        /// immediately. If the condition is not satisfied at the time the command is called, then
        /// the command will block and wait up to timeout nanoseconds for the condition to become satisfied.
        /// </para>
        /// </summary>
        /// <param name="timeout">
        /// The timeout period in units of nanoseconds. Timeout is adjusted to the closest value
        /// allowed by the implementation-dependent timeout accuracy, which may be substantially
        /// longer than one nanosecond, and may be longer than the requested period.
        /// <para>
        /// If timeout is zero, then the command does not wait, but simply returns the current state
        /// of the fences. The result <see cref="Result.Timeout"/> will be thrown in this case if the
        /// condition is not satisfied, even though no actual wait was performed.
        /// </para>
        /// <para>
        /// If the specified timeout period expires before the condition is satisfied, the command
        /// throws with <see cref="Result.Timeout"/>. If the condition is satisfied before timeout
        /// nanoseconds has expired, the command returns successfully.
        /// </para>
        /// </param>
        /// <exception cref="VulkanException">Vulkan returns an error code.</exception>
        public void Wait(ulong timeout = ~0ul)
        {
            VulkanUtil.CheckResult(vkWaitForFences(Device.LogicalDevice, 1, ref native, false, timeout));
        }

        /// <summary>
        /// Destroy a fence object.
        /// </summary>
        protected override void Destroy()
        {
            if (!IsDisposed)
                vkDestroyFence(Device.LogicalDevice, native, null);

            base.Destroy();
        }

        internal static void Reset(Fence[] fences)
        {
            int count = fences?.Length ?? 0;
            VkFence* handles = stackalloc VkFence[count];
            for (int i = 0; i < count; i++)
                handles[i] = fences[i].native;

            VulkanUtil.CheckResult(vkResetFences(Device.LogicalDevice, (uint)count, handles));
        }

        internal static void Wait(Device parent, Fence[] fences, bool waitAll, ulong timeout)
        {
            int count = fences?.Length ?? 0;
            VkFence* handles = stackalloc VkFence[count];
            for (int i = 0; i < count; i++)
                handles[i] = fences[i].native;

            VulkanUtil.CheckResult(vkWaitForFences(Device.LogicalDevice, (uint)count, handles, waitAll, timeout));
            
        }

    }

    /// <summary>
    /// Structure specifying parameters of a newly created fence.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FenceCreateInfo
    {
        internal VkFenceCreateInfo native;
        /// <summary>
        /// Initializes a new instance of the <see cref="FenceCreateInfo"/> structure.
        /// </summary>
        /// <param name="flags">Specifies the initial state and behavior of the fence.</param>
        public FenceCreateInfo(FenceCreateFlags flags = 0)
        {
            native = VkFenceCreateInfo.New();
            native.flags = (VkFenceCreateFlags)flags;
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
