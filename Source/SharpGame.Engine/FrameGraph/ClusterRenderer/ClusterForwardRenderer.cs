using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class ClusterForwardRenderer : ClusterRenderer
    {
        protected RenderTarget depthRT;
        protected RenderPass clusterRP;
        protected Framebuffer clusterFB;
        protected Format depthFormat = Device.GetSupportedDepthFormat();

        public ClusterForwardRenderer()
        {
        }

        protected override void CreateResources()
        {
            base.CreateResources();

            uint width = (uint)Graphics.Width;
            uint height = (uint)Graphics.Height;

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
            clusterRP = new RenderPass(ref renderPassInfo);

            depthRT = Graphics.DepthRT;// new RenderTarget(width, height, 1, depthFormat, ImageUsageFlags.DepthStencilAttachment | ImageUsageFlags.Sampled, ImageAspectFlags.Depth, SampleCountFlags.Count1, ImageLayout.ShaderReadOnlyOptimal);
            clusterFB = Framebuffer.Create(clusterRP, width, height, 1, new[] { depthRT.view });

            //Renderer.AddDebugImage(rtDepth.view);

        }

        protected override IEnumerator<FrameGraphPass> CreateRenderPass()
        {
            yield return new ShadowPass();

            clusterPass = new ScenePass("clustering")
            {
                PassQueue = PassQueue.EarlyGraphics,
                RenderPass = clusterRP,
                Framebuffer = clusterFB,
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

        protected override void OnBeginSubmit(FrameGraphPass renderPass, CommandBuffer cb, int imageIndex)
        {
            if (renderPass == clusterPass)
            {
                var queryPool = query_pool[imageIndex];
                cb.WriteTimestamp(PipelineStageFlags.TopOfPipe, queryPool, QUERY_CLUSTERING * 2);

            }
            else if (renderPass == mainPass)
            {
                var queryPool = query_pool[imageIndex];
                cb.ResetQueryPool(queryPool, 10, 4);
                cb.WriteTimestamp(PipelineStageFlags.TopOfPipe, queryPool, QUERY_ONSCREEN * 2);

            }
        }

        protected override void OnEndSubmit(FrameGraphPass renderPass, CommandBuffer cb, int imageIndex)
        {
            if (renderPass == clusterPass)
            {
                var queryPool = query_pool[imageIndex];
                cb.WriteTimestamp(PipelineStageFlags.FragmentShader, queryPool, QUERY_CLUSTERING * 2 + 1);
            }
            else if (renderPass == mainPass)
            {
                var queryPool = query_pool[imageIndex];

                cb.WriteTimestamp(PipelineStageFlags.ColorAttachmentOutput, queryPool, QUERY_ONSCREEN * 2 + 1);

                ClearBuffers(cb, imageIndex);
            }
        }

    }
}
