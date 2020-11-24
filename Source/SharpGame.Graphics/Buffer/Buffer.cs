using VkDeviceSize = System.UInt64;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System;

namespace SharpGame
{
    using static Vulkan;
    public interface IBindableResource { }
    
    public class Buffer : RefCounted, IBindableResource
    {
        public const ulong WholeSize = ulong.MaxValue;
        public ulong Stride { get; set; }
        public ulong Count { get; set; }
        public ulong Size { get; set; }
        public VkBufferUsageFlags UsageFlags { get; set; }

        public IntPtr Mapped { get; private set; }

        internal VkDescriptorBufferInfo descriptor;
        internal VkMemoryPropertyFlags memoryPropertyFlags;

        Format viewFormat;

        internal VkBuffer buffer;
        internal VkDeviceMemory memory;

        public BufferView view;
        internal DescriptorImageInfo imageDescriptor;

        public Buffer()
        {
        }

        public Buffer(VkBufferUsageFlags usageFlags, VkMemoryPropertyFlags memoryPropertyFlags, ulong size, VkSharingMode sharingMode = VkSharingMode.Exclusive)
            : this(usageFlags, memoryPropertyFlags, size, 1, sharingMode)
        {
        }

        public Buffer(VkBufferUsageFlags usageFlags, VkMemoryPropertyFlags memoryPropFlags, ulong stride, ulong count,
            VkSharingMode sharingMode = VkSharingMode.Exclusive, uint[] queueFamilyIndices = null, IntPtr data = default)
        {
            Stride = stride;
            Count = count;

            ulong size = stride * count;

            // Create the buffer handle
            var bufferCreateInfo = new VkBufferCreateInfo(usageFlags, size, queueFamilyIndices);
            bufferCreateInfo.sharingMode = sharingMode;
            if (data != null && (memoryPropertyFlags & VkMemoryPropertyFlags.HostCoherent) == 0)
            {
                bufferCreateInfo.usage |= VkBufferUsageFlags.TransferDst;
            }

            buffer = Device.CreateBuffer(ref bufferCreateInfo);

            Device.GetBufferMemoryRequirements(buffer, out VkMemoryRequirements memReqs);

            // Find a memory type index that fits the properties of the buffer

            var memoryTypeIndex = Device.GetMemoryType(memReqs.memoryTypeBits, memoryPropFlags);

            var memAlloc = new VkMemoryAllocateInfo(memReqs.size, memoryTypeIndex);
            memory = Device.AllocateMemory(ref memAlloc);

            //buffer.alignment = memReqs.alignment;
            Size = memAlloc.allocationSize;
            UsageFlags = usageFlags;
            memoryPropertyFlags = memoryPropFlags;

            Device.BindBufferMemory(buffer, memory, 0);

            if (data != IntPtr.Zero)
            {
                SetData(data, 0, size);
            }

            SetupDescriptor();

        }

        public static Buffer CreateUniformBuffer<T>(ulong count = 1) where T : unmanaged
        {
            return Create<T>(VkBufferUsageFlags.UniformBuffer, true, count);
        }

        public static Buffer CreateStagingBuffer(ulong size, IntPtr data)
        {
            return new Buffer(VkBufferUsageFlags.TransferSrc, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, size, 1, VkSharingMode.Exclusive, null, data);
        }

        public static Buffer CreateTexelBuffer(VkBufferUsageFlags flags, ulong size, Format format, VkSharingMode sharingMode, uint[] queueFamilyIndices)
        {
            var buffer = new Buffer(VkBufferUsageFlags.StorageTexelBuffer | flags, VkMemoryPropertyFlags.DeviceLocal, size, 1, sharingMode, queueFamilyIndices);
            buffer.CreateView(format, 0, WholeSize);
            return buffer;
        }

        public static Buffer Create<T>(VkBufferUsageFlags bufferUsages, T[] data, bool dynamic = false) where T : struct
        {
            return Create(bufferUsages, dynamic, (ulong)Unsafe.SizeOf<T>(), (ulong)data.Length, Utilities.AsPointer(ref data[0]));
        }

        public static Buffer Create<T>(VkBufferUsageFlags bufferUsages, bool dynamic, ulong count = 1, IntPtr data = default) where T : unmanaged
        {
            return Create(bufferUsages, dynamic, (ulong)Unsafe.SizeOf<T>(), count, data);
        }

        public static Buffer Create(VkBufferUsageFlags usageFlags, bool dynamic, ulong stride, ulong count, IntPtr data = default)
        {
            return new Buffer(usageFlags, dynamic ? VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent : VkMemoryPropertyFlags.DeviceLocal, stride, count, VkSharingMode.Exclusive, null, data);
        }

        public ref T Map<T>(ulong offset = 0) where T : struct
        {
            Mapped = Device.MapMemory(memory, (ulong)offset, Size, VkMemoryMapFlags.None);
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

        public void SetData<T>(ref T data, uint offset = 0) where T : unmanaged
        {
            SetData(Utilities.AsPointer(ref data), (uint)offset, (uint)Unsafe.SizeOf<T>());
        }

        public void SetData(IntPtr data, ulong offset, ulong size)
        {
            if ((memoryPropertyFlags & VkMemoryPropertyFlags.HostCoherent) == 0)
            {
                using (Buffer stagingBuffer = CreateStagingBuffer(size, data))
                {
                    Graphics.WithCommandBuffer((cmd) =>
                    {
                        VkBufferCopy copyRegion = new VkBufferCopy { srcOffset = offset, size = size };
                        cmd.CopyBuffer(stagingBuffer, this, ref copyRegion);
                    });

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
            VkMappedMemoryRange mappedRange = new VkMappedMemoryRange
            {
                sType = VkStructureType.MappedMemoryRange
            };
            mappedRange.memory = memory;
            mappedRange.offset = offset;
            mappedRange.size = size;            
            Device.FlushMappedMemoryRanges(1, ref mappedRange);
        }

        public unsafe void Invalidate(ulong size = WholeSize, ulong offset = 0)
        {
            VkMappedMemoryRange mappedRange = new VkMappedMemoryRange
            {
                sType = VkStructureType.MappedMemoryRange
            };
            mappedRange.memory = memory;
            mappedRange.offset = offset;
            mappedRange.size = size;
            Device.InvalidateMappedMemoryRanges(1, ref mappedRange);
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


}
