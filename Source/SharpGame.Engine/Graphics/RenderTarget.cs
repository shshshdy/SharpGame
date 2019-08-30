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
        public Sampler sampler;

        public RenderTarget(uint width, uint height, Format colorformat, bool depth = false)
        {
            if (depth)
            {
                image = Image.Create(width, height, ImageCreateFlags.None, 1, 1, colorformat, SampleCountFlags.Count1, (ImageUsageFlags.DepthStencilAttachment | ImageUsageFlags.TransferSrc));
                view = ImageView.Create(image, ImageViewType.Image2D, colorformat, (ImageAspectFlags.Depth | ImageAspectFlags.Stencil), 0, 1);
            }
            else
            {
                image = Image.Create(width, height, ImageCreateFlags.None, 1, 1, colorformat, SampleCountFlags.Count1, ImageUsageFlags.ColorAttachment);
                view = ImageView.Create(image, ImageViewType.Image2D, colorformat, ImageAspectFlags.Color, 0, 1);
            }

            sampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.ClampToEdge, false);
        }

        protected override void Destroy(bool disposing)
        {
            image?.Dispose();
            view?.Dispose();
            sampler?.Dispose();

            base.Destroy(disposing);
        }
    }
}
