// Copyright (c) BobbyBao and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

namespace SharpGame
{
    /// <summary>
    /// Structure specifying a buffer memory barrier.
    /// </summary>
    public partial struct VkBufferMemoryBarrier
    {
        public unsafe VkBufferMemoryBarrier(
            VkBuffer buffer,
            VkAccessFlags srcAccessMask,
            VkAccessFlags dstAccessMask,
            ulong offset = 0,
            ulong size = Vulkan.WholeSize,
            uint srcQueueFamilyIndex = Vulkan.QueueFamilyIgnored,
            uint dstQueueFamilyIndex = Vulkan.QueueFamilyIgnored,
            void* pNext = default)
        {
            sType = VkStructureType.BufferMemoryBarrier;
            this.pNext = pNext;
            this.srcAccessMask = srcAccessMask;
            this.dstAccessMask = dstAccessMask;
            this.srcQueueFamilyIndex = srcQueueFamilyIndex;
            this.dstQueueFamilyIndex = dstQueueFamilyIndex;
            this.buffer = buffer;
            this.offset = offset;
            this.size = size;
        }
    }
}
