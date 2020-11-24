using System;
using System.Collections.Generic;
using System.Text;


namespace SharpGame
{
    public class RenderTextureInfo
    {
        public uint width;
        public uint height;
        public uint layers;

        public AttachmentDescription attachmentDescription;

        public ImageUsageFlags usage;
        public Swapchain swapchain;
        public ClearValue clearValue = new ClearColorValue(0, 0, 0, 1);

        public ref Format format => ref attachmentDescription.format;
        public ref SampleCountFlags samples => ref attachmentDescription.samples;
        public ref AttachmentLoadOp loadOp => ref attachmentDescription.loadOp;
        public ref AttachmentStoreOp storeOp => ref attachmentDescription.storeOp;
        public ref AttachmentLoadOp stencilLoadOp => ref attachmentDescription.stencilLoadOp;
        public ref AttachmentStoreOp stencilStoreOp => ref attachmentDescription.stencilStoreOp;
        public ref ImageLayout initialLayout => ref attachmentDescription.initialLayout;
        public ref ImageLayout finalLayout => ref attachmentDescription.finalLayout;

        public RenderTextureInfo(Swapchain swapchain)
        {
            this.swapchain = swapchain;
            attachmentDescription = new AttachmentDescription(swapchain.ColorFormat, SampleCountFlags.Count1);
            clearValue = new ClearColorValue(0, 0, 0, 1);
        }

        public RenderTextureInfo(uint width, uint height, uint layers, Format format, ImageUsageFlags usage,
            SampleCountFlags samples = SampleCountFlags.Count1)
        {
            this.width = width;
            this.height = height;
            this.layers = layers;
            this.usage = usage;
            attachmentDescription = new AttachmentDescription(format, samples); 

            if(Device.IsDepthFormat(format))
            {
                clearValue = new ClearDepthStencilValue(1, 0);
            }
            else
            {
                clearValue = new ClearColorValue(0, 0, 0, 1);
            }
        }
    }

    public class RenderTexture : Texture
    {
        public SampleCountFlags samples;
        private Swapchain swapchain;
        public ImageView[] attachmentViews;
        public bool IsSwapchain => swapchain != null;

        public RenderTexture(Swapchain swapchain)
        {
            Create(swapchain);
        }
        
        public RenderTexture(uint width, uint height, uint layers, Format format, ImageUsageFlags usage,
            SampleCountFlags samples = SampleCountFlags.Count1)
        {
            this.width = width;
            this.height = height;
            this.depth = 1;
            this.layers = layers;
            this.format = format;
            this.imageUsageFlags = usage;
            this.samples = samples;

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
                this.samples = info.samples;

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
            this.samples = SampleCountFlags.Count1;

            attachmentViews = (ImageView[])swapchain.ImageViews.Clone();
        }

        protected void Create()
        {
            var aspectMask = Device.IsDepthFormat(format) ? VkImageAspectFlags.Depth : VkImageAspectFlags.Color;
            image = Image.Create(width, height, ImageCreateFlags.None, layers, 1, format, this.samples, imageUsageFlags);
            imageView = ImageView.Create(image, layers > 1 ? ImageViewType.Image2DArray : ImageViewType.Image2D, format, aspectMask, 0, 1, 0, layers);
            sampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.ClampToEdge, false);

            this.imageLayout = ImageLayout.ShaderReadOnlyOptimal;
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
