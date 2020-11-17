using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class RenderTextureInfo
    {
        public uint width;
        public uint height;
        public uint layers;
        public Format format;
        public ImageUsageFlags usage;
        public ImageAspectFlags aspectMask;
        public SampleCountFlags samples;
        public ImageLayout imageLayout;
        public Swapchain swapchain;

        public RenderTextureInfo(Swapchain swapchain)
        {
            this.swapchain = swapchain;
        }

        public RenderTextureInfo(uint width, uint height, uint layers, Format format, ImageUsageFlags usage,
            SampleCountFlags samples = SampleCountFlags.Count1, ImageLayout imageLayout = ImageLayout.Undefined)
        {
            this.width = width;
            this.height = height;
            this.layers = layers;
            this.format = format;
            this.usage = usage;
            this.aspectMask = Device.IsDepthFormat(format) ? ImageAspectFlags.Depth : ImageAspectFlags.Color;
            this.samples = samples;
            this.imageLayout = imageLayout;
        }
    }

    public class RenderTexture : Texture
    {
        public ImageAspectFlags aspectMask;
        public SampleCountFlags samples;
        Swapchain swapchain;
        public ImageView[] attachmentViews;
        public bool IsSwapchain => swapchain != null;

        public RenderTexture(Swapchain swapchain)
        {
            Create(swapchain);
        }
        
        public RenderTexture(uint width, uint height, uint layers, Format format, ImageUsageFlags usage,
            SampleCountFlags samples = SampleCountFlags.Count1, ImageLayout imageLayout = ImageLayout.Undefined)
        {
            this.width = width;
            this.height = height;
            this.depth = 1;
            this.layers = layers;
            this.format = format;
            this.imageUsageFlags = usage;
            this.aspectMask = Device.IsDepthFormat(format) ? ImageAspectFlags.Depth : ImageAspectFlags.Color;
            this.samples = samples;
            this.imageLayout = imageLayout;

            Create();
        }

        public RenderTexture(in RenderTextureInfo info)
        {
            Create(info);
        }

        protected override void Destroy(bool disposing)
        {
            base.Destroy(disposing);
            
            swapchain = null;

            Array.Clear(attachmentViews, 0, attachmentViews.Length);
        }

        public void Create(in RenderTextureInfo info)
        {
            if (info.swapchain != null)
                Create(info.swapchain);
            else
            {
                this.width = info.width;
                this.height = info.height;
                this.depth = 1;
                this.layers = info.layers;
                this.format = info.format;
                this.imageUsageFlags = info.usage;
                this.aspectMask = info.aspectMask;
                this.samples = info.samples;
                this.imageLayout = info.imageLayout;

                Create();
            }
        }

        void Create(Swapchain swapchain)
        {
            this.swapchain = swapchain;
            this.extent = swapchain.extent;
            this.layers = 1;
            this.format = swapchain.ColorFormat;
            this.imageUsageFlags = ImageUsageFlags.ColorAttachment;
            this.aspectMask = Device.IsDepthFormat(format) ? ImageAspectFlags.Depth : ImageAspectFlags.Color;
            this.samples = SampleCountFlags.Count1;
            this.imageLayout = ImageLayout.ColorAttachmentOptimal;

            attachmentViews = (ImageView[])swapchain.ImageViews.Clone();
        }


        protected void Create()
        {
            image = Image.Create(width, height, ImageCreateFlags.None, layers, 1, format, this.samples, imageUsageFlags);
            imageView = ImageView.Create(image, layers > 1 ? ImageViewType.Image2DArray : ImageViewType.Image2D, format, aspectMask, 0, 1, 0, layers);
            sampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.ClampToEdge, false);
            descriptor = new DescriptorImageInfo(sampler, imageView, imageLayout);
            attachmentViews = new ImageView[Swapchain.IMAGE_COUNT];
            Array.Fill(attachmentViews, imageView);
        }
    }

    public class RenderTarget
    {
        public Extent3D extent;
        public List<RenderTexture> attachments = new List<RenderTexture>();
        public RenderTarget()
        {
        }

        public RenderTexture this[int index] => attachments[index];
        public uint AttachmentCount => (uint)attachments.Count;

        public void Add(in RenderTextureInfo info)
        {
            Add(new RenderTexture(info));
        }

        public void Add(RenderTexture rt)
        {
            if (extent.width == 0 || extent.height == 0)
            {
                extent = rt.extent;
            }
            else
            {
                Debug.Assert(extent == rt.extent);
            }

            attachments.Add(rt);

        }

        public VkImageView[] GetViews(int imageIndex)
        {
            var views = new VkImageView[AttachmentCount];
            for(int i = 0; i < AttachmentCount; i++)
            {
                views[i] = this[i].attachmentViews[imageIndex].handle;
            }

            return views;
        }

        public void Clear()
        {
            foreach(var attachment in attachments)
            {
                attachment?.Dispose();
            }
        }

    }

}
