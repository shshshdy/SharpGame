using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public struct Cascade
    {
        public Framebuffer frameBuffer;
        public ResourceSet descriptorSet;
        public ImageView view;

        public float splitDepth;
        public Matrix viewProjMatrix;

    }

    public class DepthPass : ScenePass
    {
        const uint SHADOW_MAP_CASCADE_COUNT = 4;
        const uint SHADOWMAP_DIM = 2048;

        RenderTarget depthRT;
        Cascade[] cascades = new Cascade[SHADOW_MAP_CASCADE_COUNT];

        public DepthPass() : base(Pass.Depth)
        {
            var depthFormat = Device.GetSupportedDepthFormat();

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
                    srcStageMask = PipelineStageFlags.BottomOfPipe,
                    dstStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    srcAccessMask = AccessFlags.MemoryRead,
                    dstAccessMask = (AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite),
                    dependencyFlags = DependencyFlags.ByRegion
                },

                new SubpassDependency
                {
                    srcSubpass = 0,
                    dstSubpass = VulkanNative.SubpassExternal,
                    srcStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    dstStageMask = PipelineStageFlags.BottomOfPipe,
                    srcAccessMask = (AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite),
                    dstAccessMask = AccessFlags.MemoryRead,
                    dependencyFlags = DependencyFlags.ByRegion
                },
            };

            var renderPassInfo = new RenderPassCreateInfo(attachments, subpassDescription, dependencies);
            renderPass = new RenderPass(ref renderPassInfo);

            depthRT = new RenderTarget(SHADOWMAP_DIM, SHADOWMAP_DIM, SHADOW_MAP_CASCADE_COUNT, depthFormat, ImageUsageFlags.DepthStencilAttachment | ImageUsageFlags.Sampled, ImageAspectFlags.Depth);
          
            for (uint i = 0; i < SHADOW_MAP_CASCADE_COUNT; i++)
            {
                cascades[i].view = ImageView.Create(depthRT.image, ImageViewType.Image2D, depthFormat, ImageAspectFlags.Depth, 0, 1, i, 1);
                cascades[i].frameBuffer = Framebuffer.Create(renderPass, SHADOWMAP_DIM, SHADOWMAP_DIM, 1, new[] { cascades[i].view });
            }


        }
            
    }
}
