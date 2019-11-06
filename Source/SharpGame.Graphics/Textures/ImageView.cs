﻿using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using global::System.Runtime.CompilerServices;

    public class ImageView : DisposeBase, IBindableResource
    {
        public Image Image { get; }
        public uint Width => Image.extent.width;
        public uint Height => Image.extent.height;

        public VkImageView handle;

        DescriptorImageInfo descriptor;

        internal ref DescriptorImageInfo Descriptor
        {
            get
            {
                descriptor = new DescriptorImageInfo(Sampler.ClampToEdge, this, ImageLayout.ShaderReadOnlyOptimal);
                return ref descriptor;
            }
        }

        public ImageView(ref ImageViewCreateInfo imageViewCreateInfo)
        {
            imageViewCreateInfo.ToNative(out VkImageViewCreateInfo native);
            handle = Device.CreateImageView(ref native);
            Image = imageViewCreateInfo.image;
        }

        protected override void Destroy(bool disposing)
        {
            Device.Destroy(handle);
        }

        public static ImageView Create(Image image, ImageViewType viewType, Format format, ImageAspectFlags aspectMask, uint baseMipLevel, uint numMipLevels, uint baseArrayLayer = 0, uint arrayLayers = 1)
        {
            ImageViewCreateInfo viewCreateInfo = new ImageViewCreateInfo
            {
                image = image,
                viewType = viewType,
                format = format,
                components = new ComponentMapping(ComponentSwizzle.R, ComponentSwizzle.G, ComponentSwizzle.B, ComponentSwizzle.A),

                subresourceRange = new ImageSubresourceRange
                {
                    aspectMask = aspectMask,
                    baseMipLevel = baseMipLevel,
                    levelCount = numMipLevels,
                    baseArrayLayer = baseArrayLayer,
                    layerCount = arrayLayers,
                }
            };

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