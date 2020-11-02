using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public struct TransientBuffer
    {
        public Buffer buffer;
        public uint offset;
        public uint size;

        public IntPtr Data => buffer.Mapped + (int)offset;

        public void SetData<T>(ref T data) where T : unmanaged => buffer.SetData(ref data, offset);
        public VkDescriptorBufferInfo Descriptor => new VkDescriptorBufferInfo { buffer = buffer.buffer, offset = offset, range = size };
    }

    public class TransientBufferManager : DisposeBase
    {
        public struct TransientBufferDesc
        {
            public Buffer buffer;
            public uint size;
        }

        FastList<TransientBufferDesc> buffers = new FastList<TransientBufferDesc>();
        public BufferUsageFlags BufferUsageFlags { get; }
        public uint Size { get; }
        public ulong alignment;
        public TransientBufferManager(BufferUsageFlags usage, uint size)
        {
            BufferUsageFlags = usage;
            Size = size;

            if (usage == BufferUsageFlags.UniformBuffer)
            {
                alignment = Device.Properties.limits.minUniformBufferOffsetAlignment;
            }
            else if (usage == BufferUsageFlags.StorageBuffer)
            {
                alignment = Device.Properties.limits.minStorageBufferOffsetAlignment;
            }
            else if (usage == BufferUsageFlags.UniformTexelBuffer)
            {
                alignment = Device.Properties.limits.minTexelBufferOffsetAlignment;
            }
            else if (usage == BufferUsageFlags.IndexBuffer || usage == BufferUsageFlags.VertexBuffer || usage == BufferUsageFlags.IndirectBuffer)
            {
                // Used to calculate the offset, required when allocating memory (its value should be power of 2)
                alignment = 16;
            }
            else
            {
                throw new Exception("Usage not recognised");
            }
        }

        protected override void Destroy(bool disposing)
        {
            foreach(var buf in buffers)
            {
                buf.buffer.Release();
            }
        }


        public TransientBuffer Alloc(uint size)
        {
            var tb = new TransientBuffer
            {
                size = size
            };

            for (int i = 0; i < buffers.Count; i++)
            {
                ref TransientBufferDesc tbc = ref buffers.At(i);
                if(tbc.size + size < tbc.buffer.Size)
                {
                    tb.offset = tbc.size;
                    tb.buffer = tbc.buffer;
                    tbc.size += MathUtil.Align(size, (uint)alignment);
                    return tb;
                }
            }

            ref TransientBufferDesc desc = ref CreateNewBuffer();
            tb.offset = desc.size;
            tb.buffer = desc.buffer;
            desc.size += MathUtil.Align(size, (uint)alignment);
            return tb;
        }

        public void Reset()
        {
            for (int i = 0; i < buffers.Count; i++)
            {
                ref TransientBufferDesc tbc = ref buffers.At(i);                
                tbc.size = 0;                  
            }

        }

        public void Flush()
        {
            for (int i = 0; i < buffers.Count; i++)
            {
                ref TransientBufferDesc tbc = ref buffers.At(i);
                if(tbc.size > 0)
                {
                    tbc.buffer.Flush(tbc.size);
                }
            }
        }

        private ref TransientBufferDesc CreateNewBuffer()
        {
            var buffer = new Buffer(BufferUsageFlags, MemoryPropertyFlags.HostVisible, Size);
            buffer.Map();
            buffers.Add(new TransientBufferDesc { buffer = buffer, size = 0 });
            return ref buffers.At(buffers.Count - 1);
        }
    }
}
