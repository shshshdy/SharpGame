//#define HWDEPTH
using Assimp;
using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class HybridRenderer : ClusterRenderer
    {
        protected FrameGraphPass geometryPass;
        protected FrameGraphPass translucentClustering;
        protected FrameGraphPass compositePass;
        protected FrameGraphPass onscreenPass;

        private RenderTexture albedoRT;
        private RenderTexture normalRT;
        private RenderTexture depthRT;
        private RenderTexture depthHWRT;

        protected Shader clusterDeferred;

        Geometry quad;

        DescriptorSetLayout deferredLayout0;
        DescriptorSetLayout deferredLayout1;

        DescriptorSet deferredSet0;
        DescriptorSet deferredSet1;

        public HybridRenderer()
        {
        }

        protected override void CreateResources()
        {
            base.CreateResources();

            //FrameGraph.AddDebugImage(albedoRT.view);
            //FrameGraph.AddDebugImage(normalRT.view);
            //FrameGraph.AddDebugImage(depthHWRT.view);

            clusterDeferred = Resources.Instance.Load<Shader>("Shaders/ClusterDeferred.shader");
            quad = GeometryUtil.CreateUnitQuad();

            deferredLayout0 = new DescriptorSetLayout
            {
                new DescriptorSetLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex),
            };

            deferredSet0 = new DescriptorSet(deferredLayout0, View.ubCameraVS);

            deferredLayout1 = new DescriptorSetLayout
            {
                new DescriptorSetLayoutBinding(0, DescriptorType.CombinedImageSampler, ShaderStage.Fragment),
                new DescriptorSetLayoutBinding(1, DescriptorType.CombinedImageSampler, ShaderStage.Fragment),
                new DescriptorSetLayoutBinding(2, DescriptorType.CombinedImageSampler, ShaderStage.Fragment),
            };

        }

        protected override void CreateRenderPath()
        {
            this.Add(new ShadowPass());

            geometryPass = new FrameGraphPass(SubmitQueue.EarlyGraphics)
            {
                renderPassCreator = OnCreateRenderPass,
                frameBufferCreator = OnCreateFramebuffer,

                ClearColorValue = new[] 
                { 
                    new ClearColorValue(0.25f, 0.25f, 0.25f, 1),
                    new ClearColorValue(0, 0, 0, 1),
                    new ClearColorValue(0, 0, 0, 0)
                },

                Subpasses = new[]
                {
                    new SceneSubpass("gbuffer")
                    {
                        Set1 = clusterSet1
                    }
                }
            };

            this.Add(geometryPass);

            uint width = (uint)Graphics.Width;
            uint height = (uint)Graphics.Height;
            translucentClustering = new FrameGraphPass(SubmitQueue.EarlyGraphics)
            {
                renderPassCreator = OnCreateClusterRenderPass,

                frameBufferCreator = (rp) => {
                    var clusterFB = Framebuffer.Create(rp, width, height, 1, new[] { depthHWRT.imageView });
                    return new Framebuffer[3] { clusterFB, clusterFB, clusterFB };
                },

                Subpasses = new[]
                {
                    new SceneSubpass("clustering")
                    {
                        Set1 = clusterSet1
                    }
                }
            };

            this.Add(translucentClustering);

            lightCull = new ComputePass(ComputeLight);
            this.Add(lightCull);
            
            onscreenPass = new FrameGraphPass
            {
                renderPassCreator = () => Graphics.CreateRenderPass(false, false),
                frameBufferCreator = (rp) => Graphics.CreateSwapChainFramebuffers(rp),

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

            this.Add(onscreenPass);
        }

        RenderPass OnCreateRenderPass()
        {
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

            return new RenderPass(attachments, subpassDescription, dependencies);
        
        }

        Framebuffer[] OnCreateFramebuffer(RenderPass rp)
        {
            uint width = (uint)Graphics.Width;
            uint height = (uint)Graphics.Height;
            Format depthFormat = Device.GetSupportedDepthFormat();

            albedoRT = new RenderTexture(width, height, 1, Format.R8g8b8a8Unorm,
                        ImageUsageFlags.ColorAttachment | ImageUsageFlags.Sampled, 
                        SampleCountFlags.Count1, ImageLayout.ColorAttachmentOptimal);

            normalRT = new RenderTexture(width, height, 1, Format.R8g8b8a8Unorm,
                        ImageUsageFlags.ColorAttachment | ImageUsageFlags.Sampled,
                        SampleCountFlags.Count1, ImageLayout.ColorAttachmentOptimal);

            depthRT = new RenderTexture(width, height, 1, Format.R32g32b32a32Sfloat,
                        ImageUsageFlags.ColorAttachment | ImageUsageFlags.Sampled,
                        SampleCountFlags.Count1, ImageLayout.ColorAttachmentOptimal);

            depthHWRT =/* Graphics.DepthRT; */new RenderTexture(width, height, 1, depthFormat,
                        ImageUsageFlags.DepthStencilAttachment | ImageUsageFlags.Sampled, /*ImageAspectFlags.Depth | ImageAspectFlags.Stencil,*/
                        SampleCountFlags.Count1, ImageLayout.DepthStencilReadOnlyOptimal
                        );

            var geometryFB = Framebuffer.Create(rp, width, height, 1, new[] { albedoRT.imageView, normalRT.imageView, depthRT.imageView, depthHWRT.imageView });

#if HWDEPTH
            deferredSet1 = new ResourceSet(deferredLayout1, albedoRT, normalRT, depthHWRT);
#else
            deferredSet1 = new DescriptorSet(deferredLayout1, albedoRT, normalRT, depthRT);
#endif
            return new Framebuffer[] { geometryFB, geometryFB, geometryFB };

        }

        void Composite(GraphicsSubpass graphicsPass, RenderContext rc, CommandBuffer cmd)
        {
            var scenePass = graphicsPass as SceneSubpass;
            var pass = clusterDeferred.Main;

            Span<DescriptorSet> sets = new []
            {
                deferredSet0,
                resourceSet0,
                resourceSet1,
                deferredSet1,
            };

            cmd.DrawGeometry(quad, pass, 0, sets);

            scenePass.DrawScene(cmd, BlendFlags.AlphaBlend);

        }

        protected override void OnBeginPass(FrameGraphPass renderPass, CommandBuffer cmd)
        {
            int workContext = Graphics.WorkContext;
            if (renderPass == geometryPass)
            {
                var queryPool = query_pool[workContext];
                //cb.WriteTimestamp(PipelineStageFlags.TopOfPipe, queryPool, QUERY_CLUSTERING * 2);

            }
            else if (renderPass == onscreenPass)
            {
                var queryPool = query_pool[workContext];
                //cb.ResetQueryPool(queryPool, 10, 4);
                //cb.WriteTimestamp(PipelineStageFlags.TopOfPipe, queryPool, QUERY_ONSCREEN * 2);

            }
        }

        protected override void OnEndPass(FrameGraphPass renderPass, CommandBuffer cmd)
        {
            int workContext = Graphics.WorkContext;

            if (renderPass == geometryPass)
            {
                var queryPool = query_pool[workContext];
                //cb.WriteTimestamp(PipelineStageFlags.FragmentShader, queryPool, QUERY_CLUSTERING * 2 + 1);
            }
            else if (renderPass == onscreenPass)
            {
                var queryPool = query_pool[workContext];

                //cb.WriteTimestamp(PipelineStageFlags.ColorAttachmentOutput, queryPool, QUERY_ONSCREEN * 2 + 1);

                ClearBuffers(cmd, workContext);
            }
        }
    }
}
