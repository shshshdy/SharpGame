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

        public RenderTarget(uint width, uint height, uint layers, Format format, ImageUsageFlags usage, ImageAspectFlags aspectMask, SampleCountFlags samples = SampleCountFlags.Count1)
        {
            image = Image.Create(width, height, ImageCreateFlags.None, layers, 1, format, SampleCountFlags.Count1, usage);
            view = ImageView.Create(image, layers > 1 ? ImageViewType.Image2DArray : ImageViewType.Image2D, format, aspectMask, 0, 1, 0, layers);
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
