using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class Renderer : Object
    {
        private ImageView[] _imageViews;
        private Framebuffer[] _framebuffers;

        public Graphics Graphics => Get<Graphics>();

        public Renderer()
        {
        }

        public void Initialize()
        {

            _imageViews = CreateImageViews();
            _framebuffers = CreateFramebuffers();
        }


        private ImageView[] CreateImageViews()
        {
            var imageViews = new ImageView[Graphics.SwapchainImages.Length];
            for (int i = 0; i < Graphics.SwapchainImages.Length; i++)
            {
                imageViews[i] = Graphics.SwapchainImages[i].CreateView(new ImageViewCreateInfo(
                    Graphics.Swapchain.Format,
                    new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1)));
            }
            return imageViews;
        }

        private Framebuffer[] CreateFramebuffers()
        {
            var framebuffers = new Framebuffer[Graphics.SwapchainImages.Length];
            for (int i = 0; i < Graphics.SwapchainImages.Length; i++)
            {
                framebuffers[i] = Graphics.MainRenderPass.CreateFramebuffer(new FramebufferCreateInfo(
                    new[] { _imageViews[i] },
                    Graphics.Width,
                    Graphics.Height));
            }
            return framebuffers;
        }

    }
}
