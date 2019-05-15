﻿using Vulkan;
using VkDeviceSize = System.UInt64;
using static Vulkan.VulkanNative;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System;

namespace SharpGame
{
    public interface IBindable { }

    public unsafe class GraphicsBuffer
    {
        public VkBuffer buffer;
        public VkDevice device;
        public VkDeviceMemory memory;
        public VkDescriptorBufferInfo descriptor;
        public VkDeviceSize size = 0;
        public VkDeviceSize alignment = 0;
        public void* mapped = null;

        /** @brief Usage flags to be filled by external source at buffer creation (to query at some later point) */
        public VkBufferUsageFlags usageFlags;
        /** @brief Memory propertys flags to be filled by external source at buffer creation (to query at some later point) */
        public VkMemoryPropertyFlags memoryPropertyFlags;

        /** 
        * Map a memory range of this buffer. If successful, mapped points to the specified buffer range.
        * 
        * @param size (Optional) Size of the memory range to map. Pass WholeSize to map the complete buffer range.
        * @param offset (Optional) Byte offset from beginning
        * 
        * @return VkResult of the buffer mapping call
        */
        public VkResult map(VkDeviceSize size = WholeSize, VkDeviceSize offset = 0)
        {
            void* mappedLocal;
            var result = vkMapMemory(device, memory, offset, size, 0, &mappedLocal);
            mapped = mappedLocal;
            return result;
        }

        /**
        * Unmap a mapped memory range
        *
        * @note Does not return a result as vkUnmapMemory can't fail
        */
        public void unmap()
        {
            if (mapped != null)
            {
                vkUnmapMemory(device, memory);
                mapped = null;
            }
        }

        /** 
        * Attach the allocated memory block to the buffer
        * 
        * @param offset (Optional) Byte offset (from the beginning) for the memory region to bind
        * 
        * @return VkResult of the bindBufferMemory call
        */
        public VkResult bind(VkDeviceSize offset = 0)
        {
            return vkBindBufferMemory(device, buffer, memory, offset);
        }

        /**
        * Setup the default descriptor for this buffer
        *
        * @param size (Optional) Size of the memory range of the descriptor
        * @param offset (Optional) Byte offset from beginning
        *
        */
        public void setupDescriptor(VkDeviceSize size = WholeSize, VkDeviceSize offset = 0)
        {
            descriptor.offset = offset;
            descriptor.buffer = buffer;
            descriptor.range = size;
        }

        /**
        * Copies the specified data to the mapped buffer
        * 
        * @param data Pointer to the data to copy
        * @param size Size of the data to copy in machine units
        *
        */
        public void copyTo(void* data, VkDeviceSize size)
        {
            Debug.Assert(mapped != null);
            Debug.Assert(size <= uint.MaxValue);
            Unsafe.CopyBlock(mapped, data, (uint)size);
        }

        public void SetData(void* data, uint offset, uint size)
        {
            map(size, offset);
            copyTo(data, size);
            unmap();
        }

        /** 
        * Flush a memory range of the buffer to make it visible to the device
        *
        * @note Only required for non-coherent memory
        *
        * @param size (Optional) Size of the memory range to flush. Pass WholeSize to flush the complete buffer range.
        * @param offset (Optional) Byte offset from beginning
        *
        * @return VkResult of the flush call
        */
        public VkResult flush(VkDeviceSize size = WholeSize, VkDeviceSize offset = 0)
        {
            VkMappedMemoryRange mappedRange = VkMappedMemoryRange.New();
            mappedRange.memory = memory;
            mappedRange.offset = offset;
            mappedRange.size = size;
            return vkFlushMappedMemoryRanges(device, 1, &mappedRange);
        }

        /**
        * Invalidate a memory range of the buffer to make it visible to the host
        *
        * @note Only required for non-coherent memory
        *
        * @param size (Optional) Size of the memory range to invalidate. Pass WholeSize to invalidate the complete buffer range.
        * @param offset (Optional) Byte offset from beginning
        *
        * @return VkResult of the invalidate call
        */
        public VkResult invalidate(VkDeviceSize size = WholeSize, VkDeviceSize offset = 0)
        {
            VkMappedMemoryRange mappedRange = VkMappedMemoryRange.New();
            mappedRange.memory = memory;
            mappedRange.offset = offset;
            mappedRange.size = size;
            return vkInvalidateMappedMemoryRanges(device, 1, &mappedRange);
        }

        /** 
        * Release all Vulkan resources held by this buffer
        */
        public void destroy()
        {
            if (buffer.Handle != 0)
            {
                vkDestroyBuffer(device, buffer, null);
            }
            if (memory.Handle != 0)
            {
                vkFreeMemory(device, memory, null);
            }
        }

    }
}
