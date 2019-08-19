using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using static Vulkan.VulkanNative;

    public class ImageView : DisposeBase
    {
        public VkImageView handle;

        public ImageView(ref ImageViewCreateInfo imageViewCreateInfo)
        {
            imageViewCreateInfo.ToNative(out VkImageViewCreateInfo native);
            handle = Device.CreateImageView(ref native);
        }

        protected override void Destroy()
        {
            Device.Destroy(handle);
        }


        public static ImageView Create(Texture texture, Format format, ImageAspectFlags aspectMask, uint baseMipLevel, uint numMipLevels)
        {
            ImageViewCreateInfo viewCreateInfo = new ImageViewCreateInfo
            {
                image = texture.image,
                viewType = (texture.layers == 6) ? ImageViewType.ImageCube : ImageViewType.Image2D,
                format = format
            };

            viewCreateInfo.subresourceRange.aspectMask = (VkImageAspectFlags)aspectMask;
            viewCreateInfo.subresourceRange.baseMipLevel = baseMipLevel;
            viewCreateInfo.subresourceRange.levelCount = numMipLevels;
            viewCreateInfo.subresourceRange.baseArrayLayer = 0;
            viewCreateInfo.subresourceRange.layerCount = RemainingArrayLayers;
            return new ImageView(ref viewCreateInfo);
        }
    }

    public struct ComponentMapping
    {
        public ComponentSwizzle r;
        public ComponentSwizzle g;
        public ComponentSwizzle b;
        public ComponentSwizzle a;

        public ComponentMapping(ComponentSwizzle r, ComponentSwizzle g, ComponentSwizzle b, ComponentSwizzle a)
        {
            this.r = r; this.g = g; this.b = b; this.a = a;
        }
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
