//#define HWDEPTH
using Assimp;
using System;
using System.Collections.Generic;
using System.Text;


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
        private RenderTexture positionRT;
        private RenderTexture depthHWRT;

        protected Shader clusterDeferred;

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

            deferredLayout0 = new DescriptorSetLayout
            {
                new DescriptorSetLayoutBinding(0, VkDescriptorType.UniformBuffer, VkShaderStageFlags.All),
            };

            deferredSet0 = new DescriptorSet(deferredLayout0, View.ubGlobal);

            deferredLayout1 = new DescriptorSetLayout
            {
                new DescriptorSetLayoutBinding(0, VkDescriptorType.CombinedImageSampler, VkShaderStageFlags.Fragment),
                new DescriptorSetLayoutBinding(1, VkDescriptorType.CombinedImageSampler, VkShaderStageFlags.Fragment),
                new DescriptorSetLayoutBinding(2, VkDescriptorType.CombinedImageSampler, VkShaderStageFlags.Fragment),
            };

        }

        protected override void CreateRenderPath()
        {
            this.Add(new ShadowPass());

            geometryPass = new FrameGraphPass(SubmitQueue.EarlyGraphics)
            {
                //new RenderTextureInfo(width, height, 1, VkFormat.R8G8B8A8UNorm, ImageUsageFlags.ColorAttachment | ImageUsageFlags.Sampled),
                //new RenderTextureInfo(width, height, 1, VkFormat.R8G8B8A8UNorm, ImageUsageFlags.ColorAttachment | ImageUsageFlags.Sampled),
                //new RenderTextureInfo(width, height, 1, VkFormat.R32G32B32A32SFloat, ImageUsageFlags.ColorAttachment | ImageUsageFlags.Sampled),
                //new RenderTextureInfo(width, height, 1, depthFormat, ImageUsageFlags.DepthStencilAttachment | ImageUsageFlags.Sampled),

                new SceneSubpass("gbuffer")
                {
                    Set1 = clusterSet1
                }

            };

            geometryPass.clearValues = new VkClearValue[]
            {
                new VkClearColorValue(0.25f, 0.25f, 0.25f, 1),
                new VkClearColorValue(0, 0, 0, 1),
                new VkClearColorValue(0, 0, 0, 0),
                new VkClearDepthStencilValue(1, 0)
            };

            geometryPass.frameBufferCreator = OnCreateFramebuffer;
            geometryPass.renderPassCreator = OnCreateRenderPass;

            this.Add(geometryPass);

            translucentClustering = this.CreateClusteringPass();

            this.Add(translucentClustering);

            lightCull = new ComputePass(ComputeLight);
            this.Add(lightCull);
            
            onscreenPass = new FrameGraphPass
            {
                new SceneSubpass("cluster_forward")
                {
                    OnDraw = Composite,

                    Set1 = resourceSet0,
                    Set2 = resourceSet1,
                    BlendFlags = BlendFlags.AlphaBlend
                }

            };

            onscreenPass.renderPassCreator = () => Graphics.CreateRenderPass(false, false);
            onscreenPass.frameBufferCreator = (rp) => Graphics.CreateSwapChainFramebuffers(rp);

            this.Add(onscreenPass);
        }

        RenderPass OnCreateRenderPass()
        {
            VkFormat depthFormat = Device.GetSupportedDepthFormat();

            VkAttachmentDescription[] attachments =
            {
                new VkAttachmentDescription(VkFormat.R8G8B8A8UNorm, finalLayout : VkImageLayout.ShaderReadOnlyOptimal),
                new VkAttachmentDescription(VkFormat.R8G8B8A8UNorm, finalLayout : VkImageLayout.ShaderReadOnlyOptimal),
                new VkAttachmentDescription(VkFormat.R32G32B32A32SFloat, finalLayout : VkImageLayout.ShaderReadOnlyOptimal),
                new VkAttachmentDescription(depthFormat, finalLayout : VkImageLayout.DepthStencilReadOnlyOptimal)
            };

            var colorAttachments = new[]
            {
                 new VkAttachmentReference(0, VkImageLayout.ColorAttachmentOptimal),
                 new VkAttachmentReference(1, VkImageLayout.ColorAttachmentOptimal),
                 new VkAttachmentReference(2, VkImageLayout.ColorAttachmentOptimal)
            };

            var depthStencilAttachment = new[]
            {
                 new VkAttachmentReference(3, VkImageLayout.DepthStencilAttachmentOptimal)
            };

            SubpassDescription[] subpassDescription =
            {
		        // clustering subpass
                new SubpassDescription
                {
                    pipelineBindPoint = VkPipelineBindPoint.Graphics,
                    pColorAttachments = colorAttachments,
                    pDepthStencilAttachment = depthStencilAttachment
                },
            };

            // Subpass dependencies for layout transitions
            VkSubpassDependency[] dependencies =
            {
                new VkSubpassDependency
                {
                    srcSubpass = Vulkan.SubpassExternal,
                    dstSubpass = 0,
                    srcStageMask = VkPipelineStageFlags.BottomOfPipe,
                    dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                    srcAccessMask = VkAccessFlags.MemoryRead,
                    dstAccessMask = VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite,
                    dependencyFlags = VkDependencyFlags.ByRegion
                },

                new VkSubpassDependency
                {
                    srcSubpass = 0,
                    dstSubpass = Vulkan.SubpassExternal,
                    srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                    dstStageMask = VkPipelineStageFlags.BottomOfPipe,
                    srcAccessMask = VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite,
                    dstAccessMask = VkAccessFlags.MemoryRead,
                    dependencyFlags = VkDependencyFlags.ByRegion
                },
            };

            return new RenderPass(attachments, subpassDescription, dependencies);
        
        }

        Framebuffer[] OnCreateFramebuffer(RenderPass rp)
        {
            uint width = (uint)Graphics.Width;
            uint height = (uint)Graphics.Height;
            VkFormat depthFormat = Device.GetSupportedDepthFormat();

            albedoRT = new RenderTexture(width, height, 1, VkFormat.R8G8B8A8UNorm, VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.Sampled);
            normalRT = new RenderTexture(width, height, 1, VkFormat.R8G8B8A8UNorm, VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.Sampled);
            positionRT = new RenderTexture(width, height, 1, VkFormat.R32G32B32A32SFloat, VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.Sampled);
            depthHWRT = new RenderTexture(width, height, 1, depthFormat, VkImageUsageFlags.DepthStencilAttachment | VkImageUsageFlags.Sampled);

            var geometryFB = new Framebuffer(rp, width, height, 1, new[] { albedoRT.imageView, normalRT.imageView, positionRT.imageView, depthHWRT.imageView });
#if HWDEPTH
            deferredSet1 = new ResourceSet(deferredLayout1, albedoRT, normalRT, depthHWRT);
#else
            deferredSet1 = new DescriptorSet(deferredLayout1, albedoRT, normalRT, positionRT);
#endif
            return new Framebuffer[] { geometryFB, geometryFB, geometryFB };

        }

        void Composite(GraphicsSubpass graphicsPass, RenderContext rc, CommandBuffer cmd)
        {
            var scenePass = graphicsPass as SceneSubpass;
            var pass = clusterDeferred.Main;

            Span<DescriptorSet> sets = new []
            {
                resourceSet0,
                resourceSet1,
                deferredSet1,
            };

            Span<uint> offset = new uint[] {0};
            cmd.DrawFullScreenQuad(pass, 0, View.Set0, offset, sets);

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
