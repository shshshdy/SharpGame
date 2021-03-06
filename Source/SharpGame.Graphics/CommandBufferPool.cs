﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading;


namespace SharpGame
{
    public class CommandBufferPool : DisposeBase
    {
        public CommandBuffer[] CommandBuffers { get; private set; }
        public uint QueueIndex { get; }
        private VkCommandPool cmdPool;
        private Vector<VkCommandBuffer> cmdBuffers = new Vector<VkCommandBuffer>();

        int currentIndex = 0;
        public int CurrentIndex => currentIndex;

        public IntPtr GetAddress(uint idx) => cmdBuffers.GetAddress(idx);

        public CommandBufferPool(uint queue, VkCommandPoolCreateFlags commandPoolCreateFlags)
        {
            QueueIndex = queue;
            cmdPool = Device.CreateCommandPool(queue, commandPoolCreateFlags);
        }

        public CommandBuffer this[int index]
        {
            get { return CommandBuffers[index]; }
        }

        public unsafe CommandBuffer AllocateCommandBuffer(VkCommandBufferLevel commandBufferLevel)
        {
            VkCommandBuffer cmdBuffer;
            Device.AllocateCommandBuffers(cmdPool, commandBufferLevel, 1, &cmdBuffer);
            return new CommandBuffer(cmdBuffer);
        }

        public unsafe void FreeCommandBuffer(CommandBuffer cmdBuffer)
        {
            fixed (VkCommandBuffer* cb = &cmdBuffer.commandBuffer)
                Device.FreeCommandBuffers(cmdPool, 1, cb);
        }

        public unsafe void Allocate(VkCommandBufferLevel commandBufferLevel, uint count)
        {
            if(cmdBuffers.Count > 0)
            {
                Free();
            }

            cmdBuffers.Resize(count);
            Device.AllocateCommandBuffers(cmdPool, commandBufferLevel, count, (VkCommandBuffer*)cmdBuffers.Data);

            CommandBuffers = new CommandBuffer[count];
            for (int i = 0; i < count; i++)
            {
                CommandBuffers[i] = new CommandBuffer(cmdBuffers[i]);
            }
        }

        public unsafe void Free()
        {
            if(CommandBuffers != null)
            {
                Device.FreeCommandBuffers(cmdPool, cmdBuffers.Count, (VkCommandBuffer*)cmdBuffers.Data);
                cmdBuffers.Count = 0;
                CommandBuffers = null;
            }
        }

        public CommandBuffer Get()
        {
            int idx = currentIndex;
            Interlocked.Increment(ref currentIndex);
            return CommandBuffers[idx % CommandBuffers.Length];
        }

        public void Clear()
        {
            currentIndex = 0;
        }

        public void Reset()
        {
            Device.ResetCommandPool(cmdPool, VkCommandPoolResetFlags.None);
        }
    }
}
