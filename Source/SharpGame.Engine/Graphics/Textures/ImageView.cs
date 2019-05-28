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

    public struct ComponentMapping
    {
        public ComponentSwizzle r;
        public ComponentSwizzle g;
        public ComponentSwizzle b;
        public ComponentSwizzle a;
    }

    public struct ImageViewCreateInfo
    {
        public uint flags;
        public Image image;
        public ImageViewType viewType;
        public Format format;
        public ComponentMapping components;
        public VkImageSubresourceRange subresourceRange;

        internal void ToNative(out VkImageViewCreateInfo native)
        {
            native = VkImageViewCreateInfo.New();
            native.flags = flags;
            native.image = image.handle;
            native.viewType = (VkImageViewType)viewType;
            native.format = (VkFormat)format;
            native.components = new VkComponentMapping { r = (VkComponentSwizzle)components.r, g = (VkComponentSwizzle)components.g, b = (VkComponentSwizzle)components.b, a = (VkComponentSwizzle)components.a };
            native.subresourceRange = subresourceRange;
        }
    }


}
