using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public class DoubleBuffer : DisposeBase
    {
        Buffer[] buffers = new Buffer[2];
        public Buffer Buffer => buffers[Graphics.Instance.WorkContext];
        public IntPtr Mapped => buffers[Graphics.Instance.WorkContext].Mapped;

        public DoubleBuffer(BufferUsageFlags bufferUsage, uint size)
        {
            buffers[0] = new Buffer(bufferUsage, MemoryPropertyFlags.HostVisible, size);
            buffers[0].Map(0, size);
            buffers[1] = new Buffer(bufferUsage, MemoryPropertyFlags.HostVisible, size);
            buffers[1].Map(0, size);
        }

        public DoubleBuffer(BufferUsageFlags usageFlags, MemoryPropertyFlags memoryPropFlags, ulong size, 
            SharingMode sharingMode = SharingMode.Exclusive, uint[] queueFamilyIndices = null)
        {
            buffers[0] = new Buffer(usageFlags, memoryPropFlags, size, 1, sharingMode, queueFamilyIndices);
            buffers[0].Map(0, size);
            buffers[1] = new Buffer(usageFlags, memoryPropFlags, size, 1, sharingMode, queueFamilyIndices);
            buffers[1].Map(0, size);
        }

        public Buffer this[int index] => buffers[index];

        public void CreateView(Format format, ulong offset = 0, ulong range = ulong.MaxValue)
        {
            buffers[0].CreateView(format, offset, range);
            buffers[1].CreateView(format, offset, range);
        }

        public void SetData<T>(ref T data, uint offset = 0) where T : struct
        {
            SetData(Utilities.AsPointer(ref data), offset, (uint)Unsafe.SizeOf<T>());
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public void SetData(IntPtr data, ulong offset, ulong size)
        {
            Utilities.CopyBlock(Mapped + (int)offset, data, (int)size);
        }

        public void Flush(ulong size = ulong.MaxValue, ulong offset = 0)
        {
            Buffer.Flush(size, offset);
        }

        protected override void Destroy(bool disposing)
        {
            base.Destroy(disposing);

            buffers[0]?.Dispose();
            buffers[1]?.Dispose();
        }
    }


    public class DynamicBuffer : DisposeBase
    {
        public DoubleBuffer Buffer { get; }
        public uint Size { get; }

        uint offset;

        public DynamicBuffer(uint size)
        {
            this.Size = size;
            Buffer = new DoubleBuffer(BufferUsageFlags.UniformBuffer, size);
            
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public uint Alloc(uint size, IntPtr data)
        {
            uint uboAlignment = (uint)Device.Properties.limits.minUniformBufferOffsetAlignment;
            uint dynamicAlignment = ((size / uboAlignment) * uboAlignment + ((size % uboAlignment) > 0 ? uboAlignment : 0));
#if DEBUG
            if(offset + dynamicAlignment > Size)
            {
                Debug.Assert(false);
                return 0;
            }
#endif
            Buffer.SetData(data, offset, size);
            uint oldOffset = offset;
            offset += dynamicAlignment;
            return oldOffset;
        }

        public void Clear()
        {
            offset = 0;
        }

        public void Flush()
        {
            Buffer.Flush(offset, 0);
        }

        public void Flush(int index)
        {
            Buffer[index].Flush(offset, 0);
        }

        protected override void Destroy(bool disposing)
        {
            base.Destroy(disposing);

            Buffer?.Dispose();

        }
    }
}
