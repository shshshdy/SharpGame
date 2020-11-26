using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class DeviceMemory : RefCounted
    {
        internal VkDeviceMemory memory;
        public IntPtr Mapped { get; private set; }
        public ulong Count { get; set; }
        public ulong Size { get; set; }

        internal VkMemoryPropertyFlags memoryPropertyFlags = VkMemoryPropertyFlags.DeviceLocal;
        internal ulong allocationSize = 0;
        internal uint memoryTypeIndex = 0;

        public void Allocate(in VkMemoryRequirements memReqs)
        {
            var memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, memoryPropertyFlags);
            VkMemoryAllocateInfo memAllocInfo = new VkMemoryAllocateInfo(memReqs.size, memoryTypeIndex);
            memory = Device.AllocateMemory(ref memAllocInfo);
            
            allocationSize = memAllocInfo.allocationSize;
            memoryTypeIndex = memAllocInfo.memoryTypeIndex;
        }

        public ref T Map<T>(ulong offset = 0) where T : struct
        {
            Mapped = Device.MapMemory(memory, (ulong)offset, Size, VkMemoryMapFlags.None);
            return ref Utilities.As<T>(Mapped);
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
                sType = VkStructureType.MappedMemoryRange
            };
            mappedRange.memory = memory;
            mappedRange.offset = offset;
            mappedRange.size = size;
            Device.FlushMappedMemoryRanges(1, ref mappedRange);
        }

        public unsafe void Invalidate(ulong size = Vulkan.WholeSize, ulong offset = 0)
        {
            VkMappedMemoryRange mappedRange = new VkMappedMemoryRange
            {
                sType = VkStructureType.MappedMemoryRange
            };
            mappedRange.memory = memory;
            mappedRange.offset = offset;
            mappedRange.size = size;
            Device.InvalidateMappedMemoryRanges(1, ref mappedRange);
        }

        protected override void Destroy()
        {
            if (memory.Handle != 0)
            {
                Device.FreeMemory(memory);
            }
        }
    }
}
