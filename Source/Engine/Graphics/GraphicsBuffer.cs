using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using VulkanCore;

using Buffer = VulkanCore.Buffer;

namespace SharpGame
{
    public class GraphicsBuffer : Object, IBindable
    {
        public byte[] Data { get; set; }
        public int Count { get; set; }
        public int Stride { get; set; }
        public BufferUsages BufferUsages { get; set; }
        public bool Dynamic { get; set; }


        [IgnoreDataMember]
        public int Size => Count * Stride;
        [IgnoreDataMember]
        public Buffer Buffer { get; set; }
        protected DeviceMemory Memory { get; set; }

        public GraphicsBuffer()
        {
        }

        public GraphicsBuffer(BufferUsages bufferUsages)
        {
            BufferUsages = bufferUsages;
        }


        protected override void Destroy()
        {
            Memory.Dispose();
            Buffer.Dispose();
        }

        public static GraphicsBuffer CreateDynamic<T>(BufferUsages bufferUsages, int count = 1) where T : struct
        {
            var buf = new GraphicsBuffer
            {
                BufferUsages = bufferUsages,
                Dynamic = true
            };

            buf.Init<T>(count);
            return Graphics.ToDispose(buf);
        }

        public static GraphicsBuffer CreateUniform<T>(int count = 1) where T : struct
        {
            return GraphicsBuffer.CreateDynamic<T>(BufferUsages.UniformBuffer, count);
        }

        public static GraphicsBuffer Create<T>(BufferUsages bufferUsages, T[] data, bool dynamic = false) where T : struct
        {
            var buf = new GraphicsBuffer
            {
                BufferUsages = bufferUsages,
                Dynamic = dynamic
            };

            buf.Init(data);
            return Graphics.ToDispose(buf);
        }

        public static GraphicsBuffer Create(BufferUsages bufferUsages, IntPtr vertices, int stride, int count, bool dynamic = false)
        {
            var buf = new GraphicsBuffer
            {
                BufferUsages = bufferUsages,
                Dynamic = dynamic
            };

            buf.Init(vertices, stride, count);
            return Graphics.ToDispose(buf);
        }

        public void Init<T>(T[] data) where T : struct
        {
            int stride = Interop.SizeOf<T>();
            long size = data.Length * stride;
            Init(Utilities.AsPointer(ref data[0]), stride, data.Length);
        }

        public void Init<T>(int count = 1) where T : struct
        {
            int stride = Interop.SizeOf<T>();
            long size = count * stride;
            Init(IntPtr.Zero, stride, count);
        }

        public void Init(IntPtr vertices, int stride, int count)
        {
            var graphics = Get<Graphics>();
            long size = count * stride;

            // Create a staging buffer that is writable by host.
            VulkanCore.Buffer stagingBuffer = null;
            DeviceMemory stagingMemory = null;
            if (vertices != IntPtr.Zero)
            {
                stagingBuffer = Graphics.Device.CreateBuffer(new BufferCreateInfo(size, BufferUsages.TransferSrc));
                MemoryRequirements stagingReq = stagingBuffer.GetMemoryRequirements();
                int stagingMemoryTypeIndex = Graphics.MemoryProperties.MemoryTypes.IndexOf(
                    stagingReq.MemoryTypeBits,
                    MemoryProperties.HostVisible | MemoryProperties.HostCoherent);

                stagingMemory = Graphics.Device.AllocateMemory(new MemoryAllocateInfo(stagingReq.Size, stagingMemoryTypeIndex));
                IntPtr vertexPtr = stagingMemory.Map(0, stagingReq.Size);
                Utilities.CopyMemory(vertexPtr, vertices, (int)size);
                stagingMemory.Unmap();
                stagingBuffer.BindMemory(stagingMemory);
            }

            // Create a device local buffer where the vertex data will be copied and which will be used for rendering.
            VulkanCore.Buffer buffer = Graphics.Device.CreateBuffer(new BufferCreateInfo(size, BufferUsages | BufferUsages.TransferDst));
            MemoryRequirements req = buffer.GetMemoryRequirements();
            int memoryTypeIndex = Graphics.MemoryProperties.MemoryTypes.IndexOf(
                req.MemoryTypeBits,
               Dynamic ? MemoryProperties.HostVisible | MemoryProperties.HostCoherent : MemoryProperties.DeviceLocal
                );

            DeviceMemory memory = Graphics.Device.AllocateMemory(new MemoryAllocateInfo(req.Size, memoryTypeIndex));
            buffer.BindMemory(memory);

            if (vertices != IntPtr.Zero)
            {
                // Copy the data from staging buffers to device local buffers.
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

            this.Buffer = buffer;
            this.Memory = memory;
            this.Stride = stride;
            this.Count = count;
        }

        public void SetData<T>(ref T data, int offset = 0)
        {
            int size = Unsafe.SizeOf<T>();
            var dest = Map(offset, size);
            Interop.Write(dest, ref data);
            Unmap();
        }

        public void SetData<T>(T[] data, int offset = 0)
        {
            int size = Unsafe.SizeOf<T>()*data.Length;
            var dest = Map(offset, size);
            Interop.Write(dest, ref data);
            Unmap();
        }

        public void SetData(IntPtr data, int offset, int size)
        {
            var dest = Map(offset, size);
            Utilities.CopyMemory(dest, data, size);
            Unmap();
        }

        public IntPtr Map(long offset, long size) => Memory.Map(offset, size);
        public void Unmap() => Memory.Unmap();

        public static implicit operator Buffer(GraphicsBuffer value) => value.Buffer;
        
    }
}
