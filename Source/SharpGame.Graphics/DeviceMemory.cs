using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class DeviceMemory<T> : HandleBase<T> where T : IDisposable
    {
        internal VkDeviceMemory memory;
        public IntPtr Mapped { get; private set; }

        internal VkMemoryPropertyFlags memoryPropertyFlags = VkMemoryPropertyFlags.DeviceLocal;
        public ulong AllocationSize { get; private set; } = 0;

        public void Allocate(in VkMemoryRequirements memReqs)
        {
            var memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, memoryPropertyFlags);
            VkMemoryAllocateInfo memAllocInfo = new VkMemoryAllocateInfo(memReqs.size, memoryTypeIndex);
            memory = Device.AllocateMemory(ref memAllocInfo);
            AllocationSize = memAllocInfo.allocationSize;
        }

        public ref K Map<K>(ulong offset = 0) where K : struct
        {
            Mapped = Device.MapMemory(memory, offset, Vulkan.WholeSize, VkMemoryMapFlags.None);
            return ref Utilities.As<K>(Mapped);
        }

        public IntPtr Map(ulong offset = 0, ulong size = Vulkan.WholeSize)
        {
            Mapped = Device.MapMemory(memory, offset, size, 0);
            return Mapped;
        }

        public void Unmap()
        {
            Device.UnmapMemory(memory);
            Mapped = IntPtr.Zero;
        }

        public unsafe void Flush(ulong size = Vulkan.WholeSize, ulong offset = 0)
        {
            VkMappedMemoryRange mappedRange = new VkMappedMemoryRange
            {
                sType = VkStructureType.MappedMemoryRange,
                memory = memory,
                offset = offset,
                size = size
            };
            Device.FlushMappedMemoryRanges(1, ref mappedRange);
        }

        public unsafe void Invalidate(ulong size = Vulkan.WholeSize, ulong offset = 0)
        {
            VkMappedMemoryRange mappedRange = new VkMappedMemoryRange
            {
                sType = VkStructureType.MappedMemoryRange,
                memory = memory,
                offset = offset,
                size = size
            };
            Device.InvalidateMappedMemoryRanges(1, ref mappedRange);
        }

        protected override void Destroy(bool disposing)
        {
            if (memory != VkDeviceMemory.Null)
            {
                Device.FreeMemory(memory);
            }

            base.Destroy(disposing);
        }
    }
}
