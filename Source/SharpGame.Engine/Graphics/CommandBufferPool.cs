﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Vulkan;

namespace SharpGame
{
    public enum CommandPoolCreateFlags
    {
        None = 0,
        Transient = 1,
        ResetCommandBuffer = 2
    }

    public class CommandBufferPool : DisposeBase
    {
        public CommandBuffer[] CommandBuffers { get; private set; }

        private VkCommandPool cmdPool;
        private NativeList<VkCommandBuffer> cmdBuffers = new NativeList<VkCommandBuffer>();

        public int currentIndex;

        public IntPtr GetAddress(uint idx) => cmdBuffers.GetAddress(idx);

        public CommandBufferPool(uint queue, CommandPoolCreateFlags commandPoolCreateFlags)
        {
            cmdPool = Device.CreateCommandPool(queue, (VkCommandPoolCreateFlags)commandPoolCreateFlags);
        }

        public CommandBuffer this[int index]
        {
            get { return CommandBuffers[index]; }
        }

        protected override void Destroy(bool disposing)
        {
            Free();

            base.Destroy(disposing);
        }

        public void Allocate(CommandBufferLevel commandBufferLevel, uint count)
        {
            if(cmdBuffers.Count > 0)
            {
                Free();
            }

            cmdBuffers.Resize(count);
            cmdBuffers.Count = count;

            var cmdBufAllocateInfo = VkCommandBufferAllocateInfo.New();
            cmdBufAllocateInfo.commandPool = cmdPool;
            cmdBufAllocateInfo.level = (VkCommandBufferLevel)commandBufferLevel;
            cmdBufAllocateInfo.commandBufferCount = count;

            unsafe
            {
                VulkanUtil.CheckResult(VulkanNative.vkAllocateCommandBuffers(Graphics.device, ref cmdBufAllocateInfo, (VkCommandBuffer*)cmdBuffers.Data));
            }

            CommandBuffers = new CommandBuffer[count];
            for (int i = 0; i < count; i++)
            {
                CommandBuffers[i] = new CommandBuffer(cmdBuffers[i]);
            }
        }

        public void Free()
        {
            VulkanNative.vkFreeCommandBuffers(Graphics.device, cmdPool, cmdBuffers.Count, cmdBuffers.Data);
            cmdBuffers.Count = 0;
            CommandBuffers = null;
        }

        public CommandBuffer Get()
        {
            int idx = currentIndex;
                Interlocked.Increment(ref currentIndex);
            return CommandBuffers[idx % CommandBuffers.Length];
        }

        public void Reset()
        {
            VulkanNative.vkResetCommandPool(Graphics.device, cmdPool, VkCommandPoolResetFlags.None);
        }
    }
}
