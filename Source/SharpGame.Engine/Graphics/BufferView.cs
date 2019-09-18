using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class BufferView : DisposeBase
    {
        internal VkBufferView view;

        public BufferView(Buffer buffer, Format format, ulong offset, ulong range)
        {
            var bufferViewCreateInfo = new BufferViewCreateInfo(buffer, format, offset, range);
            view = Device.CreateBufferView(ref bufferViewCreateInfo.native);
        }

        public BufferView(ref BufferViewCreateInfo bufferViewCreateInfo)
        {
            view = Device.CreateBufferView(ref bufferViewCreateInfo.native);
        }

        protected override void Destroy(bool disposing)
        {
            base.Destroy(disposing);

            Device.DestroyBufferView(view);
        }
    }

    public ref struct BufferViewCreateInfo
    {
        public Buffer buffer { set => native.buffer = value.buffer; }
        public Format format { get => (Format)native.format; set => native.format = (VkFormat)value; }

        public ulong offset { get => native.offset; set => native.offset = value; }
        public ulong range { get => native.range; set => native.range = value; }

        internal VkBufferViewCreateInfo native;

        public BufferViewCreateInfo(Buffer buffer, Format format, ulong offset, ulong range)
        {
            native = VkBufferViewCreateInfo.New();
            this.buffer = buffer;
            this.format = format;
            this.offset = offset;
            this.range = range;
        }

    }
}
