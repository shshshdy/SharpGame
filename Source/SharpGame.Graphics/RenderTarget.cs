using System;
using System.Collections.Generic;
using System.Text;


namespace SharpGame
{
    public enum SizeHint
    {
        None,
        Full,
        Half,
        Quater
    }

    public enum RTType
    {
        ColorOutput,
        DepthOutput,
        None,
    }

    public class AttachmentInfo
    {
        public uint Width { get; }
        public uint Height { get; }
        public uint Layers { get; }
        public RTType RTType { get; } = RTType.None;
        public SizeHint SizeHint { get; } = SizeHint.None;
        public VkImageUsageFlags Usage { get; }
        public VkClearValue ClearValue { get; } = new VkClearColorValue(0, 0, 0, 1);

        public VkAttachmentDescription attachmentDescription;
        public ref VkFormat format => ref attachmentDescription.format;
        public ref VkSampleCountFlags samples => ref attachmentDescription.samples;
        public ref VkAttachmentLoadOp loadOp => ref attachmentDescription.loadOp;
        public ref VkAttachmentStoreOp storeOp => ref attachmentDescription.storeOp;
        public ref VkAttachmentLoadOp stencilLoadOp => ref attachmentDescription.stencilLoadOp;
        public ref VkAttachmentStoreOp stencilStoreOp => ref attachmentDescription.stencilStoreOp;
        public ref VkImageLayout initialLayout => ref attachmentDescription.initialLayout;
        public ref VkImageLayout finalLayout => ref attachmentDescription.finalLayout;

        public AttachmentInfo(VkFormat format)
        {
            this.RTType = format.IsDepthFormat() ? RTType.DepthOutput : RTType.ColorOutput;
            attachmentDescription = new VkAttachmentDescription(format, VkSampleCountFlags.Count1);

            if (Device.IsDepthFormat(format))
            {
                ClearValue = new VkClearDepthStencilValue(1, 0);
            }
            else
            {
                ClearValue = new VkClearColorValue(0, 0, 0, 1);
            }
        }

        public AttachmentInfo(SizeHint sizeHint, VkFormat format, VkImageUsageFlags usage, VkSampleCountFlags samples = VkSampleCountFlags.Count1)
        {
            this.SizeHint = sizeHint;
            this.Usage = usage;
            attachmentDescription = new VkAttachmentDescription(format, samples);

            if (Device.IsDepthFormat(format))
            {
                ClearValue = new VkClearDepthStencilValue(1, 0);
            }
            else
            {
                ClearValue = new VkClearColorValue(0, 0, 0, 1);
            }
        }

        public AttachmentInfo(uint width, uint height, uint layers, VkFormat format, VkImageUsageFlags usage,
            VkSampleCountFlags samples = VkSampleCountFlags.Count1)
        {
            this.Width = width;
            this.Height = height;
            this.Layers = layers;
            this.Usage = usage;
            attachmentDescription = new VkAttachmentDescription(format, samples); 

            if(Device.IsDepthFormat(format))
            {
                ClearValue = new VkClearDepthStencilValue(1, 0);
            }
            else
            {
                ClearValue = new VkClearColorValue(0, 0, 0, 1);
            }
        }
    }

    public class RenderTexture : Texture
    {
        private VkSampleCountFlags samples;
        private SizeHint sizeHint = SizeHint.None;
        private Swapchain swapchain;
        public ImageView[] attachmentViews;
        public bool IsSwapchain => swapchain != null;

        public RenderTexture(Swapchain swapchain)
        {
            Create(swapchain);
        }
        
        public RenderTexture(uint width, uint height, uint layers, VkFormat format, VkImageUsageFlags usage,
            VkSampleCountFlags samples = VkSampleCountFlags.Count1, SizeHint sizeHint = SizeHint.None)
        {
            this.extent = new VkExtent3D(width, height, 1);
            this.layers = layers;
            this.format = format;
            this.imageUsageFlags = usage;
            this.samples = samples;

            Create();
        }

        public RenderTexture(in AttachmentInfo info)
        {
            Create(info);
        }

        public void Create(in AttachmentInfo info)
        {
            Debug.Assert(info.RTType == RTType.None);
            this.extent = new VkExtent3D(info.Width, info.Height, 1);
            this.layers = info.Layers;
            this.format = info.format;
            this.imageUsageFlags = info.Usage;
            this.samples = info.samples;
            this.sizeHint = info.SizeHint;

            Create();
        }

        protected override void Destroy(bool disposing)
        {
            base.Destroy(disposing);
            
            swapchain = null;

            Array.Clear(attachmentViews, 0, attachmentViews.Length);
        }

        private void Create(Swapchain swapchain)
        {
            this.swapchain = swapchain;
            this.extent = swapchain.extent;
            this.layers = 1;
            this.format = swapchain.ColorFormat;
            this.imageUsageFlags = VkImageUsageFlags.ColorAttachment;
            this.samples = VkSampleCountFlags.Count1;
            this.sizeHint = SizeHint.Full;
            attachmentViews = (ImageView[])swapchain.ImageViews.Clone();
        }

        protected void Create()
        {
            var aspectMask = Device.IsDepthFormat(format) ? VkImageAspectFlags.Depth : VkImageAspectFlags.Color;
            image = Image.Create(width, height, VkImageCreateFlags.None, layers, 1, format, this.samples, imageUsageFlags);
            imageView = ImageView.Create(image, layers > 1 ? VkImageViewType.Image2DArray : VkImageViewType.Image2D, format, aspectMask, 0, 1, 0, layers);
            sampler = new Sampler(VkFilter.Linear, VkSamplerMipmapMode.Linear, VkSamplerAddressMode.ClampToEdge, 1, false);
            imageLayout = VkImageLayout.ShaderReadOnlyOptimal;
            descriptor = new VkDescriptorImageInfo(sampler, imageView, imageLayout);
            attachmentViews = new ImageView[Swapchain.IMAGE_COUNT];

            Array.Fill(attachmentViews, imageView);
        }
    }

    public class RenderTarget
    {
        public VkExtent2D Extent { get; private set; }
        public uint AttachmentCount => (uint)attachments.Count;
        private List<RenderTexture> attachments = new List<RenderTexture>();

        public RenderTarget(uint width, uint height)
        {
            Extent = new VkExtent2D(width, height);
        }

        public RenderTexture this[int index] => attachments[index];

        public RenderTexture Add(Swapchain swapchain)
        {
            var rt = new RenderTexture(swapchain);
            Add(rt);
            return rt;
        }

        public RenderTexture Add(VkFormat format, VkImageUsageFlags usage, VkSampleCountFlags samples = VkSampleCountFlags.Count1, SizeHint sizeHint = SizeHint.Full)
        {
            var rt = new RenderTexture(Extent.width, Extent.height, 1, format, usage, samples, sizeHint);
            Add(rt);
            return rt;
        }

        public RenderTexture Add(uint width, uint height, uint layers, VkFormat format, VkImageUsageFlags usage, VkSampleCountFlags samples = VkSampleCountFlags.Count1)
        {
            var rt = new RenderTexture(width, height, layers, format, usage, samples, SizeHint.None);
            Add(rt);
            return rt;
        }

        public void Add(in AttachmentInfo info)
        {
            Add(new RenderTexture(info));
        }

        public void Add(RenderTexture rt)
        {
            attachments.Add(rt);
        }

        public VkImageView[] GetViews(int imageIndex)
        {
            var views = new VkImageView[AttachmentCount];
            for(int i = 0; i < AttachmentCount; i++)
            {
                views[i] = this[i].attachmentViews[imageIndex];
            }

            return views;
        }

        public void Clear()
        {
            foreach(var attachment in attachments)
            {
                attachment?.Dispose();
            }

            attachments.Clear();
        }

    }

}
