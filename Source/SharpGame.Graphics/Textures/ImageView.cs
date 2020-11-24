using System;
using System.Collections.Generic;
using System.Text;


namespace SharpGame
{
    using global::System.Runtime.CompilerServices;

    public class ImageView : DisposeBase, IBindableResource
    {
        public Image Image { get; }
        public uint Width => Image.extent.width;
        public uint Height => Image.extent.height;

        public VkImageView handle;

        public DescriptorImageInfo descriptor;

        public ImageView(ref ImageViewCreateInfo imageViewCreateInfo)
        {
            imageViewCreateInfo.ToNative(out VkImageViewCreateInfo native);
            handle = Device.CreateImageView(ref native);
            Image = imageViewCreateInfo.image;
            descriptor = new DescriptorImageInfo(Sampler.ClampToEdge, this, ImageLayout.ShaderReadOnlyOptimal);
        }

        protected override void Destroy(bool disposing)
        {
            Device.Destroy(handle);
        }

        public static ImageView Create(Image image, ImageViewType viewType, Format format, VkImageAspectFlags aspectMask, uint baseMipLevel, uint numMipLevels, uint baseArrayLayer = 0, uint arrayLayers = 1)
        {
            ImageViewCreateInfo viewCreateInfo = new ImageViewCreateInfo
            {
                image = image,
                viewType = viewType,
                format = format,
                components = new VkComponentMapping(VkComponentSwizzle.R, VkComponentSwizzle.G, VkComponentSwizzle.B, VkComponentSwizzle.A),

                subresourceRange = new VkImageSubresourceRange
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

    public struct ImageViewCreateInfo
    {
        public VkImageViewCreateFlags flags;
        public Image image;
        public ImageViewType viewType;
        public Format format;
        public VkComponentMapping components;
        public VkImageSubresourceRange subresourceRange;

        internal void ToNative(out VkImageViewCreateInfo native)
        {
            native = new VkImageViewCreateInfo
            {
                sType = VkStructureType.ImageViewCreateInfo
            };
            native.flags = flags;
            native.image = image.handle;
            native.viewType = (VkImageViewType)viewType;
            native.format = (VkFormat)format;
            native.components = new VkComponentMapping { r = components.r, g = components.g, b = components.b, a = components.a };
            native.subresourceRange = subresourceRange;
        }
    }


}
