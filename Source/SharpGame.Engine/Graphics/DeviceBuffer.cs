﻿using Vulkan;
using VkDeviceSize = System.UInt64;
using static Vulkan.VulkanNative;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System;

namespace SharpGame
{
    public interface IBindableResource { }
    
    public unsafe class DeviceBuffer : RefCounted, IBindableResource
    {
        public int Stride { get; set; }
        public int Count { get; set; }
        public int Size => Stride * Count;

        /** @brief Usage flags to be filled by external source at buffer creation (to query at some later point) */
        public BufferUsageFlags usageFlags;

        internal VkBuffer buffer;
        internal VkDeviceMemory memory;
        internal VkDescriptorBufferInfo descriptor;

        internal ulong size = 0;
        internal ulong alignment = 0;

        /** @brief Memory propertys flags to be filled by external source at buffer creation (to query at some later point) */
        internal VkMemoryPropertyFlags memoryPropertyFlags;

        public ref T Map<T>(int offset = 0) where T : struct
        {
            void* mapped = Device.MapMemory(memory, (ulong)offset, size, 0);
            return ref Unsafe.AsRef<T>(mapped);
        }

        public void* Map(ulong offset = 0, ulong size = WholeSize)
        {
            return Device.MapMemory(memory, offset, size, 0);
        }
               
        public void Unmap()
        {
            Device.UnmapMemory(memory);            
        }

        public void SetupDescriptor(ulong size = WholeSize, ulong offset = 0)
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

        public VkResult Flush(ulong size = WholeSize, ulong offset = 0)
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

        public static DeviceBuffer CreateDynamic<T>(BufferUsageFlags bufferUsages, int count = 1) where T : struct
        {
            return Create(bufferUsages, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, Unsafe.SizeOf<T>(), count);
        }

        public static DeviceBuffer CreateUniformBuffer<T>(int count = 1) where T : struct
        {
            return CreateDynamic<T>(BufferUsageFlags.UniformBuffer, count);
        }

        public static DeviceBuffer Create<T>(BufferUsageFlags bufferUsages, T[] data, bool dynamic = false) where T : struct
        {
            return Create(bufferUsages, dynamic,  Unsafe.SizeOf<T>(), data.Length, Utilities.AsPointer(ref data[0]));
        }

        public static DeviceBuffer Create(BufferUsageFlags usageFlags, bool dynamic, int stride, int count, IntPtr data = default)
        {
            return Create(usageFlags, dynamic ? VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent : VkMemoryPropertyFlags.DeviceLocal, stride, count, (void*)data);
        }

        public static DeviceBuffer Create(BufferUsageFlags usageFlags, VkMemoryPropertyFlags memoryPropertyFlags, int stride,  int count, void* data = null)
        {
            DeviceBuffer buffer = new DeviceBuffer
            {
                Stride = stride,
                Count = count
            };

            ulong size = (ulong)(stride * count);

            // Create the buffer handle
            BufferCreateInfo bufferCreateInfo = new BufferCreateInfo(usageFlags, size);
            if (data != null && (memoryPropertyFlags & VkMemoryPropertyFlags.HostCoherent) == 0)
            {
                bufferCreateInfo.usage |= BufferUsageFlags.TransferDst;
            }

            buffer.buffer = Device.CreateBuffer(ref bufferCreateInfo.native);
            Device.GetBufferMemoryRequirements(buffer.buffer, out VkMemoryRequirements memReqs);

            // Find a memory type index that fits the properties of the buffer
            var memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, memoryPropertyFlags);

            // Create the memory backing up the buffer handle
            MemoryAllocateInfo memAlloc = new MemoryAllocateInfo(memReqs.size, memoryTypeIndex);
            
            buffer.memory = Device.AllocateMemory(ref memAlloc.native);

            buffer.alignment = memReqs.alignment;
            buffer.size = memAlloc.allocationSize;
            buffer.usageFlags = usageFlags;
            buffer.memoryPropertyFlags = memoryPropertyFlags;

            Device.BindBufferMemory(buffer.buffer, buffer.memory, 0);

            // If a pointer to the buffer data has been passed, map the buffer and copy over the data
            if (data != null)
            {
                if ((memoryPropertyFlags & VkMemoryPropertyFlags.HostCoherent) == 0)
                {
                    VkBuffer stagingBuffer;
                    VkDeviceMemory stagingMemory;

                    Device.CreateBuffer(VkBufferUsageFlags.TransferSrc, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                        size, &stagingBuffer, &stagingMemory, data);

                    // Copy from staging buffers
                    VkCommandBuffer copyCmd = Device.CreateCommandBuffer(VkCommandBufferLevel.Primary, true);
                    VkBufferCopy copyRegion = new VkBufferCopy { size = size };
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


    public ref struct BufferCreateInfo
    {
        public BufferUsageFlags usage { get => (BufferUsageFlags)native.usage; set => native.usage = (VkBufferUsageFlags)value; }
        public ulong size { get => native.size; set => native.size = value; }
        public SharingMode sharingMode { get => (SharingMode)native.sharingMode; set => native.sharingMode = (VkSharingMode)value; }
        //public uint[] pQueueFamilyIndices { get => native.flags; set => native.flags = value; }
        public uint[] pQueueFamilyIndices;
        public VkBufferCreateFlags flags { get => native.flags; set => native.flags = value; }

        internal VkBufferCreateInfo native;

        public BufferCreateInfo(BufferUsageFlags usage, ulong size)
        {
            native = VkBufferCreateInfo.New();
            pQueueFamilyIndices = null;
            this.usage = usage;
            this.size = size;
        }
    }

    public ref struct MemoryAllocateInfo
    {
        public ulong allocationSize => native.allocationSize;
        public uint memoryTypeIndex => native.memoryTypeIndex;

        internal VkMemoryAllocateInfo native;

        public MemoryAllocateInfo(ulong allocationSize, uint memoryTypeIndex)
        {
            native = VkMemoryAllocateInfo.New();
            native.allocationSize = allocationSize;
            native.memoryTypeIndex = memoryTypeIndex;
        }
    }
}
