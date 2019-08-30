using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public struct TransientBuffer
    {
        public DeviceBuffer buffer;
        public uint offset;
        public uint size;

        public IntPtr Data => buffer.Mapped + (int)offset;
    }

    public class TransientBufferManager : DisposeBase
    {
        public struct TransientBufferDesc
        {
            public DeviceBuffer buffer;
            public uint size;
        }

        FastList<TransientBufferDesc>[] buffers = new [] { new FastList<TransientBufferDesc>(), new FastList<TransientBufferDesc> ()};
        public BufferUsageFlags BufferUsageFlags { get; }
        public uint Size { get; }

        public TransientBufferManager(BufferUsageFlags useage, uint size)
        {
            BufferUsageFlags = useage;
            Size = size;
        }

        protected override void Destroy(bool disposing)
        {
            foreach(var buf in buffers[0])
            {
                buf.buffer.Release();
            }

            foreach (var buf in buffers[1])
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

            var currentBuffers = buffers[Graphics.Instance.WorkContext];
            for (int i = 0; i < currentBuffers.Count; i++)
            {
                ref TransientBufferDesc tbc = ref currentBuffers.At(i);
                if(tbc.size + size < tbc.buffer.Size)
                {
                    tb.offset = tbc.size;
                    tb.buffer = tbc.buffer;
                    tbc.size += size;
                    return tb;
                }
            }

            ref TransientBufferDesc desc = ref CreateNewBuffer();
            tb.offset = desc.size;
            tb.buffer = desc.buffer;
            desc.size += size;
            return tb;
        }

        public void Reset()
        {
            var currentBuffers = buffers[Graphics.Instance.WorkContext];
            for (int i = 0; i < currentBuffers.Count; i++)
            {
                ref TransientBufferDesc tbc = ref currentBuffers.At(i);                
                tbc.size = 0;                  
            }

        }

        public void Flush()
        {
            var currentBuffers = buffers[Graphics.Instance.RenderContext];
            for (int i = 0; i < currentBuffers.Count; i++)
            {
                ref TransientBufferDesc tbc = ref currentBuffers.At(i);
                if(tbc.size > 0)
                {
                    tbc.buffer.Flush(tbc.size);
                }
            }
        }

        unsafe ref TransientBufferDesc CreateNewBuffer()
        {
            var currentBuffers = buffers[Graphics.Instance.WorkContext];
            var buffer = DeviceBuffer.Create(BufferUsageFlags, MemoryPropertyFlags.HostVisible, 1, Size);
            buffer.Map();
            currentBuffers.Add(new TransientBufferDesc { buffer = buffer, size = 0 });
            return ref currentBuffers.At(currentBuffers.Count - 1);
        }
    }
}
