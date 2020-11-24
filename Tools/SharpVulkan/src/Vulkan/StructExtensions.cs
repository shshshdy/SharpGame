using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{
    public partial struct VkMemoryAllocateInfo
    {
        public VkMemoryAllocateInfo(ulong allocationSize, uint memoryTypeIndex)
        {
            this.sType = VkStructureType.MemoryAllocateInfo;
            this.allocationSize = allocationSize;
            this.memoryTypeIndex = memoryTypeIndex;
            unsafe { this.pNext = null; }
        }
    }

    public partial struct VkBufferCreateInfo
    {
        public unsafe VkBufferCreateInfo(VkBufferUsageFlags usage, ulong size, uint[] queueFamilyIndices = null)
        {
            this.sType = VkStructureType.BufferCreateInfo;
            this.pNext = null;
            this.flags = VkBufferCreateFlags.None;
            this.sharingMode = VkSharingMode.Exclusive;

            if (queueFamilyIndices != null)
            {
                this.queueFamilyIndexCount = (uint)queueFamilyIndices.Length;
                this.pQueueFamilyIndices = (uint*)Unsafe.AsPointer(ref queueFamilyIndices[0]);
            }
            else
            {
                this.queueFamilyIndexCount = 0;
                this.pQueueFamilyIndices = null;
            }

            this.usage = usage;
            this.size = size;
        }
    }

    public partial struct VkMemoryBarrier
    {
        public unsafe VkMemoryBarrier(VkAccessFlags srcAccessMask, VkAccessFlags dstAccessMask)
        {
            this.sType = VkStructureType.MemoryBarrier;
            this.pNext = null;
            this.srcAccessMask = (VkAccessFlags)srcAccessMask;
            this.dstAccessMask = (VkAccessFlags)dstAccessMask;
        }
    }
}
