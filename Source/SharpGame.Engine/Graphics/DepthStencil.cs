using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;
using static Vulkan.VulkanNative;

namespace SharpGame
{
    public class DepthStencil : DisposeBase
    {
        public Image image;
        public ImageView view;

        public DepthStencil(uint width, uint height, Format format)
        {
            image = Image.Create(width, height, ImageCreateFlags.None, 1, 1, format, SampleCountFlags.Count1, (ImageUsageFlags.DepthStencilAttachment | ImageUsageFlags.TransferSrc));
            view = ImageView.Create(image, ImageViewType.Image2D, format, (ImageAspectFlags.Depth | ImageAspectFlags.Stencil), 0, 1);
        }

        protected override void Destroy()
        {
            image.Dispose();
            view.Dispose();

            base.Destroy();
        }
    }

}
