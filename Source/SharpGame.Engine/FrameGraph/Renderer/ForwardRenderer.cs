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

            var fgPass = new FrameGraphPass
            {
                new RenderTextureInfo(width, height, 1, VkFormat.R8G8B8A8UNorm, VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.Sampled| VkImageUsageFlags.InputAttachment)
                {
                    finalLayout = VkImageLayout.ShaderReadOnlyOptimal
                },

                new RenderTextureInfo(width, height, 1, depthFormat, VkImageUsageFlags.DepthStencilAttachment | VkImageUsageFlags.Sampled| VkImageUsageFlags.InputAttachment),

                new SceneSubpass
                {
                    DisableDepthStencilAttachment = false
                }

            };


            Add(fgPass);


            var onScreenPass = new FrameGraphPass
            {
                new RenderTextureInfo(Graphics.Swapchain)
                {
                },

                new FullScreenSubpass("shaders/post/fullscreen.frag")
                {
                    onBindResource = (rs)=>
                    {
                        rs.SetResourceSet(0, fgPass.RenderTarget[0]);
                    }
                }

            };


            Add(onScreenPass);


        }

    }
}
