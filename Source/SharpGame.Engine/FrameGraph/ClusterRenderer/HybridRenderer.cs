using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class HybridRenderer : ClusterRenderer
    {
        private RenderTarget albedoRT;
        private RenderTarget normalRT;
        private RenderTarget depthRT;

        private Framebuffer geometryFB;
        private RenderPass geometryRP;

        protected GraphicsPass lightingPass;
        protected GraphicsPass compositePass;
        protected ScenePass translucentPass;

        public HybridRenderer()
        {
        }

        protected override void CreateResources()
        {
            base.CreateResources();

            uint width = (uint)Graphics.Width;
            uint height = (uint)Graphics.Height;
            Format depthFormat = Device.GetSupportedDepthFormat();

            AttachmentDescription[] attachments =
            {
                new AttachmentDescription(Format.R8g8b8a8Unorm, finalLayout : ImageLayout.ShaderReadOnlyOptimal),
                new AttachmentDescription(Format.R8g8b8a8Unorm, finalLayout : ImageLayout.ShaderReadOnlyOptimal),
                new AttachmentDescription(depthFormat, finalLayout : ImageLayout.DepthStencilReadOnlyOptimal)
            };

            var colorAttachments = new[]
            {
                 new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal),
                 new AttachmentReference(1, ImageLayout.ColorAttachmentOptimal)
            };

            var depthStencilAttachment = new[]
            {
                 new AttachmentReference(0, ImageLayout.DepthStencilAttachmentOptimal)
            };

            SubpassDescription[] subpassDescription =
            {
		        // clustering subpass
                new SubpassDescription
                {
                    pipelineBindPoint = PipelineBindPoint.Graphics,
                    pColorAttachments = colorAttachments,
                    pDepthStencilAttachment = depthStencilAttachment
                },
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
                    dstAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite,
                    dependencyFlags = DependencyFlags.ByRegion
                },

                new SubpassDependency
                {
                    srcSubpass = 0,
                    dstSubpass = VulkanNative.SubpassExternal,
                    srcStageMask = PipelineStageFlags.ColorAttachmentOutput,
                    dstStageMask = PipelineStageFlags.BottomOfPipe,
                    srcAccessMask = AccessFlags.ColorAttachmentRead | AccessFlags.ColorAttachmentWrite,
                    dstAccessMask = AccessFlags.MemoryRead,
                    dependencyFlags = DependencyFlags.ByRegion
                },
            };

            var renderPassInfo = new RenderPassCreateInfo(attachments, subpassDescription, dependencies);
            geometryRP = new RenderPass(ref renderPassInfo);

            albedoRT = new RenderTarget(width, height, 1, Format.R8g8b8a8Unorm,
                        ImageUsageFlags.ColorAttachment | ImageUsageFlags.Sampled, ImageAspectFlags.Color,
                        SampleCountFlags.Count1, ImageLayout.ColorAttachmentOptimal);

            normalRT = new RenderTarget(width, height, 1, Format.R8g8b8a8Unorm,
                        ImageUsageFlags.ColorAttachment | ImageUsageFlags.Sampled, ImageAspectFlags.Color,
                        SampleCountFlags.Count1, ImageLayout.ColorAttachmentOptimal);

            depthRT = new RenderTarget(width, height, 1, depthFormat,
                        ImageUsageFlags.DepthStencilAttachment | ImageUsageFlags.Sampled, ImageAspectFlags.Depth,
                        SampleCountFlags.Count1, ImageLayout.DepthStencilReadOnlyOptimal);

            geometryFB = Framebuffer.Create(geometryRP, width, height, 1,
                new[] { albedoRT.view, normalRT.view, depthRT.view });

            Renderer.AddDebugImage(albedoRT.view);
            Renderer.AddDebugImage(normalRT.view);
            Renderer.AddDebugImage(depthRT.view);

        }

        protected override IEnumerator<FrameGraphPass> CreateRenderPass()
        {
            yield return new ShadowPass();

            clustering = new ScenePass("gbuffer")
            {
                PassQueue = PassQueue.EarlyGraphics,                
                RenderPass = geometryRP,
                Framebuffer = geometryFB,
                Set1 = clusterSet1
            };

            yield return clustering;

            lightCull = new ComputePass(ComputeLight);
            yield return lightCull;

            translucentPass = new ScenePass("cluster_forward")
            {
                Set1 = resourceSet0,
                Set2 = resourceSet1,
                BlendFlags = BlendFlags.AlphaBlend
            };

            yield return translucentPass;
        }

    }
}
