using System;
using System.Collections.Generic;
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

}
