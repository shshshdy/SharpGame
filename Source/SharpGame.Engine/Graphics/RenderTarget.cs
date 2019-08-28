using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class RenderTarget : DisposeBase
    {
        public Image image;
        public ImageView view;

        public Image depthImage;
        public ImageView depthView;

        public RenderTarget(uint width, uint height, Format colorformat, Format depthformat)
        {
            if (colorformat != Format.Undefined)
            {
                image = Image.Create(width, height, ImageCreateFlags.None, 1, 1, colorformat, SampleCountFlags.Count1, ImageUsageFlags.ColorAttachment);
                view = ImageView.Create(image, ImageViewType.Image2D, colorformat, ImageAspectFlags.Color, 0, 1);
            }

            if (depthformat != Format.Undefined)
            {
                depthImage = Image.Create(width, height, ImageCreateFlags.None, 1, 1, depthformat, SampleCountFlags.Count1, (ImageUsageFlags.DepthStencilAttachment | ImageUsageFlags.TransferSrc));
                depthView = ImageView.Create(depthImage, ImageViewType.Image2D, depthformat, (ImageAspectFlags.Depth | ImageAspectFlags.Stencil), 0, 1);
            }
        }

        protected override void Destroy(bool disposing)
        {
            image?.Dispose();
            view?.Dispose();
            depthImage?.Dispose();
            depthView?.Dispose();

            base.Destroy(disposing);
        }
    }
}
