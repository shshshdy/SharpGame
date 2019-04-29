using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using VulkanCore;

using Buffer = VulkanCore.Buffer;

namespace SharpGame
{
    public class GraphicsBuffer : Object
    {
        public byte[] Data { get; set; }
        public int Count { get; set; }
        public int Stride { get; set; }
        [IgnoreDataMember]
        public int Size => Count * Stride;
        internal Buffer Buffer { get; }
        protected DeviceMemory Memory { get; }

        public GraphicsBuffer()
        {
        }

        private GraphicsBuffer(Buffer buffer, DeviceMemory memory, int stride, int count)
        {
            Buffer = buffer;
            Memory = memory;
            Stride = stride;
            Count = count;
        }

        public IntPtr Map(long offset, long size) => Memory.Map(offset, size);
        public void Unmap() => Memory.Unmap();

        public override void Dispose()
        {
            Memory.Dispose();
            Buffer.Dispose();
        }

        public static implicit operator Buffer(GraphicsBuffer value) => value.Buffer;

        public static GraphicsBuffer DynamicUniform<T>(int count) where T : struct
        {
            var graphics = Get<Graphics>();
            int stride = Interop.SizeOf<T>();
            long size = stride * count;

            Buffer buffer = graphics.Device.CreateBuffer(new BufferCreateInfo(size, BufferUsages.UniformBuffer));
            MemoryRequirements memoryRequirements = buffer.GetMemoryRequirements();
            // We require host visible memory so we can map it and write to it directly.
            // We require host coherent memory so that writes are visible to the GPU right after unmapping it.
            int memoryTypeIndex = graphics.MemoryProperties.MemoryTypes.IndexOf(
                memoryRequirements.MemoryTypeBits,
                MemoryProperties.HostVisible | MemoryProperties.HostCoherent);
            DeviceMemory memory = graphics.Device.AllocateMemory(new MemoryAllocateInfo(memoryRequirements.Size, memoryTypeIndex));
            buffer.BindMemory(memory);

            return graphics.ToDispose(new GraphicsBuffer(buffer, memory, stride, count));
        }

        public static GraphicsBuffer Index(int[] indices)
        {
            var graphics = Get<Graphics>();

            int stride = sizeof(int);
            long size = indices.Length * stride;
            return Index(Utilities.AsPointer(ref indices[0]), stride, indices.Length);
        }

        public static GraphicsBuffer Index(short[] indices)
        {
            var graphics = Get<Graphics>();

            int stride = sizeof(short);
            long size = indices.Length * stride;
            return Index(Utilities.AsPointer(ref indices[0]), stride, indices.Length);
        }

        public unsafe static GraphicsBuffer Index(IntPtr indices, int stride, int count)
        {
            var graphics = Get<Graphics>();
            long size = count * stride;
            // Create staging buffer.
            Buffer stagingBuffer = graphics.Device.CreateBuffer(new BufferCreateInfo(size, BufferUsages.TransferSrc));
            MemoryRequirements stagingReq = stagingBuffer.GetMemoryRequirements();
            int stagingMemoryTypeIndex = graphics.MemoryProperties.MemoryTypes.IndexOf(
                stagingReq.MemoryTypeBits,
                MemoryProperties.HostVisible | MemoryProperties.HostCoherent);
            DeviceMemory stagingMemory = graphics.Device.AllocateMemory(new MemoryAllocateInfo(stagingReq.Size, stagingMemoryTypeIndex));
            IntPtr indexPtr = stagingMemory.Map(0, stagingReq.Size);
            //Interop.Write(indexPtr, indices);
            stagingMemory.Unmap();
            stagingBuffer.BindMemory(stagingMemory);

            // Create a device local buffer.
            Buffer buffer = graphics.Device.CreateBuffer(new BufferCreateInfo(size, BufferUsages.IndexBuffer | BufferUsages.TransferDst));
            MemoryRequirements req = buffer.GetMemoryRequirements();
            int memoryTypeIndex = graphics.MemoryProperties.MemoryTypes.IndexOf(
                req.MemoryTypeBits,
                MemoryProperties.DeviceLocal);
            DeviceMemory memory = graphics.Device.AllocateMemory(new MemoryAllocateInfo(req.Size, memoryTypeIndex));
            buffer.BindMemory(memory);

            // Copy the data from staging buffer to device local buffer.
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

            return new GraphicsBuffer(buffer, memory, stride, count);
        }

        public unsafe static GraphicsBuffer Vertex<T>(T[] vertices) where T : struct
        {
            var graphics = Get<Graphics>();
            int stride = Interop.SizeOf<T>();
            long size = vertices.Length * stride;
            return Vertex((IntPtr)Unsafe.AsPointer(ref vertices[0]), stride, vertices.Length);
        }

        public unsafe static GraphicsBuffer Vertex(IntPtr vertices, int stride, int count)
        {
            var graphics = Get<Graphics>();
            long size = count * stride;

            // Create a staging buffer that is writable by host.
            Buffer stagingBuffer = graphics.Device.CreateBuffer(new BufferCreateInfo(size, BufferUsages.TransferSrc));
            MemoryRequirements stagingReq = stagingBuffer.GetMemoryRequirements();
            int stagingMemoryTypeIndex = graphics.MemoryProperties.MemoryTypes.IndexOf(
                stagingReq.MemoryTypeBits,
                MemoryProperties.HostVisible | MemoryProperties.HostCoherent);
            DeviceMemory stagingMemory = graphics.Device.AllocateMemory(new MemoryAllocateInfo(stagingReq.Size, stagingMemoryTypeIndex));
            IntPtr vertexPtr = stagingMemory.Map(0, stagingReq.Size);
            //Interop.Write(vertexPtr, vertices);
            Unsafe.CopyBlock((void*)vertexPtr, (void*)vertices, (uint)size);
            stagingMemory.Unmap();
            stagingBuffer.BindMemory(stagingMemory);

            // Create a device local buffer where the vertex data will be copied and which will be used for rendering.
            Buffer buffer = graphics.Device.CreateBuffer(new BufferCreateInfo(size, BufferUsages.VertexBuffer | BufferUsages.TransferDst));
            MemoryRequirements req = buffer.GetMemoryRequirements();
            int memoryTypeIndex = graphics.MemoryProperties.MemoryTypes.IndexOf(
                req.MemoryTypeBits,
                MemoryProperties.DeviceLocal);
            DeviceMemory memory = graphics.Device.AllocateMemory(new MemoryAllocateInfo(req.Size, memoryTypeIndex));
            buffer.BindMemory(memory);

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

            return new GraphicsBuffer(buffer, memory, stride, count);
        }

        public static GraphicsBuffer Storage<T>( T[] data) where T : struct
        {
            Graphics ctx = Get<Graphics>();
            int stride = Interop.SizeOf<T>();
            long size = data.Length * stride;

            // Create a staging buffer that is writable by host.
            Buffer stagingBuffer = ctx.Device.CreateBuffer(new BufferCreateInfo(size, BufferUsages.TransferSrc));
            MemoryRequirements stagingReq = stagingBuffer.GetMemoryRequirements();
            int stagingMemoryTypeIndex = ctx.MemoryProperties.MemoryTypes.IndexOf(
                stagingReq.MemoryTypeBits,
                MemoryProperties.HostVisible | MemoryProperties.HostCoherent);
            DeviceMemory stagingMemory = ctx.Device.AllocateMemory(new MemoryAllocateInfo(stagingReq.Size, stagingMemoryTypeIndex));
            IntPtr vertexPtr = stagingMemory.Map(0, stagingReq.Size);
            Interop.Write(vertexPtr, data);
            stagingMemory.Unmap();
            stagingBuffer.BindMemory(stagingMemory);

            // Create a device local buffer where the data will be copied.
            Buffer buffer = ctx.Device.CreateBuffer(new BufferCreateInfo(size, BufferUsages.VertexBuffer | BufferUsages.StorageBuffer | BufferUsages.TransferDst));
            MemoryRequirements req = buffer.GetMemoryRequirements();
            int memoryTypeIndex = ctx.MemoryProperties.MemoryTypes.IndexOf(
                req.MemoryTypeBits,
                MemoryProperties.DeviceLocal);
            DeviceMemory memory = ctx.Device.AllocateMemory(new MemoryAllocateInfo(req.Size, memoryTypeIndex));
            buffer.BindMemory(memory);

            // Copy the data from staging buffers to device local buffers.
            CommandBuffer cmdBuffer = ctx.GraphicsCommandPool.AllocateBuffers(new CommandBufferAllocateInfo(CommandBufferLevel.Primary, 1))[0];
            cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.OneTimeSubmit));
            cmdBuffer.CmdCopyBuffer(stagingBuffer, buffer, new BufferCopy(size));
            cmdBuffer.End();

            // Submit.
            Fence fence = ctx.Device.CreateFence();
            ctx.GraphicsQueue.Submit(new SubmitInfo(commandBuffers: new[] { cmdBuffer }), fence);
            fence.Wait();

            // Cleanup.
            fence.Dispose();
            cmdBuffer.Dispose();
            stagingBuffer.Dispose();
            stagingMemory.Dispose();

            return ctx.ToDispose(new GraphicsBuffer(buffer, memory, stride, data.Length));
        }
    }
}
