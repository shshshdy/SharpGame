using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ForwardRenderer : RenderPipeline
    {
        public ForwardRenderer()
        {        
            Add(new ShadowPass());

            var onScreenPass = new FrameGraphPass
            {
                new AttachmentInfo(Graphics.ColorFormat),
                new AttachmentInfo(Graphics.DepthFormat),

                new SceneSubpass
                {
                    DisableDepthStencil = false
                }

            };

            Add(onScreenPass);

        }

    }

    public class ForwardHdrRenderer : RenderPipeline
    {
        public ForwardHdrRenderer()
        {
            Add(new ShadowPass());

            var fgPass = new FrameGraphPass
            {
                new AttachmentInfo("color", SizeHint.Full, VkFormat.R16G16B16A16SFloat, VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.Sampled| VkImageUsageFlags.InputAttachment)
                {
                    finalLayout = VkImageLayout.ShaderReadOnlyOptimal
                },

                new AttachmentInfo(Graphics.DepthFormat),

                new SceneSubpass
                {
                    DisableDepthStencil = false
                }

            };


            Add(fgPass);


            var specializationInfo = new SpecializationInfo(new VkSpecializationMapEntry(0, 0, sizeof(uint)));
            specializationInfo.Write(0, 1);

            var onScreenPass = new FrameGraphPass
            {
                new AttachmentInfo(Graphics.Swapchain.ColorFormat),

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
