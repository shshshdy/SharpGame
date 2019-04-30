using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class UniformBuffer : GraphicsBuffer
    {
        public UniformBuffer()
        {

        }

        public void SetData<T>(int count = 1) where T : struct
        {
            var graphics = Get<Graphics>();
            int stride = Interop.SizeOf<T>();
            long size = stride * count;

            VulkanCore.Buffer buffer = graphics.Device.CreateBuffer(new BufferCreateInfo(size, BufferUsages.UniformBuffer));
            MemoryRequirements memoryRequirements = buffer.GetMemoryRequirements();
            // We require host visible memory so we can map it and write to it directly.
            // We require host coherent memory so that writes are visible to the GPU right after unmapping it.
            int memoryTypeIndex = graphics.MemoryProperties.MemoryTypes.IndexOf(
                memoryRequirements.MemoryTypeBits,
                MemoryProperties.HostVisible | MemoryProperties.HostCoherent);
            DeviceMemory memory = graphics.Device.AllocateMemory(new MemoryAllocateInfo(memoryRequirements.Size, memoryTypeIndex));
            buffer.BindMemory(memory);

            Buffer = buffer;
            Memory = memory;
            Stride = stride;
            Count = count;
            
        }

        public static UniformBuffer Create<T>(int count) where T : struct
        {
            var ub = new UniformBuffer();
            ub.SetData<T>(count);
            return Graphics.ToDispose(ub);
        }

    }
}
