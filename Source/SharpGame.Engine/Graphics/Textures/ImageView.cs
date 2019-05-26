using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class ImageView : DisposeBase
    {
        internal VkImageView handle;
        public ImageView(ref ImageViewCreateInfo imageViewCreateInfo)
        {
            imageViewCreateInfo.ToNative(out VkImageViewCreateInfo native);
            handle = Device.CreateImageView(ref native);
        }

        protected override void Destroy()
        {
            Device.Destroy(handle);
        }
    }

    public struct ImageViewCreateInfo
    {
        public uint flags;
        public Image image;
        public VkImageViewType viewType;
        public Format format;
        public VkComponentMapping components;
        public VkImageSubresourceRange subresourceRange;

        internal void ToNative(out VkImageViewCreateInfo native)
        {
            native = VkImageViewCreateInfo.New();
            native.flags = flags;
            native.image = image.handle;
            native.viewType = viewType;
            native.format = (VkFormat)format;
            native.components = components;
            native.subresourceRange = subresourceRange;            
        }
    }
}
