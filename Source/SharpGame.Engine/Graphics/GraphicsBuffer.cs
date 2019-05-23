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

        /** @brief Usage flags to be filled by external source at buffer creation (to query at some later point) */
        public BufferUsage usageFlags;
        /** @brief Memory propertys flags to be filled by external source at buffer creation (to query at some later point) */
        public VkMemoryPropertyFlags memoryPropertyFlags;

        public ref T Map<T>(int offset = 0) where T : struct
        {
            void* mapped = Device.MapMemory(memory, (ulong)offset, size, 0);
            return ref Unsafe.AsRef<T>(mapped);
        }

        public void* Map(VkDeviceSize offset = 0, VkDeviceSize size = WholeSize)
        {
            return Device.MapMemory(memory, offset, size, 0);
        }
               
        public void Unmap()
        {
            Device.UnmapMemory(memory);            
        }

        public void SetupDescriptor(VkDeviceSize size = WholeSize, VkDeviceSize offset = 0)
        {
            descriptor.offset = offset;
            descriptor.buffer = buffer;
            descriptor.range = size;
        }

        public void SetData<T>(ref T data, int offset = 0) where T : struct
        {
            SetData(Unsafe.AsPointer(ref data), (uint)offset, (uint)Unsafe.SizeOf<T>());
        }

        public void SetData(void* data, uint offset, uint size)
        {
            void* mapped = Map(offset, size);
            Unsafe.CopyBlock(mapped, data, (uint)size);
            Unmap();
        }

        public VkResult Flush(VkDeviceSize size = WholeSize, VkDeviceSize offset = 0)
        {
            VkMappedMemoryRange mappedRange = VkMappedMemoryRange.New();
            mappedRange.memory = memory;
            mappedRange.offset = offset;
            mappedRange.size = size;
            return vkFlushMappedMemoryRanges(Graphics.device, 1, &mappedRange);
        }

        public VkResult Invalidate(VkDeviceSize size = WholeSize, VkDeviceSize offset = 0)
        {
            VkMappedMemoryRange mappedRange = VkMappedMemoryRange.New();
            mappedRange.memory = memory;
            mappedRange.offset = offset;
            mappedRange.size = size;
            return vkInvalidateMappedMemoryRanges(Graphics.device, 1, &mappedRange);
        }

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

        public static GraphicsBuffer CreateDynamic<T>(BufferUsage bufferUsages, int count = 1) where T : struct
        {
            return Create(bufferUsages, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, Unsafe.SizeOf<T>(), count);
        }

        public static GraphicsBuffer CreateUniformBuffer<T>(int count = 1) where T : struct
        {
            return CreateDynamic<T>(BufferUsage.UniformBuffer, count);
        }

        public static GraphicsBuffer Create<T>(BufferUsage bufferUsages, T[] data, bool dynamic = false) where T : struct
        {
            return Create(bufferUsages, dynamic ? VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent : VkMemoryPropertyFlags.DeviceLocal, Unsafe.SizeOf<T>(), data.Length);
        }

        public static GraphicsBuffer Create(BufferUsage usageFlags, bool dynamic, int stride, int count, IntPtr data = default)
        {
            return Create(usageFlags, dynamic ? VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent : VkMemoryPropertyFlags.DeviceLocal, stride, count, (void*)data);
        }

        public static GraphicsBuffer Create(BufferUsage usageFlags, VkMemoryPropertyFlags memoryPropertyFlags, int stride,  int count, void* data = null)
        {
            GraphicsBuffer buffer = new GraphicsBuffer
            {
                Stride = stride,
                Count = count
            };

            ulong size = (ulong)(stride * count);

            // Create the buffer handle
            VkBufferCreateInfo bufferCreateInfo = VkBufferCreateInfo.New();
            bufferCreateInfo.usage = (VkBufferUsageFlags)usageFlags;
            bufferCreateInfo.size = size;

            if (data != null && (memoryPropertyFlags & VkMemoryPropertyFlags.HostCoherent) == 0)
            {
                bufferCreateInfo.usage |= VkBufferUsageFlags.TransferDst;
            }

            Util.CheckResult(vkCreateBuffer(Graphics.device, &bufferCreateInfo, null, out buffer.buffer));

            // Create the memory backing up the buffer handle
            VkMemoryRequirements memReqs;
            VkMemoryAllocateInfo memAlloc = VkMemoryAllocateInfo.New();
            vkGetBufferMemoryRequirements(Graphics.device, buffer.buffer, &memReqs);
            memAlloc.allocationSize = memReqs.size;
            // Find a memory type index that fits the properties of the buffer
            memAlloc.memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, memoryPropertyFlags);
            Util.CheckResult(vkAllocateMemory(Graphics.device, &memAlloc, null, out buffer.memory));

            buffer.alignment = memReqs.alignment;
            buffer.size = memAlloc.allocationSize;
            buffer.usageFlags = usageFlags;
            buffer.memoryPropertyFlags = memoryPropertyFlags;

            vkBindBufferMemory(Graphics.device, buffer.buffer, buffer.memory, 0);

            // If a pointer to the buffer data has been passed, map the buffer and copy over the data
            if (data != null)
            {
                if ((memoryPropertyFlags & VkMemoryPropertyFlags.HostCoherent) == 0)
                {
                    VkBuffer stagingBuffer;
                    VkDeviceMemory stagingMemory;

                    Util.CheckResult(Device.CreateBuffer(VkBufferUsageFlags.TransferSrc,
                        VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, size, &stagingBuffer, &stagingMemory, data));

                    // Copy from staging buffers
                    VkCommandBuffer copyCmd = Device.CreateCommandBuffer(VkCommandBufferLevel.Primary, true);
                    VkBufferCopy copyRegion = new VkBufferCopy();
                    copyRegion.size = size;
                    vkCmdCopyBuffer(copyCmd, stagingBuffer, buffer.buffer, 1, &copyRegion);

                    Device.FlushCommandBuffer(copyCmd, Graphics.queue, true);
                    Device.DestroyBuffer(stagingBuffer);
                    Device.FreeMemory(stagingMemory);
                }
                else
                {
                    var mapped = buffer.Map();
                    Unsafe.CopyBlock(mapped, data, (uint)size);
                    buffer.Unmap();
                }

            }

            // Initialize a default descriptor that covers the whole buffer size
            buffer.SetupDescriptor();

            // Attach the memory to the buffer object
            return buffer;
        }



    }
}
