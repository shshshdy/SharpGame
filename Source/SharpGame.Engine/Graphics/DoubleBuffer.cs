using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public class DoubleBuffer : DisposeBase
    {
        public DeviceBuffer[] Buffer { get; } = new DeviceBuffer[2];
        public uint Size { get; }
        uint offset;

        public DoubleBuffer(uint size)
        {
            this.Size = size;
            Buffer[0] = DeviceBuffer.Create(BufferUsageFlags.UniformBuffer, MemoryPropertyFlags.HostVisible, size);
            Buffer[0].Map(0, size);
            Buffer[1] = DeviceBuffer.Create(BufferUsageFlags.UniformBuffer, MemoryPropertyFlags.HostVisible, size);
            Buffer[1].Map(0, size);
        }

        [MethodImpl((MethodImplOptions)0x100)]
        public unsafe uint Alloc(uint size, IntPtr data)
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
            var mappedBuf = Buffer[Graphics.Instance.WorkContext].Mapped;
            void* buf = (void*)(mappedBuf + (int)offset);
            Unsafe.CopyBlock(buf, (void*)data, size);
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
            Buffer[Graphics.Instance.WorkContext].Flush(offset, 0);
        }
    }
}
