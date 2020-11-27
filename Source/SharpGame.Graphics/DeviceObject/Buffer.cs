using VkDeviceSize = System.UInt64;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System;

namespace SharpGame
{
    using static Vulkan;
    public interface IBindableResource { }
    
    public class Buffer : DeviceMemory, IBindableResource
    {
        public ulong Stride { get; set; }
        public VkBufferUsageFlags UsageFlags { get; set; }

        public VkBuffer handle;
        public BufferView view;

        internal VkDescriptorBufferInfo descriptor;


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

            handle = Device.CreateBuffer(ref bufferCreateInfo);

            UsageFlags = usageFlags;
            memoryPropertyFlags = memoryPropFlags;

            Device.GetBufferMemoryRequirements(handle, out VkMemoryRequirements memReqs);

            Allocate(memReqs);
            Device.BindBufferMemory(handle, memory, 0);

            Size = allocationSize;

            if (data != IntPtr.Zero)
            {
                SetData(data, 0, size);
            }

            SetupDescriptor();

        }

        public static implicit operator VkBuffer(Buffer buffer) => buffer.handle;

        public static Buffer CreateUniformBuffer<T>(ulong count = 1) where T : unmanaged
        {
            return Create<T>(VkBufferUsageFlags.UniformBuffer, true, count);
        }

        public static Buffer CreateStagingBuffer(ulong size, IntPtr data)
        {
            return new Buffer(VkBufferUsageFlags.TransferSrc, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, size, 1, VkSharingMode.Exclusive, null, data);
        }

        public static Buffer CreateTexelBuffer(VkBufferUsageFlags flags, ulong size, VkFormat format, VkSharingMode sharingMode, uint[] queueFamilyIndices)
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

        public void SetupDescriptor()
        {
            descriptor.offset = 0;
            descriptor.buffer = handle;
            descriptor.range = WholeSize;// Size;
        }

        public void CreateView(VkFormat format, ulong offset, ulong range)
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
                using Buffer stagingBuffer = CreateStagingBuffer(size, data);

                Graphics.WithCommandBuffer((cmd) =>
                {
                    VkBufferCopy copyRegion = new VkBufferCopy { srcOffset = offset, size = size };
                    cmd.CopyBuffer(stagingBuffer, this, ref copyRegion);
                });
            }
            else
            {
                IntPtr mapped = Map(offset, size);
                Utilities.CopyBlock(mapped, data, (int)size);
                Unmap();
            }

        }
        
        protected override void Destroy()
        {
            if (handle != 0)
            {
                Device.DestroyBuffer(handle);
            }

            view?.Dispose();

            base.Destroy();
        }


    }

    public class BufferView : DisposeBase
    {
        internal VkBufferView handle;

        public BufferView(Buffer buffer, VkFormat format, ulong offset, ulong range)
        {
            var bufferViewCreateInfo = new VkBufferViewCreateInfo
            {
                sType = VkStructureType.BufferViewCreateInfo,
                buffer = buffer.handle,
                format = (VkFormat)format,
                offset = offset,
                range = range
            };
            handle = Device.CreateBufferView(ref bufferViewCreateInfo);
        }

        protected override void Destroy(bool disposing)
        {
            base.Destroy(disposing);

            Device.DestroyBufferView(handle);
        }
    }
}
