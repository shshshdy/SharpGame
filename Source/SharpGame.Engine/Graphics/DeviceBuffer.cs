using Vulkan;
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
        public ulong Stride { get; set; }
        public ulong Count { get; set; }
        public ulong Size { get; set; }

        public IntPtr Mapped;

        /** @brief Usage flags to be filled by external source at buffer creation (to query at some later point) */
        public BufferUsageFlags usageFlags;

        internal VkBuffer buffer;
        internal VkDeviceMemory memory;

        internal DescriptorBufferInfo descriptor;

        /** @brief Memory propertys flags to be filled by external source at buffer creation (to query at some later point) */
        internal MemoryPropertyFlags memoryPropertyFlags;

        public DeviceBuffer()
        {
        }

        public DeviceBuffer(ref BufferCreateInfo createInfo, MemoryPropertyFlags memoryPropertyFlags)
        {
            buffer = Device.CreateBuffer(ref createInfo.native);

            Device.GetBufferMemoryRequirements(buffer, out VkMemoryRequirements memReqs);

            // Find a memory type index that fits the properties of the buffer
            var memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, (VkMemoryPropertyFlags)memoryPropertyFlags);

            // Create the memory backing up the buffer handle
            MemoryAllocateInfo memAlloc = new MemoryAllocateInfo(memReqs.size, memoryTypeIndex);
            memory = Device.AllocateMemory(ref memAlloc.native);
            //buffer.alignment = memReqs.alignment;
            Size = memAlloc.allocationSize;
            usageFlags = createInfo.usage;
            this.memoryPropertyFlags = memoryPropertyFlags;

            Device.BindBufferMemory(buffer, memory, 0);

        }

        public ref T Map<T>(ulong offset = 0) where T : struct
        {
            Mapped = Device.MapMemory(memory, (ulong)offset, Size, 0);
            return ref Unsafe.AsRef<T>((void*)Mapped);
        }

        public IntPtr Map(ulong offset = 0, ulong size = WholeSize)
        {
            Mapped = Device.MapMemory(memory, offset, size, 0);
            return Mapped;
        }
               
        public void Unmap()
        {
            Device.UnmapMemory(memory);
            Mapped = IntPtr.Zero;
        }

        public void SetupDescriptor(ulong size = WholeSize, ulong offset = 0)
        {
            descriptor.offset = offset;
            descriptor.buffer = buffer;
            descriptor.range = size;
        }

        public void SetData<T>(ref T data, uint offset = 0) where T : struct
        {
            SetData(Unsafe.AsPointer(ref data), (uint)offset, (uint)Unsafe.SizeOf<T>());
        }

        public void SetData(void* data, uint offset, uint size)
        {
            IntPtr mapped = Map(offset, size);
            Unsafe.CopyBlock((void*)mapped, data, (uint)size);
            Unmap();
        }

        public void Flush(ulong size = WholeSize, ulong offset = 0)
        {
            VkMappedMemoryRange mappedRange = VkMappedMemoryRange.New();
            mappedRange.memory = memory;
            mappedRange.offset = offset;
            mappedRange.size = size;            
            VulkanUtil.CheckResult(vkFlushMappedMemoryRanges(Graphics.device, 1, &mappedRange));
        }

        public void Invalidate(ulong size = WholeSize, ulong offset = 0)
        {
            VkMappedMemoryRange mappedRange = VkMappedMemoryRange.New();
            mappedRange.memory = memory;
            mappedRange.offset = offset;
            mappedRange.size = size;
            VulkanUtil.CheckResult(vkInvalidateMappedMemoryRanges(Graphics.device, 1, &mappedRange));
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

        public static DeviceBuffer CreateDynamic<T>(BufferUsageFlags bufferUsages, ulong count = 1) where T : struct
        {
            return Create(bufferUsages, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent, (ulong)Unsafe.SizeOf<T>(), count);
        }

        public static DeviceBuffer CreateUniformBuffer<T>(ulong count = 1) where T : struct
        {
            return CreateDynamic<T>(BufferUsageFlags.UniformBuffer, count);
        }

        public static DeviceBuffer Create<T>(BufferUsageFlags bufferUsages, T[] data, bool dynamic = false) where T : struct
        {
            return Create(bufferUsages, dynamic,  (ulong)Unsafe.SizeOf<T>(), (ulong)data.Length, Utilities.AsPointer(ref data[0]));
        }

        public static DeviceBuffer Create(BufferUsageFlags usageFlags, bool dynamic, ulong stride, ulong count, IntPtr data = default)
        {
            return Create(usageFlags, dynamic ? MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent : MemoryPropertyFlags.DeviceLocal, stride, count, (void*)data);
        }

        public static DeviceBuffer Create(BufferUsageFlags usageFlags, MemoryPropertyFlags memoryPropertyFlags, ulong size)
        {
            return Create(usageFlags, memoryPropertyFlags, size, 1, null);
        }

        public static DeviceBuffer CreateStagingBuffer(ulong size, void * data)
        {
            return Create(BufferUsageFlags.TransferSrc, MemoryPropertyFlags.HostVisible
                | MemoryPropertyFlags.HostCoherent, size, 1, data);
        }

        public static DeviceBuffer Create(BufferUsageFlags usageFlags, MemoryPropertyFlags memoryPropertyFlags, ulong stride, ulong count, void* data = null)
        {
            ulong size = stride * count;

            // Create the buffer handle
            BufferCreateInfo bufferCreateInfo = new BufferCreateInfo(usageFlags, size);
            if (data != null && (memoryPropertyFlags & MemoryPropertyFlags.HostCoherent) == 0)
            {
                bufferCreateInfo.usage |= BufferUsageFlags.TransferDst;
            }

            DeviceBuffer buffer = new DeviceBuffer(ref bufferCreateInfo, memoryPropertyFlags)
            {
                Stride = stride,
                Count = count,
                Size = size
            };

            // If a pointer to the buffer data has been passed, map the buffer and copy over the data
            if (data != null)
            {
                if ((memoryPropertyFlags & MemoryPropertyFlags.HostCoherent) == 0)
                {
                    using (DeviceBuffer stagingBuffer = CreateStagingBuffer(size, data))
                    {
                        CommandBuffer copyCmd = Graphics.CreateCommandBuffer(CommandBufferLevel.Primary, true);
                        BufferCopy copyRegion = new BufferCopy { size = size };
                        copyCmd.CopyBuffer(stagingBuffer, buffer, ref copyRegion);
                        Graphics.FlushCommandBuffer(copyCmd, Graphics.GraphicsQueue, true);
                        stagingBuffer.Dispose();
                    }

                }
                else
                {
                    var mapped = buffer.Map();
                    Unsafe.CopyBlock((void*)mapped, data, (uint)size);
                    buffer.Unmap();
                }

            }

            // Initialize a default descriptor that covers the whole buffer size
            buffer.SetupDescriptor();
            return buffer;
        }

    }

    public ref struct BufferCreateInfo
    {
        public BufferUsageFlags usage { get => (BufferUsageFlags)native.usage; set => native.usage = (VkBufferUsageFlags)value; }
        public ulong size { get => native.size; set => native.size = value; }
        public SharingMode sharingMode { get => (SharingMode)native.sharingMode; set => native.sharingMode = (VkSharingMode)value; }
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
