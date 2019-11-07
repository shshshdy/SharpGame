using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class HybridRenderer : ClusterRenderer
    {
        protected RenderTarget depthRT;
        protected RenderPass geometryRP;
        protected Framebuffer geometryFB;

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
                new AttachmentDescription(depthFormat, finalLayout : ImageLayout.DepthStencilReadOnlyOptimal)
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
                    dstStageMask = PipelineStageFlags.VertexShader,
                    srcAccessMask = AccessFlags.MemoryWrite,
                    dstAccessMask = AccessFlags.UniformRead,
                    dependencyFlags = DependencyFlags.ByRegion
                },

                new SubpassDependency
                {
                    srcSubpass = 0,
                    dstSubpass = VulkanNative.SubpassExternal,
                    srcStageMask = PipelineStageFlags.FragmentShader,
                    dstStageMask = PipelineStageFlags.ComputeShader,
                    srcAccessMask =  AccessFlags.ShaderWrite,
                    dstAccessMask = AccessFlags.ShaderRead,
                    dependencyFlags = DependencyFlags.ByRegion
                },
            };

            var renderPassInfo = new RenderPassCreateInfo(attachments, subpassDescription, dependencies);
            geometryRP = new RenderPass(ref renderPassInfo);

            depthRT = Graphics.DepthRT;
            geometryFB = Framebuffer.Create(geometryRP, width, height, 1, new[] { depthRT.view });

            //Renderer.AddDebugImage(rtDepth.view);

        }

        protected override IEnumerator<FrameGraphPass> CreateRenderPass()
        {
            yield return new ShadowPass();

            clusterPass = new ScenePass("gbuffer")
            {
                PassQueue = PassQueue.EarlyGraphics,                
                RenderPass = geometryRP,
                Framebuffer = geometryFB,
                Set1 = clusterSet1
            };

            yield return clusterPass;

            lightPass = new ComputePass(ComputeLight);
            yield return lightPass;

            mainPass = new ScenePass("cluster_forward")
            {
#if NO_DEPTHWRITE
                RenderPass = Graphics.CreateRenderPass(true, false),
#endif
                Set1 = resourceSet0,
                Set2 = resourceSet1,
            };

            yield return mainPass;
        }

    }
}
