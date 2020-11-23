using System;
using System.Collections.Generic;
using System.Text;


namespace SharpGame
{
    public class BufferView : DisposeBase
    {
        internal VkBufferView view;

        public BufferView(Buffer buffer, Format format, ulong offset, ulong range)
        {
            var bufferViewCreateInfo = new VkBufferViewCreateInfo
            {
                sType = VkStructureType.BufferViewCreateInfo
            };
            bufferViewCreateInfo.buffer = buffer.buffer;
            bufferViewCreateInfo.format = (VkFormat)format;
            bufferViewCreateInfo.offset = offset;
            bufferViewCreateInfo.range = range;
            view = Device.CreateBufferView(ref bufferViewCreateInfo);
        }

        protected override void Destroy(bool disposing)
        {
            base.Destroy(disposing);

            Device.DestroyBufferView(view);
        }
    }

}
