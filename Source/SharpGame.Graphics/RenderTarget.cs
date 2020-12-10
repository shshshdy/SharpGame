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
        public uint width;
        public uint height;
        public uint layers;
        public RTType rTType = RTType.None;
        public SizeHint sizeHint = SizeHint.None;
        public VkAttachmentDescription attachmentDescription;
        public VkImageUsageFlags usage;
        public VkClearValue clearValue = new VkClearColorValue(0, 0, 0, 1);

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
            this.rTType = format.IsDepthFormat() ? RTType.DepthOutput : RTType.ColorOutput;
            attachmentDescription = new VkAttachmentDescription(format, VkSampleCountFlags.Count1);
            clearValue = new VkClearColorValue(0, 0, 0, 1);
        }

        public AttachmentInfo(SizeHint sizeHint, VkFormat format, VkImageUsageFlags usage, VkSampleCountFlags samples = VkSampleCountFlags.Count1)
        {
            this.sizeHint = sizeHint;
            this.usage = usage;
            attachmentDescription = new VkAttachmentDescription(format, samples);

            if (Device.IsDepthFormat(format))
            {
                clearValue = new VkClearDepthStencilValue(1, 0);
            }
            else
            {
                clearValue = new VkClearColorValue(0, 0, 0, 1);
            }
        }

        public AttachmentInfo(uint width, uint height, uint layers, VkFormat format, VkImageUsageFlags usage,
            VkSampleCountFlags samples = VkSampleCountFlags.Count1)
        {
            this.width = width;
            this.height = height;
            this.layers = layers;
            this.usage = usage;
            attachmentDescription = new VkAttachmentDescription(format, samples); 

            if(Device.IsDepthFormat(format))
            {
                clearValue = new VkClearDepthStencilValue(1, 0);
            }
            else
            {
                clearValue = new VkClearColorValue(0, 0, 0, 1);
            }
        }
    }

    public class RenderTexture : Texture
    {
        public SizeHint sizeHint = SizeHint.None;
        public VkSampleCountFlags samples;
        private Swapchain swapchain;
        public ImageView[] attachmentViews;
        public bool IsSwapchain => swapchain != null;

        public RenderTexture(Swapchain swapchain)
        {
            Create(swapchain);
        }
        
        public RenderTexture(uint width, uint height, uint layers, VkFormat format, VkImageUsageFlags usage,
            VkSampleCountFlags samples = VkSampleCountFlags.Count1)
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

        protected override void Destroy(bool disposing)
        {
            base.Destroy(disposing);
            
            swapchain = null;

            Array.Clear(attachmentViews, 0, attachmentViews.Length);
        }

        public void Create(in AttachmentInfo info)
        {
            Debug.Assert(info.rTType == RTType.None);
            this.extent = new VkExtent3D(info.width, info.height, 1);                
            this.layers = info.layers;
            this.format = info.format;
            this.imageUsageFlags = info.usage;
            this.samples = info.samples;
            this.sizeHint = info.sizeHint;
            Create();            
        }

        void Create(Swapchain swapchain)
        {
            this.swapchain = swapchain;
            this.extent = swapchain.extent;
            this.layers = 1;
            this.format = swapchain.ColorFormat;
            this.imageUsageFlags = VkImageUsageFlags.ColorAttachment;
            this.samples = VkSampleCountFlags.Count1;
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
        public VkExtent2D extent;
        public List<RenderTexture> attachments = new List<RenderTexture>();
        public RenderTarget(uint width, uint height)
        {
            extent = new VkExtent2D(width, height);
        }

        public RenderTexture this[int index] => attachments[index];
        public uint AttachmentCount => (uint)attachments.Count;

        public RenderTexture Add(uint width, uint height, uint layers, VkFormat format, VkImageUsageFlags usage, VkSampleCountFlags samples = VkSampleCountFlags.Count1)
        {
            var rt = new RenderTexture(width, height, layers, format, usage, samples);
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
