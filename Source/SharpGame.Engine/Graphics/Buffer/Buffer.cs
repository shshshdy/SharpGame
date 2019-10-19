using Vulkan;
using VkDeviceSize = System.UInt64;
using static Vulkan.VulkanNative;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System;

namespace SharpGame
{
    public interface IBindableResource { }
    
    public class Buffer : RefCounted, IBindableResource
    {
        public const ulong WholeSize = ulong.MaxValue;
        public ulong Stride { get; set; }
        public ulong Count { get; set; }
        public ulong Size { get; set; }
        public BufferUsageFlags UsageFlags { get; set; }

        public IntPtr Mapped { get; private set; }

        internal DescriptorBufferInfo descriptor;
        internal MemoryPropertyFlags memoryPropertyFlags;

        Format viewFormat;

        internal VkBuffer buffer;
        internal VkDeviceMemory memory;

        public BufferView view;
        internal DescriptorImageInfo imageDescriptor;

        public Buffer()
        {
        }

        public Buffer(BufferUsageFlags usageFlags, MemoryPropertyFlags memoryPropertyFlags, ulong size, SharingMode sharingMode = SharingMode.Exclusive)
            : this(usageFlags, memoryPropertyFlags, size, 1, sharingMode)
        {
        }

        public Buffer(BufferUsageFlags usageFlags, MemoryPropertyFlags memoryPropFlags, ulong stride, ulong count,
            SharingMode sharingMode = SharingMode.Exclusive, uint[] queueFamilyIndices = null, IntPtr data = default)
        {
            Stride = stride;
            Count = count;

            ulong size = stride * count;

            // Create the buffer handle
            BufferCreateInfo bufferCreateInfo = new BufferCreateInfo(usageFlags, size, queueFamilyIndices);
            bufferCreateInfo.sharingMode = sharingMode;
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

        public static Buffer CreateUniformBuffer<T>(ulong count = 1) where T : struct
        {
            return Create<T>(BufferUsageFlags.UniformBuffer, true, count);
        }

        public static Buffer CreateStagingBuffer(ulong size, IntPtr data)
        {
            return new Buffer(BufferUsageFlags.TransferSrc, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent, size, 1, SharingMode.Exclusive, null, data);
        }

        public static Buffer CreateTexelBuffer(BufferUsageFlags flags, ulong size, Format format, SharingMode sharingMode, uint[] queueFamilyIndices)
        {
            var buffer = new Buffer(BufferUsageFlags.StorageTexelBuffer | flags, MemoryPropertyFlags.DeviceLocal, size, 1, sharingMode, queueFamilyIndices);
            buffer.CreateView(format, 0, WholeSize);
            return buffer;
        }

        public static Buffer Create<T>(BufferUsageFlags bufferUsages, T[] data, bool dynamic = false) where T : struct
        {
            return Create(bufferUsages, dynamic, (ulong)Unsafe.SizeOf<T>(), (ulong)data.Length, Utilities.AsPointer(ref data[0]));
        }

        public static Buffer Create<T>(BufferUsageFlags bufferUsages, bool dynamic, ulong count = 1, IntPtr data = default) where T : struct
        {
            return Create(bufferUsages, dynamic, (ulong)Unsafe.SizeOf<T>(), count, data);
        }

        public static Buffer Create(BufferUsageFlags usageFlags, bool dynamic, ulong stride, ulong count, IntPtr data = default)
        {
            return new Buffer(usageFlags, dynamic ? MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent : MemoryPropertyFlags.DeviceLocal, stride, count, SharingMode.Exclusive, null, data);
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

        public void SetupDescriptor()
        {
            descriptor.offset = 0;
            descriptor.buffer = buffer;
            descriptor.range = WholeSize;// Size;
        }

        public void CreateView(Format format, ulong offset, ulong range)
        {
            view = new BufferView(this, format, offset, range);
        }

        public void SetData<T>(ref T data, uint offset = 0) where T : struct
        {
            SetData(Utilities.AsPointer(ref data), (uint)offset, (uint)Unsafe.SizeOf<T>());
        }

        public void SetData(IntPtr data, ulong offset, ulong size)
        {
            if ((memoryPropertyFlags & MemoryPropertyFlags.HostCoherent) == 0)
            {
                using (Buffer stagingBuffer = CreateStagingBuffer(size, data))
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

            view?.Dispose();
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

        public unsafe BufferCreateInfo(BufferUsageFlags usage, ulong size, uint[] queueFamilyIndices = null)
        {
            native = VkBufferCreateInfo.New();
            pQueueFamilyIndices = queueFamilyIndices;
            if(queueFamilyIndices != null)
            {
                native.queueFamilyIndexCount = (uint)queueFamilyIndices.Length;
                native.pQueueFamilyIndices = (uint*)Unsafe.AsPointer(ref pQueueFamilyIndices[0]);
            }
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
