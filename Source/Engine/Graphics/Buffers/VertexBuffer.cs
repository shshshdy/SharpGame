using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class VertexBuffer : GraphicsBuffer
    {
        public VertexBuffer()
        {
        }

        public void SetData<T>(T[] vertices) where T : struct
        {
            var graphics = Get<Graphics>();
            int stride = Interop.SizeOf<T>();
            long size = vertices.Length * stride;

            SetData(Utilities.AsPointer(ref vertices[0]), stride, vertices.Length);
        }

        public void SetData(IntPtr vertices, int stride, int count)
        {
            var graphics = Get<Graphics>();
            long size = count * stride;

            // Create a staging buffer that is writable by host.
            VulkanCore.Buffer stagingBuffer = null;
            DeviceMemory stagingMemory = null;
            if (vertices != IntPtr.Zero)
            {
                stagingBuffer = graphics.Device.CreateBuffer(new BufferCreateInfo(size, BufferUsages.TransferSrc));
                MemoryRequirements stagingReq = stagingBuffer.GetMemoryRequirements();
                int stagingMemoryTypeIndex = graphics.MemoryProperties.MemoryTypes.IndexOf(
                    stagingReq.MemoryTypeBits,
                    MemoryProperties.HostVisible | MemoryProperties.HostCoherent);

                stagingMemory = graphics.Device.AllocateMemory(new MemoryAllocateInfo(stagingReq.Size, stagingMemoryTypeIndex));
                IntPtr vertexPtr = stagingMemory.Map(0, stagingReq.Size);
                Utilities.CopyMemory(vertexPtr, vertices, (int)size);
                stagingMemory.Unmap();
                stagingBuffer.BindMemory(stagingMemory);
            }


            // Create a device local buffer where the vertex data will be copied and which will be used for rendering.
            VulkanCore.Buffer buffer = graphics.Device.CreateBuffer(new BufferCreateInfo(size, BufferUsages.VertexBuffer | BufferUsages.TransferDst));
            MemoryRequirements req = buffer.GetMemoryRequirements();
            int memoryTypeIndex = graphics.MemoryProperties.MemoryTypes.IndexOf(
                req.MemoryTypeBits,
               Dynamic ? MemoryProperties.HostVisible | MemoryProperties.HostCoherent :  MemoryProperties.DeviceLocal
                );

            DeviceMemory memory = graphics.Device.AllocateMemory(new MemoryAllocateInfo(req.Size, memoryTypeIndex));
            buffer.BindMemory(memory);

            if(vertices != IntPtr.Zero)
            {
                // Copy the data from staging buffers to device local buffers.
                CommandBuffer cmdBuffer = graphics.GraphicsCommandPool.AllocateBuffers(new CommandBufferAllocateInfo(CommandBufferLevel.Primary, 1))[0];
                cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.OneTimeSubmit));
                cmdBuffer.CmdCopyBuffer(stagingBuffer, buffer, new BufferCopy(size));
                cmdBuffer.End();

                // Submit.
                Fence fence = graphics.Device.CreateFence();
                graphics.GraphicsQueue.Submit(new SubmitInfo(commandBuffers: new[] { cmdBuffer }), fence);
                fence.Wait();

                // Cleanup.
                fence.Dispose();

                cmdBuffer.Dispose();

                stagingBuffer.Dispose();
                stagingMemory.Dispose();
            }            

            this.Buffer = buffer;
            this.Memory = memory;
            this.Stride = stride;
            this.Count = count;
        }

        public static VertexBuffer Create<T>(T[] vertices, bool dynamic = false) where T : struct
        {
            VertexBuffer vb = new VertexBuffer
            {
                Dynamic = dynamic
            };
            
            vb.SetData(vertices);
            return Graphics.ToDispose(vb);
        }

        public static GraphicsBuffer Create(IntPtr vertices, int stride, int count, bool dynamic = false)
        {
            VertexBuffer vb = new VertexBuffer
            {
                Dynamic = dynamic
            };

            vb.SetData(vertices, stride, count);
            return Graphics.ToDispose(vb);
        }

    }
}
