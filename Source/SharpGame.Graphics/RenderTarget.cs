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

    public class RenderTexture : RefCounted, IBindableResource
    {
        public Image image;
        public ImageView view;
        public Sampler sampler;

        RenderTextureInfo renderTargetInfo;

        public ref RenderTextureInfo Info => ref renderTargetInfo;

        internal DescriptorImageInfo descriptor;
        
        public RenderTexture(uint width, uint height, uint layers, Format format, ImageUsageFlags usage, ImageAspectFlags aspectMask,
            SampleCountFlags samples = SampleCountFlags.Count1, ImageLayout imageLayout = ImageLayout.Undefined)
        {
            renderTargetInfo = new RenderTextureInfo
            {
                width = width,
                height = height,
                layers = layers,
                format = format,
                usage = usage,
                aspectMask = aspectMask,
                samples = samples,
                imageLayout = imageLayout,
                isSwapchain = false,
            };

            Create(renderTargetInfo);
        }

        public RenderTexture(in RenderTextureInfo info)
        {
            Create(info);
        }

        void Create(in RenderTextureInfo info)
        {
            renderTargetInfo = info;
            image = Image.Create(info.width, info.height, ImageCreateFlags.None, info.layers, 1, info.format, SampleCountFlags.Count1, info.usage);
            view = ImageView.Create(image, info.layers > 1 ? ImageViewType.Image2DArray : ImageViewType.Image2D, info.format, info.aspectMask, 0, 1, 0, info.layers);
            sampler = Sampler.Create(Filter.Linear, SamplerMipmapMode.Linear, SamplerAddressMode.ClampToEdge, false);
            descriptor = new DescriptorImageInfo(sampler, view, info.imageLayout);
        }

        protected override void Destroy()
        {
            image?.Dispose();
            view?.Dispose();
            sampler?.Dispose();
        }
    }

    public class RenderTarget
    {
        public List<RenderTexture> attachments;
    }

}
