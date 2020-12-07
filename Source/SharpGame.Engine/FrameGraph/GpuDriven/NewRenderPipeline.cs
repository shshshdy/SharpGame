using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class NewRenderPipeline : RenderPipeline
    {
        public NewRenderPipeline()
        {
            var depthFormat = Graphics.DepthFormat;
            uint width = Graphics.Width;
            uint height = Graphics.Height;

            Add(new ShadowPass());

            Add(new FrameGraphPass
            {
                new RenderTextureInfo(Graphics.Swapchain),
                new RenderTextureInfo(width, height, 1, depthFormat, VkImageUsageFlags.DepthStencilAttachment),

                new SceneSubpass
                {
                    DisableDepthStencilAttachment = false
                }
                
            });
        }

    }
}
