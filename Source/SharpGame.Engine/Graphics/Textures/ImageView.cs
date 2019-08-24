using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using global::System.Runtime.CompilerServices;
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
                format = format,
                components = new ComponentMapping(ComponentSwizzle.R, ComponentSwizzle.G, ComponentSwizzle.B, ComponentSwizzle.A),
            };

            viewCreateInfo.subresourceRange.aspectMask = aspectMask;
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

    public struct ImageSubresourceRange
    {
        public ImageAspectFlags aspectMask;
        public uint baseMipLevel;
        public uint levelCount;
        public uint baseArrayLayer;
        public uint layerCount;

        public ImageSubresourceRange(ImageAspectFlags aspectMask, uint baseMipLevel = 0, uint levelCount = 1, uint baseArrayLayer = 0, uint layerCount = 1)
        {
            this.aspectMask = aspectMask;
            this.baseMipLevel = baseMipLevel;
            this.levelCount = levelCount;
            this.baseArrayLayer = baseArrayLayer;
            this.layerCount = layerCount;
        }
    }

    public struct ImageViewCreateInfo
    {
        public uint flags;
        public Image image;
        public ImageViewType viewType;
        public Format format;
        public ComponentMapping components;
        public ImageSubresourceRange subresourceRange;

        internal void ToNative(out VkImageViewCreateInfo native)
        {
            native = VkImageViewCreateInfo.New();
            native.flags = flags;
            native.image = image.handle;
            native.viewType = (VkImageViewType)viewType;
            native.format = (VkFormat)format;
            native.components = new VkComponentMapping { r = (VkComponentSwizzle)components.r, g = (VkComponentSwizzle)components.g, b = (VkComponentSwizzle)components.b, a = (VkComponentSwizzle)components.a };
            native.subresourceRange = Unsafe.As<ImageSubresourceRange, VkImageSubresourceRange>(ref subresourceRange);
        }
    }


}
