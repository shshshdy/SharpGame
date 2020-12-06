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

            var onScreenPass = new FrameGraphPass
            {
                new RenderTextureInfo(Graphics.Swapchain),
                new RenderTextureInfo(width, height, 1, depthFormat, VkImageUsageFlags.DepthStencilAttachment | VkImageUsageFlags.Sampled| VkImageUsageFlags.InputAttachment),


                new SceneSubpass
                {
                    DisableDepthStencilAttachment = false
                }

            };


            Add(onScreenPass);


        }

    }

    public class ForwardHdrRenderer : RenderPipeline
    {
        public ForwardHdrRenderer()
        {
            var depthFormat = Graphics.DepthFormat;
            uint width = Graphics.Width;
            uint height = Graphics.Height;

            Add(new ShadowPass());

            var fgPass = new FrameGraphPass
            {
                new RenderTextureInfo(width, height, 1, VkFormat.R16G16B16A16SFloat, VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.Sampled| VkImageUsageFlags.InputAttachment)
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


            var specializationInfo = new SpecializationInfo(new VkSpecializationMapEntry(0, 0, sizeof(uint)));
            specializationInfo.Write(0, 1);

            var onScreenPass = new FrameGraphPass
            {
                new RenderTextureInfo(Graphics.Swapchain),

                new FullScreenSubpass("shaders/post/fullscreen.frag")
                {
                    onBindResource = (rs)=>
                    {
                        rs.SetResourceSet(0, fgPass.RenderTarget[0]);
                    }
                },

                new FullScreenSubpass("shaders/post/bloom.frag")
                {
                    AddtiveMode = true,

                    onBindResource = (rs)=>
                    {
                        rs.SetResourceSet(0, fgPass.RenderTarget[0]);
                    }
                },


                new FullScreenSubpass("shaders/post/bloom.frag", specializationInfo)
                {
                    AddtiveMode = true,

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
