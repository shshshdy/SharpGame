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
        public bool Dynamic { get; set; }

        [IgnoreDataMember]
        public int Size => Count * Stride;
        [IgnoreDataMember]
        public Buffer Buffer { get; set; }
        protected DeviceMemory Memory { get; set; }

        public GraphicsBuffer()
        {
        }

        protected override void Destroy()
        {
            Memory.Dispose();
            Buffer.Dispose();
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
