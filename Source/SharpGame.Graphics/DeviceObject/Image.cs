using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;


namespace SharpGame
{
    public class Image : DeviceMemory
    {
        public VkImage handle;
        public VkImageType imageType;
        public VkFormat format;
        public VkExtent3D extent;
        public uint mipLevels;
        public uint arrayLayers;

        public Image(VkImage handle)
        {
            this.handle = handle;
        }

        public unsafe Image(ref VkImageCreateInfo imageCreateInfo)
        {            
            handle = Device.CreateImage(ref imageCreateInfo);
            imageType = imageCreateInfo.imageType;
            format = imageCreateInfo.format;
            extent = imageCreateInfo.extent;
            mipLevels = imageCreateInfo.mipLevels;
            arrayLayers = imageCreateInfo.arrayLayers;

            Device.GetImageMemoryRequirements(this, out var memReqs);

            Allocate(memReqs);

            Device.BindImageMemory(handle, memory, 0);
        }

        protected override void Destroy()
        {
            //Donot destroy swapchain image 
            if(memory != VkDeviceMemory.Null)
                Device.Destroy(handle);

            base.Destroy();
        }

        public unsafe static Image Create(uint width, uint height, VkImageCreateFlags flags, uint layers, uint levels,
            VkFormat format, VkSampleCountFlags samples, VkImageUsageFlags usage)
        {
            var imageType = height == 1 ? width > 1 ? VkImageType.Image1D : VkImageType.Image2D : VkImageType.Image2D;
            var createInfo = new VkImageCreateInfo
            {
                sType = VkStructureType.ImageCreateInfo,
                flags = flags,
                imageType = imageType,
                format = format,
                extent = new VkExtent3D(width, height, 1),
                mipLevels = levels,
                arrayLayers = layers,
                samples = samples,
                tiling = VkImageTiling.Optimal,
                usage = usage,
                sharingMode = VkSharingMode.Exclusive,
                initialLayout = VkImageLayout.Undefined
            };

            Image image = new Image(ref createInfo);
            return image;
        }


    }

    public class ImageView : DisposeBase, IBindableResource
    {
        public Image Image { get; }
        public uint Width => Image.extent.width;
        public uint Height => Image.extent.height;

        public VkImageView handle;

        public DescriptorImageInfo descriptor;

        public ImageView(Image image, ref VkImageViewCreateInfo imageViewCreateInfo)
        {
            handle = Device.CreateImageView(ref imageViewCreateInfo);
            Image = image;
            descriptor = new DescriptorImageInfo(Sampler.ClampToEdge, this, VkImageLayout.ShaderReadOnlyOptimal);
        }

        protected override void Destroy(bool disposing)
        {
            Device.Destroy(handle);
        }

        public static ImageView Create(Image image, VkImageViewType viewType, VkFormat format, VkImageAspectFlags aspectMask, uint baseMipLevel, uint numMipLevels, uint baseArrayLayer = 0, uint arrayLayers = 1)
        {
            var viewCreateInfo = new VkImageViewCreateInfo
            {
                sType = VkStructureType.ImageViewCreateInfo,
                image = image.handle,
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

            return new ImageView(image, ref viewCreateInfo);
        }
    }


}
