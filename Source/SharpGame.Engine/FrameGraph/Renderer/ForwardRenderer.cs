using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ForwardRenderer : RenderPipeline
    {
        public ForwardRenderer()
        {
            var depthFormat = Graphics.DepthFormat;
            uint width = Graphics.Width;
            uint height = Graphics.Height;

            Add(new ShadowPass());
            /*
            Add(new FrameGraphPass
            {
                new RenderTextureInfo((uint)width, (uint)height, 1, VkFormat.R8G8B8A8UNorm, 
                VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.Sampled| VkImageUsageFlags.InputAttachment)
                {
                    finalLayout = VkImageLayout.ShaderReadOnlyOptimal
                },
                new RenderTextureInfo((uint)width, (uint)height, 1, depthFormat, VkImageUsageFlags.DepthStencilAttachment),

                new SceneSubpass
                {
                    DisableDepthStencilAttachment = false
                }

            });*/

            Add(new FrameGraphPass
            {
                new RenderTextureInfo(Graphics.Swapchain)
                {

                },

                new RenderTextureInfo((uint)width, (uint)height, 1, depthFormat, VkImageUsageFlags.DepthStencilAttachment),
                
                new SceneSubpass
                {
                    DisableDepthStencilAttachment = false
                },

//                 new FullScreenSubpass("shaders/post/fullscreen.frag")
//                 {
//                 }

            });


        }

    }
}
