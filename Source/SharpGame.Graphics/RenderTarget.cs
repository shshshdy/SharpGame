using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public struct RenderTextureInfo
    {
        public uint width;
        public uint height;
        public uint layers;
        public Format format;
        public ImageUsageFlags usage;
        public ImageAspectFlags aspectMask;
        public SampleCountFlags samples;
        public ImageLayout imageLayout;
        public bool isSwapchain;
    }

    public class RenderTexture : Texture
    {
        public ImageAspectFlags aspectMask;
        public SampleCountFlags samples;
        public bool isSwapchain;

        public RenderTexture(Image swapchainImage)
        {
            this.width = swapchainImage.extent.width;
            this.height = swapchainImage.extent.height;
            this.layers = 1;
            this.format = swapchainImage.format;
            this.imageUsageFlags = ImageUsageFlags.ColorAttachment;
            this.aspectMask = Device.IsDepthFormat(format) ? ImageAspectFlags.Depth : ImageAspectFlags.Color;
            this.samples = SampleCountFlags.Count1;
            this.imageLayout = ImageLayout.ColorAttachmentOptimal;
            this.isSwapchain = true;
        }
        
        public RenderTexture(uint width, uint height, uint layers, Format format, ImageUsageFlags usage, //ImageAspectFlags aspectMask,
            SampleCountFlags samples = SampleCountFlags.Count1, ImageLayout imageLayout = ImageLayout.Undefined)
        {
            this.width = width;
            this.height = height;
            this.layers = layers;
            this.format = format;
            this.imageUsageFlags = usage;
            this.aspectMask = Device.IsDepthFormat(format) ? ImageAspectFlags.Depth : ImageAspectFlags.Color;// aspectMask;
            this.samples = samples;
            this.imageLayout = imageLayout;
            this.isSwapchain = false;

            Create();
        }

        public RenderTexture(in RenderTextureInfo info)
        {
            Create(info);
        }

        void Create(in RenderTextureInfo info)
        {
            this.width = info.width;
            this.height = info.height;
            this.layers = info.layers;
            this.format = info.format;
            this.imageUsageFlags = info.usage;
            this.aspectMask = info.aspectMask;
            this.samples = info.samples;
            this.imageLayout = info.imageLayout;
            this.isSwapchain = info.isSwapchain;

            Create();
        }

        protected void Create()
        {
            image = Image.Create(width, height, ImageCreateFlags.None, layers, 1, format, this.samples, imageUsageFlags);
            imageView = ImageView.Create(image, layers > 1 ? ImageViewType.Image2DArray : ImageViewType.Image2D, format, aspectMask, 0, 1, 0, layers);
            sampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.ClampToEdge, false);
            descriptor = new DescriptorImageInfo(sampler, imageView, imageLayout);
        }
    }

    public class RenderTarget
    {
        public List<RenderTexture> attachments;
    }

}
