using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public struct RenderTargetInfo :IEquatable<RenderTargetInfo>
    {
        public uint width;
        public uint height;
        public uint layers;
        public Format format;
        public ImageUsageFlags usage;
        public ImageAspectFlags aspectMask;
        public SampleCountFlags samples;
        public ImageLayout imageLayout;

        public bool Equals(RenderTargetInfo other)
        {
            return width == other.width && height == other.height && layers == other.layers && format == other.format
                && usage == other.usage && aspectMask == other.aspectMask && samples == other.samples && imageLayout == other.imageLayout;
        }
    }

    public class RenderTarget : RefCounted, IBindableResource
    {
        public Image image;
        public ImageView view;
        public Sampler sampler;

        RenderTargetInfo renderTargetInfo;
        public ref RenderTargetInfo Info => ref renderTargetInfo;

        internal DescriptorImageInfo descriptor;
        
        public RenderTarget(uint width, uint height, uint layers, Format format, ImageUsageFlags usage, ImageAspectFlags aspectMask,
            SampleCountFlags samples = SampleCountFlags.Count1, ImageLayout imageLayout = ImageLayout.Undefined)
        {
            image = Image.Create(width, height, ImageCreateFlags.None, layers, 1, format, SampleCountFlags.Count1, usage);
            view = ImageView.Create(image, layers > 1 ? ImageViewType.Image2DArray : ImageViewType.Image2D, format, aspectMask, 0, 1, 0, layers);
            sampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.ClampToEdge, false);
            descriptor = new DescriptorImageInfo(sampler, view, imageLayout); 

            renderTargetInfo = new RenderTargetInfo
            {
                width = width,
                height = height,
                layers = layers,
                format = format,
                usage = usage,
                aspectMask = aspectMask,
                samples = samples,
                imageLayout = imageLayout
            };
           
        }

        protected override void Destroy()
        {
            image?.Dispose();
            view?.Dispose();
            sampler?.Dispose();
        }
    }
}
