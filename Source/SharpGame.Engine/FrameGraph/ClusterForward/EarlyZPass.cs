using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public partial class ClusterLighting : ScenePass
    {
        const uint ATTACHMENT_REFERENCE_DEPTH = 0;
        const uint SUBPASS_DEPTH = 0;
        const uint SUBPASS_CLUSTER_FLAG = 1;
       
        RenderTarget p_rt_offscreen_depth_;
        RenderPass rpEarlyZ;
        Framebuffer frameBuffer;

        Format depthFormat = Format.D16Unorm;

        private void InitEarlyZ()
        {
            uint width = (uint)Graphics.Width;
            uint height = (uint)Graphics.Height;

            AttachmentDescription[] attachments =
            {
                new AttachmentDescription(depthFormat, finalLayout : ImageLayout.DepthStencilReadOnlyOptimal)
            };

            var depthStencilAttachment = new []
            {
                 new AttachmentReference(ATTACHMENT_REFERENCE_DEPTH, ImageLayout.DepthStencilAttachmentOptimal)
            };

            SubpassDescription[] subpassDescription =
            {
		        // depth prepass
                new SubpassDescription
                {
                    pipelineBindPoint = PipelineBindPoint.Graphics,
                    pDepthStencilAttachment = depthStencilAttachment
                },
                
		        // clustering subpass
                new SubpassDescription
                {
                    pipelineBindPoint = PipelineBindPoint.Graphics,
                    pDepthStencilAttachment = depthStencilAttachment
                },
            };

            // Subpass dependencies for layout transitions
            SubpassDependency[] dependencies =
            {
                new SubpassDependency
                {
                    srcSubpass = VulkanNative.SubpassExternal,
                    dstSubpass = SUBPASS_DEPTH,
                    srcStageMask = PipelineStageFlags.BottomOfPipe,
                    dstStageMask = PipelineStageFlags.VertexShader,
                    srcAccessMask = AccessFlags.MemoryWrite,
                    dstAccessMask = AccessFlags.UniformRead,
                    dependencyFlags = DependencyFlags.ByRegion
                },

                new SubpassDependency
                {
                    srcSubpass = SUBPASS_DEPTH,
                    dstSubpass = SUBPASS_CLUSTER_FLAG,
                    srcStageMask = PipelineStageFlags.LateFragmentTests,
                    dstStageMask = PipelineStageFlags.EarlyFragmentTests,
                    srcAccessMask =  AccessFlags.DepthStencilAttachmentWrite,
                    dstAccessMask = AccessFlags.DepthStencilAttachmentRead,
                    dependencyFlags = DependencyFlags.ByRegion
                },

                new SubpassDependency
                {
                    srcSubpass = SUBPASS_CLUSTER_FLAG,
                    dstSubpass = VulkanNative.SubpassExternal,
                    srcStageMask = PipelineStageFlags.FragmentShader,
                    dstStageMask = PipelineStageFlags.ComputeShader,
                    srcAccessMask =  AccessFlags.ShaderWrite,
                    dstAccessMask = AccessFlags.ShaderRead,
                    dependencyFlags = DependencyFlags.ByRegion
                },
            };

            var renderPassInfo = new RenderPassCreateInfo(attachments, subpassDescription, dependencies);
            rpEarlyZ = new RenderPass(ref renderPassInfo);

            p_rt_offscreen_depth_ = new RenderTarget(width, height, 1, depthFormat, ImageUsageFlags.DepthStencilAttachment, ImageAspectFlags.Depth, SampleCountFlags.Count1);

            frameBuffer = Framebuffer.Create(rpEarlyZ, width, height, 1, new[] { p_rt_offscreen_depth_.view });
        }

        void DrawEarlyZ(GraphicsPass renderPass, RenderView view)
        {
            //renderPass.DrawBatchesMT(view, view.batches);
        }
    }
}
