using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class EarlyZPass : ScenePass
    {
        const uint ATTACHMENT_REFERENCE_DEPTH = 0;
        const uint SUBPASS_DEPTH = 0;
        const uint SUBPASS_CLUSTER_FLAG = 1;
       
        RenderTarget p_rt_offscreen_depth_;
        Framebuffer frameBuffer;

        public EarlyZPass() : base(Pass.EarlyZ)
        {
            var depthFormat = Device.GetSupportedDepthFormat();

            uint width = (uint)Graphics.Width;
            uint height = (uint)Graphics.Height;

            p_rt_offscreen_depth_ = new RenderTarget(width, height, 1, depthFormat,
                ImageUsageFlags.DepthStencilAttachment, ImageAspectFlags.Depth, SampleCountFlags.Count1);

            AttachmentDescription[] attachments =
            {
                new AttachmentDescription(depthFormat, finalLayout : ImageLayout.DepthStencilReadOnlyOptimal)
            };

            SubpassDescription[] subpassDescription =
            {
                new SubpassDescription
                {
                    pipelineBindPoint = PipelineBindPoint.Graphics,

                    pDepthStencilAttachment = new []
                    {
                        new AttachmentReference(0, ImageLayout.DepthStencilAttachmentOptimal)
                    },
                }
            };

            // Subpass dependencies for layout transitions
            SubpassDependency[] dependencies =
            {
                new SubpassDependency
                {
                    srcSubpass = VulkanNative.SubpassExternal,
                    dstSubpass = 0,
                    srcStageMask = PipelineStageFlags.FragmentShader,
                    dstStageMask = PipelineStageFlags.EarlyFragmentTests,
                    srcAccessMask = AccessFlags.ShaderRead,
                    dstAccessMask = AccessFlags.DepthStencilAttachmentWrite,
                    dependencyFlags = DependencyFlags.ByRegion
                },

                new SubpassDependency
                {
                    srcSubpass = 0,
                    dstSubpass = VulkanNative.SubpassExternal,
                    srcStageMask = PipelineStageFlags.LateFragmentTests,
                    dstStageMask = PipelineStageFlags.FragmentShader,
                    srcAccessMask =  AccessFlags.DepthStencilAttachmentWrite,
                    dstAccessMask = AccessFlags.ShaderRead,
                    dependencyFlags = DependencyFlags.ByRegion
                },
            };

            var renderPassInfo = new RenderPassCreateInfo(attachments, subpassDescription, dependencies);
            renderPass = new RenderPass(ref renderPassInfo);
            frameBuffer = Framebuffer.Create(renderPass, width, height, 1, new[] { p_rt_offscreen_depth_.view });
        }
    }
}
