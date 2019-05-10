using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class IndexBuffer : GraphicsBuffer
    {
        public IndexBuffer()
        {
        }
        
        public void Init<T>(T[] indices) where T : struct
        {
            var graphics = Get<Graphics>();

            int stride = Interop.SizeOf<T>();
            long size = indices.Length * stride;

            Init(Utilities.AsPointer(ref indices[0]), stride, indices.Length);
        }

        public void Init(IntPtr indices, int stride, int count)
        {
            var graphics = Get<Graphics>();
            long size = count * stride;

            Vulkan.Buffer stagingBuffer = null;
            DeviceMemory stagingMemory = null;
            if (indices != IntPtr.Zero)
            {
                // Create staging buffer.

                stagingBuffer = Graphics.Device.CreateBuffer(new BufferCreateInfo(size, BufferUsages.TransferSrc));
                MemoryRequirements stagingReq = stagingBuffer.GetMemoryRequirements();
                int stagingMemoryTypeIndex = Graphics.MemoryProperties.MemoryTypes.IndexOf(
                    stagingReq.MemoryTypeBits,
                    MemoryProperties.HostVisible | MemoryProperties.HostCoherent);

                stagingMemory = Graphics.Device.AllocateMemory(new MemoryAllocateInfo(stagingReq.Size, stagingMemoryTypeIndex));
                IntPtr indexPtr = stagingMemory.Map(0, stagingReq.Size);
                Utilities.CopyMemory(indexPtr, indices, (int)size);
                 stagingMemory.Unmap();
                stagingBuffer.BindMemory(stagingMemory);
            }


            // Create a device local buffer.
            Vulkan.Buffer buffer = Graphics.Device.CreateBuffer(new BufferCreateInfo(size, BufferUsages.IndexBuffer | BufferUsages.TransferDst));
            MemoryRequirements req = buffer.GetMemoryRequirements();
            int memoryTypeIndex = Graphics.MemoryProperties.MemoryTypes.IndexOf(
                req.MemoryTypeBits,
               Dynamic ? MemoryProperties.HostVisible | MemoryProperties.HostCoherent : MemoryProperties.DeviceLocal
               );
            DeviceMemory memory = Graphics.Device.AllocateMemory(new MemoryAllocateInfo(req.Size, memoryTypeIndex));
            buffer.BindMemory(memory);

            if (indices != IntPtr.Zero)
            {
                // Copy the data from staging buffer to device local buffer.
                CommandBuffer cmdBuffer = graphics.GraphicsCommandPool.AllocateBuffers(new CommandBufferAllocateInfo(CommandBufferLevel.Primary, 1))[0];
                cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.OneTimeSubmit));
                cmdBuffer.CmdCopyBuffer(stagingBuffer, buffer, new BufferCopy(size));
                cmdBuffer.End();

                // Submit.
                Fence fence = Graphics.Device.CreateFence();
                graphics.GraphicsQueue.Submit(new SubmitInfo(commandBuffers: new[] { cmdBuffer }), fence);
                fence.Wait();

                // Cleanup.
                fence.Dispose();
                cmdBuffer.Dispose();
                stagingBuffer.Dispose();
                stagingMemory.Dispose();

            }

            Buffer = buffer;
            Memory = memory;
            Stride = stride;
            Count = count;

        }

        public static IndexBuffer Create(int[] indices, bool dynamic = false)
        {
            var ib = new IndexBuffer
            {
                Dynamic = dynamic
            };
            ib.Init(indices);
            return Graphics.ToDispose(ib);
        }

        public static IndexBuffer Create(short[] indices, bool dynamic = false)
        {
            var ib = new IndexBuffer
            {
                Dynamic = dynamic
            };
            ib.Init(indices);
            return Graphics.ToDispose(ib);
        }

        public static IndexBuffer Create(IntPtr indices, int stride, int count, bool dynamic = false)
        {
            var ib = new IndexBuffer
            {
                Dynamic = dynamic
            };
            ib.Init(indices, stride, count);
            return Graphics.ToDispose(ib);
        }

    }
}
