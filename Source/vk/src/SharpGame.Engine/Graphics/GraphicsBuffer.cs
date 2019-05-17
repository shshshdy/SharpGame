using Vulkan;
using VkDeviceSize = System.UInt64;
using static Vulkan.VulkanNative;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System;

namespace SharpGame
{
    public interface IBindable { }

    public unsafe class GraphicsBuffer : DisposeBase, IBindable
    {
        public int Stride { get; set; }
        public int Count { get; set; }

        public VkBuffer buffer;
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
            var result = vkMapMemory(Graphics.device, memory, offset, size, 0, &mappedLocal);
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
                vkUnmapMemory(Graphics.device, memory);
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
            return vkBindBufferMemory(Graphics.device, buffer, memory, offset);
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

        public void SetData<T>(ref T data, int offset = 0) where T : struct
        {
            SetData(Unsafe.AsPointer(ref data), (uint)offset, (uint)Unsafe.SizeOf<T>());
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
            return vkFlushMappedMemoryRanges(Graphics.device, 1, &mappedRange);
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
            return vkInvalidateMappedMemoryRanges(Graphics.device, 1, &mappedRange);
        }

        /** 
        * Release all Vulkan resources held by this buffer
        */
        protected override void Destroy()
        {
            if (buffer.Handle != 0)
            {
                Device.DestroyBuffer(buffer);
            }

            if (memory.Handle != 0)
            {
                Device.FreeMemory(memory);
            }
        }

        public static GraphicsBuffer CreateDynamic<T>(VkBufferUsageFlags bufferUsages, int count = 1) where T : struct
        {
            return Create(bufferUsages, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, Unsafe.SizeOf<T>(), count);
        }

        public static GraphicsBuffer CreateUniform<T>(int count = 1) where T : struct
        {
            return CreateDynamic<T>(VkBufferUsageFlags.UniformBuffer, count);
        }

        public static GraphicsBuffer Create<T>(VkBufferUsageFlags bufferUsages, T[] data, bool dynamic = false) where T : struct
        {
            return Create(bufferUsages, dynamic ? VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent : VkMemoryPropertyFlags.DeviceLocal, Unsafe.SizeOf<T>(), data.Length);
        }

        public static GraphicsBuffer Create(VkBufferUsageFlags usageFlags, bool dynamic, int stride, int count, void* data = null)
        {
            return Create(usageFlags, dynamic ? VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent : VkMemoryPropertyFlags.DeviceLocal, stride, count, data);
        }

        public static GraphicsBuffer Create(VkBufferUsageFlags usageFlags, VkMemoryPropertyFlags memoryPropertyFlags, int stride,  int count, void* data = null)
        {
            var buffer = new GraphicsBuffer
            {
                Stride = stride,
                Count = count,
            };

            Util.CheckResult(Device.createBuffer(usageFlags, memoryPropertyFlags, buffer, (ulong)(stride * count), data));
            return buffer;
        }

    }
}
