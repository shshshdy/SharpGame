//#define HWDEPTH
using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class HybridRenderer : ClusterRenderer
    {
        private RenderTexture albedoRT;
        private RenderTexture normalRT;
        private RenderTexture depthRT;
        private RenderTexture depthHWRT;

        private Framebuffer geometryFB;
        private RenderPass geometryRP;

        protected Framebuffer clusterFB;

        protected FrameGraphPass geometryPass;
        protected FrameGraphPass translucentClustering;
        protected FrameGraphPass compositePass;
        protected FrameGraphPass translucentPass;
        protected Shader clusterDeferred;

        Geometry quad;

        ResourceLayout deferredLayout0;
        ResourceLayout deferredLayout1;

        ResourceSet[] deferredSet0 = new ResourceSet[3];
        ResourceSet[] deferredSet1 = new ResourceSet[3];

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
                new AttachmentDescription(Format.R32g32b32a32Sfloat, finalLayout : ImageLayout.ShaderReadOnlyOptimal),
                new AttachmentDescription(depthFormat, finalLayout : ImageLayout.DepthStencilReadOnlyOptimal)
            };

            var colorAttachments = new[]
            {
                 new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal),
                 new AttachmentReference(1, ImageLayout.ColorAttachmentOptimal),
                 new AttachmentReference(2, ImageLayout.ColorAttachmentOptimal)
            };

            var depthStencilAttachment = new[]
            {
                 new AttachmentReference(3, ImageLayout.DepthStencilAttachmentOptimal)
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

            albedoRT = new RenderTexture(width, height, 1, Format.R8g8b8a8Unorm,
                        ImageUsageFlags.ColorAttachment | ImageUsageFlags.Sampled, ImageAspectFlags.Color,
                        SampleCountFlags.Count1, ImageLayout.ColorAttachmentOptimal);

            normalRT = new RenderTexture(width, height, 1, Format.R8g8b8a8Unorm,
                        ImageUsageFlags.ColorAttachment | ImageUsageFlags.Sampled, ImageAspectFlags.Color,
                        SampleCountFlags.Count1, ImageLayout.ColorAttachmentOptimal);

            depthRT = new RenderTexture(width, height, 1, Format.R32g32b32a32Sfloat,
                        ImageUsageFlags.ColorAttachment | ImageUsageFlags.Sampled, ImageAspectFlags.Color,
                        SampleCountFlags.Count1, ImageLayout.ColorAttachmentOptimal);


            depthHWRT =/* Graphics.DepthRT; */new RenderTexture(width, height, 1, depthFormat,
                        ImageUsageFlags.DepthStencilAttachment | ImageUsageFlags.Sampled, ImageAspectFlags.Depth /*| ImageAspectFlags.Stencil*/,
                        SampleCountFlags.Count1, ImageLayout.DepthStencilReadOnlyOptimal
                        );

            geometryFB = Framebuffer.Create(geometryRP, width, height, 1, new[] { albedoRT.view, normalRT.view, depthRT.view, depthHWRT.view });

            FrameGraph.AddDebugImage(albedoRT.view);
            FrameGraph.AddDebugImage(normalRT.view);
            //FrameGraph.AddDebugImage(depthHWRT.view);

            clusterFB = Framebuffer.Create(clusterRP, width, height, 1, new[] { depthHWRT.view });

            clusterDeferred = Resources.Instance.Load<Shader>("Shaders/ClusterDeferred.shader");
            quad = GeometricPrimitive.CreateUnitQuad();

            deferredLayout0 = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex),
            };

            deferredSet0[0] =  new ResourceSet(deferredLayout0, View.ubCameraVS[0]);
            deferredSet0[1] = new ResourceSet(deferredLayout0, View.ubCameraVS[1]);
            deferredSet0[2] = new ResourceSet(deferredLayout0, View.ubCameraVS[2]);

            deferredLayout1 = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.CombinedImageSampler, ShaderStage.Fragment),
                new ResourceLayoutBinding(1, DescriptorType.CombinedImageSampler, ShaderStage.Fragment),
                new ResourceLayoutBinding(2, DescriptorType.CombinedImageSampler, ShaderStage.Fragment),
            };
#if HWDEPTH
            deferredSet1[0] = deferredSet1[1] = deferredSet1[2] = new ResourceSet(deferredLayout1, albedoRT, normalRT, depthHWRT);
#else
            deferredSet1[0] = deferredSet1[1] = deferredSet1[2] = new ResourceSet(deferredLayout1, albedoRT, normalRT, depthRT);
#endif

        }

        protected override IEnumerator<FrameGraphPass> CreateRenderPass()
        {
            yield return new ShadowPass();

            geometryPass = new FrameGraphPass
            {
                PassQueue = PassQueue.EarlyGraphics,
                RenderPass = geometryRP,
                Framebuffer = geometryFB,
                ClearColorValue = new[] { new ClearColorValue(0.25f, 0.25f, 0.25f, 1), new ClearColorValue(0, 0, 0, 1), new ClearColorValue(0, 0, 0, 0) },
                Subpasses = new []
                {
                    new SceneSubpass("gbuffer")
                    { Set1 = clusterSet1
                    }
                }
            };

            yield return geometryPass;
           
            translucentClustering = new FrameGraphPass
            {
                PassQueue = PassQueue.EarlyGraphics,
                RenderPass = clusterRP,
                Framebuffer = clusterFB,
                Subpasses = new[]
                {
                    new SceneSubpass("clustering")
                    {
                        Set1 = clusterSet1
                    }
                }
            };

            yield return translucentClustering;

            lightCull = new ComputePass(ComputeLight);
            yield return lightCull;
            /*
             compositePass = new GraphicsSubpass("composite")
             {
                 RenderPass = Graphics.RenderPass,
                 Framebuffers = Graphics.Framebuffers,
                 OnDraw = Composite
             };
             yield return compositePass;*/

            var renderPass = Graphics.CreateRenderPass(false, false);
            translucentPass = new FrameGraphPass
            {
                RenderPass = renderPass,
                Framebuffers = Graphics.CreateSwapChainFramebuffers(renderPass),

                OnEnd = (cb) => ClearBuffers(cb, Graphics.WorkContext),

                Subpasses = new[]
                {
                    new SceneSubpass("cluster_forward")
                    {
                        OnDraw = Composite,

                        Set1 = resourceSet0,
                        Set2 = resourceSet1,
                        BlendFlags = BlendFlags.AlphaBlend
                    }
                }
            };

            yield return translucentPass;
        }

        void Composite(GraphicsSubpass graphicsPass, RenderView view)
        {
            var scenePass = graphicsPass as SceneSubpass;

            var cmd = graphicsPass.CmdBuffer;

            var pass = clusterDeferred.Main;

            Span<ResourceSet> sets = new []
            {
                deferredSet0[Graphics.WorkContext],
                resourceSet0[Graphics.WorkContext],
                resourceSet1[Graphics.WorkContext],
                deferredSet1[Graphics.WorkContext],
            };

            cmd.DrawGeometry(quad, pass, 0, sets);

            scenePass.DrawScene(view, BlendFlags.AlphaBlend);

        }

        protected override void OnBeginPass(FrameGraphPass renderPass)
        {
            CommandBuffer cb = renderPass.CmdBuffer;
            int imageIndex = Graphics.WorkImage;
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

        protected override void OnEndPass(FrameGraphPass renderPass)
        {
            CommandBuffer cb = renderPass.CmdBuffer;
            int imageIndex = Graphics.WorkImage;

            if (renderPass == geometryPass)
            {
                var queryPool = query_pool[imageIndex];
                //cb.WriteTimestamp(PipelineStageFlags.FragmentShader, queryPool, QUERY_CLUSTERING * 2 + 1);
            }
            else if (renderPass == translucentPass)
            {
                var queryPool = query_pool[imageIndex];

                //cb.WriteTimestamp(PipelineStageFlags.ColorAttachmentOutput, queryPool, QUERY_ONSCREEN * 2 + 1);

                //ClearBuffers(cb, imageIndex);
            }
        }
    }
}
