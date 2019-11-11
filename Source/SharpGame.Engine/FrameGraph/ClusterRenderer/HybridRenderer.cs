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

        protected Framebuffer clusterFB;

        protected ScenePass geometryPass;
        protected ScenePass translucentClustering;
        protected GraphicsPass compositePass;
        protected ScenePass translucentPass;
        protected Shader clusterDeferred;

        ResourceLayout deferredLayout0;
        ResourceLayout deferredLayout1;

        ResourceSet deferredSet0;
        ResourceSet deferredSet1;

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
                 new AttachmentReference(2, ImageLayout.DepthStencilAttachmentOptimal)
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
                        ImageUsageFlags.DepthStencilAttachment | ImageUsageFlags.Sampled, ImageAspectFlags.Depth | ImageAspectFlags.Stencil,
                        SampleCountFlags.Count1, ImageLayout.DepthStencilAttachmentOptimal/*ImageLayout.DepthStencilReadOnlyOptimal*/
                        );

            geometryFB = Framebuffer.Create(geometryRP, width, height, 1, new[] { albedoRT.view, normalRT.view, depthRT.view });

            Renderer.AddDebugImage(albedoRT.view);
            Renderer.AddDebugImage(normalRT.view);
            Renderer.AddDebugImage(depthRT.view);

            clusterFB = Framebuffer.Create(clusterRP, width, height, 1, new[] { depthRT.view });

            clusterDeferred = Resources.Instance.Load<Shader>("Shaders/ClusterDeferred.shader");
         
            deferredLayout0 = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.CombinedImageSampler, ShaderStage.Fragment),
                new ResourceLayoutBinding(1, DescriptorType.CombinedImageSampler, ShaderStage.Fragment),
                new ResourceLayoutBinding(2, DescriptorType.CombinedImageSampler, ShaderStage.Fragment),
            };

            deferredSet0 = new ResourceSet(deferredLayout0, albedoRT, normalRT, depthRT);
        }

        protected override IEnumerator<FrameGraphPass> CreateRenderPass()
        {
            yield return new ShadowPass();

            geometryPass = new ScenePass("gbuffer")
            {
                PassQueue = PassQueue.EarlyGraphics,                
                RenderPass = geometryRP,
                Framebuffer = geometryFB,
                ClearColorValue = new [] { new ClearColorValue(0.25f, 0.25f, 0.25f, 1), new ClearColorValue(0, 0, 0, 1) },
                Set1 = clusterSet1
            };

            yield return geometryPass;
            
            translucentClustering = new ScenePass("clustering")
            {
                PassQueue = PassQueue.EarlyGraphics,
                RenderPass = clusterRP,
                Framebuffer = clusterFB,
                Set1 = clusterSet1
            };

            yield return translucentClustering;

            lightCull = new ComputePass(ComputeLight);
            yield return lightCull;
           /*
            compositePass = new GraphicsPass("composite")
            {
                RenderPass = Graphics.RenderPass,
                Framebuffers = Graphics.Framebuffers,
                OnDraw = Composite
            };
            yield return compositePass;*/
            
            //var renderPass = Graphics.CreateRenderPass(false, false);
            translucentPass = new ScenePass("cluster_forward")
            {
                //RenderPass = renderPass,
                //Framebuffers = Graphics.CreateSwapChainFramebuffers(renderPass),
                OnDraw = Composite,

                Set1 = resourceSet0,
                Set2 = resourceSet1,
                BlendFlags = BlendFlags.AlphaBlend
            };

            yield return translucentPass;
        }

        void Composite(GraphicsPass graphicsPass, RenderView view)
        {
            var scenePass = graphicsPass as ScenePass;
            scenePass.BeginRenderPass(view);
            var cmd = graphicsPass.CmdBuffer;

            if(cmd == null)
            {
                cmd = graphicsPass.GetCmdBuffer();
                cmd.SetViewport(view.Viewport);
                cmd.SetScissor(view.ViewRect);
            }

            scenePass.DrawFullScreenQuad(clusterDeferred.Main, cmd, deferredSet0, null);

            cmd.End();

            scenePass.DrawScene(view, BlendFlags.AlphaBlend);
            scenePass.EndRenderPass(view);
        }

        protected override void OnBeginSubmit(FrameGraphPass renderPass, CommandBuffer cb, int imageIndex)
        {
            if (renderPass == geometryPass)
            {
                var queryPool = query_pool[imageIndex];
                //cb.WriteTimestamp(PipelineStageFlags.TopOfPipe, queryPool, QUERY_CLUSTERING * 2);

            }
            else if (renderPass == translucentPass)
            {
                var queryPool = query_pool[imageIndex];
                //cb.ResetQueryPool(queryPool, 10, 4);
                //cb.WriteTimestamp(PipelineStageFlags.TopOfPipe, queryPool, QUERY_ONSCREEN * 2);

            }
        }

        protected override void OnEndSubmit(FrameGraphPass renderPass, CommandBuffer cb, int imageIndex)
        {
            if (renderPass == geometryPass)
            {
                var queryPool = query_pool[imageIndex];
                //cb.WriteTimestamp(PipelineStageFlags.FragmentShader, queryPool, QUERY_CLUSTERING * 2 + 1);
            }
            else if (renderPass == translucentPass)
            {
                var queryPool = query_pool[imageIndex];

                //cb.WriteTimestamp(PipelineStageFlags.ColorAttachmentOutput, queryPool, QUERY_ONSCREEN * 2 + 1);

                ClearBuffers(cb, imageIndex);
            }
        }
    }
}
