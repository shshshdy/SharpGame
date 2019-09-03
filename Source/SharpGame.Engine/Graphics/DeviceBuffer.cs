using Vulkan;
using VkDeviceSize = System.UInt64;
using static Vulkan.VulkanNative;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System;

namespace SharpGame
{
    public interface IBindableResource { }
    
    public class DeviceBuffer : RefCounted, IBindableResource
    {
        public ulong Stride { get; set; }
        public ulong Count { get; set; }
        public ulong Size { get; set; }
        public BufferUsageFlags UsageFlags { get; set; }

        public IntPtr Mapped { get; private set; }

        internal VkBuffer buffer;
        internal VkDeviceMemory memory;
        internal DescriptorBufferInfo descriptor;
        internal MemoryPropertyFlags memoryPropertyFlags;

        public DeviceBuffer()
        {
        }

        public DeviceBuffer(BufferUsageFlags usageFlags, MemoryPropertyFlags memoryPropertyFlags, ulong size)
            : this(usageFlags, memoryPropertyFlags, size, 1)
        {
        }

        public DeviceBuffer(BufferUsageFlags usageFlags, MemoryPropertyFlags memoryPropFlags, ulong stride, ulong count, IntPtr data = default)
        {
            Stride = stride;
            Count = count;

            ulong size = stride * count;

            // Create the buffer handle
            BufferCreateInfo bufferCreateInfo = new BufferCreateInfo(usageFlags, size);
            if (data != null && (memoryPropertyFlags & MemoryPropertyFlags.HostCoherent) == 0)
            {
                bufferCreateInfo.usage |= BufferUsageFlags.TransferDst;
            }

            buffer = Device.CreateBuffer(ref bufferCreateInfo.native);

            Device.GetBufferMemoryRequirements(buffer, out VkMemoryRequirements memReqs);

            // Find a memory type index that fits the properties of the buffer

            var memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, memoryPropFlags);

            MemoryAllocateInfo memAlloc = new MemoryAllocateInfo(memReqs.size, memoryTypeIndex);
            memory = Device.AllocateMemory(ref memAlloc.native);

            //buffer.alignment = memReqs.alignment;
            Size = memAlloc.allocationSize;
            UsageFlags = usageFlags;
            memoryPropertyFlags = memoryPropFlags;

            Device.BindBufferMemory(buffer, memory, 0);

            if (data != IntPtr.Zero)
            {
                SetData(data, 0, Size);
            }

            SetupDescriptor();
            
        }

        public static DeviceBuffer CreateUniformBuffer<T>(ulong count = 1) where T : struct
        {
            return Create<T>(BufferUsageFlags.UniformBuffer, true, count);
        }

        public static DeviceBuffer CreateStagingBuffer(ulong size, IntPtr data)
        {
            return new DeviceBuffer(BufferUsageFlags.TransferSrc, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent, size, 1, data);
        }

        public static DeviceBuffer Create<T>(BufferUsageFlags bufferUsages, T[] data, bool dynamic = false) where T : struct
        {
            return Create(bufferUsages, dynamic, (ulong)Unsafe.SizeOf<T>(), (ulong)data.Length, Utilities.AsPointer(ref data[0]));
        }

        public static DeviceBuffer Create<T>(BufferUsageFlags bufferUsages, bool dynamic, ulong count = 1, IntPtr data = default) where T : struct
        {
            return Create(bufferUsages, dynamic, (ulong)Unsafe.SizeOf<T>(), count, data);
        }

        public static DeviceBuffer Create(BufferUsageFlags usageFlags, bool dynamic, ulong stride, ulong count, IntPtr data = default)
        {
            return new DeviceBuffer(usageFlags, dynamic ? MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent : MemoryPropertyFlags.DeviceLocal, stride, count, data);
        }

        public ref T Map<T>(ulong offset = 0) where T : struct
        {
            Mapped = Device.MapMemory(memory, (ulong)offset, Size, 0);
            return ref Utilities.As<T>(Mapped);
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
            SetData(Utilities.AsPointer(ref data), (uint)offset, (uint)Unsafe.SizeOf<T>());
        }

        public void SetData(IntPtr data, ulong offset, ulong size)
        {
            if ((memoryPropertyFlags & MemoryPropertyFlags.HostCoherent) == 0)
            {
                using (DeviceBuffer stagingBuffer = CreateStagingBuffer(size, data))
                {
                    CommandBuffer copyCmd = Graphics.CreateCommandBuffer(CommandBufferLevel.Primary, true);
                    BufferCopy copyRegion = new BufferCopy { srcOffset = offset, size = size };
                    copyCmd.CopyBuffer(stagingBuffer, this, ref copyRegion);
                    Graphics.FlushCommandBuffer(copyCmd, Graphics.GraphicsQueue, true);
                }
            }
            else
            {
                IntPtr mapped = Map(offset, size);
                Utilities.CopyBlock(mapped, data, (int)size);
                Unmap();
            }

        }

        public unsafe void Flush(ulong size = WholeSize, ulong offset = 0)
        {
            VkMappedMemoryRange mappedRange = VkMappedMemoryRange.New();
            mappedRange.memory = memory;
            mappedRange.offset = offset;
            mappedRange.size = size;            
            VulkanUtil.CheckResult(vkFlushMappedMemoryRanges(Graphics.device, 1, &mappedRange));
        }

        public unsafe void Invalidate(ulong size = WholeSize, ulong offset = 0)
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
