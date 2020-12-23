#define HWDEPTH
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
        protected FrameGraphPass ssaoPass;
        protected FrameGraphPass ssaoBlur;
        protected FrameGraphPass compositePass;
        protected FrameGraphPass onscreenPass;
        bool enableSSAO = true;

        public HybridRenderer()
        {
        }

        protected override void CreateResources()
        {
            base.CreateResources();
        }

        protected override void CreateRenderPath()
        {
            this.Add(new ShadowPass());

            geometryPass = new FrameGraphPass(SubmitQueue.EarlyGraphics)
            {
                new AttachmentInfo("albedo", SizeHint.Full, VkFormat.R8G8B8A8UNorm, VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.Sampled),
                new AttachmentInfo("normal", SizeHint.Full, VkFormat.R8G8B8A8UNorm, VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.Sampled),
                new AttachmentInfo(depthTexture.format),

                new SceneSubpass("gbuffer")
                {
                    Set1 = clusterSet1
                }

            };

            geometryPass.renderPassCreator = OnCreateRenderPass;

            this.Add(geometryPass);

            translucentClustering = this.CreateClusteringPass();

            this.Add(translucentClustering);

            if(enableSSAO)
            {
                ssaoPass = new FrameGraphPass
                {
                    new AttachmentInfo("ssao", SizeHint.Full, VkFormat.R8UNorm, VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.Sampled),

                    new SSAOSubpass()
                };

                this.Add(ssaoPass);

                ssaoBlur = new FrameGraphPass
                {
                    new AttachmentInfo("ssao_blur",  SizeHint.Full, VkFormat.R8UNorm, VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.Sampled),

                    new SSAOBlurSubpass()
                };

                this.Add(ssaoBlur);

            }

            lightCull = new ComputePass(ComputeLight);
            this.Add(lightCull);

            var specializationInfo = new SpecializationInfo(new VkSpecializationMapEntry(0, 0, sizeof(uint)));
            specializationInfo.Write(0, 1);

            onscreenPass = new FrameGraphPass
            {
                new AttachmentInfo(Graphics.ColorFormat),
                new AttachmentInfo(depthTexture.format)
                {
                    storeOp = VkAttachmentStoreOp.Store
                },

                new FullScreenSubpass("shaders/glsl/cluster_deferred.frag", specializationInfo)
                {
                    [0, 0] = "global",
                    [3, 0] = "albedo",
                    [3, 1] = "normal",
                    [3, 2] = "depth",
                    [3, 3] = "ssao_blur",

                    resourceSet = new[]
                    {
                        null, resourceSet1, resourceSet2
                    },

                },

                new SceneSubpass("cluster_forward")
                {
                    DisableDepthStencil = false,
                    Set1 = resourceSet1,
                    Set2 = resourceSet2,
                    BlendFlags = BlendFlags.AlphaBlend
                },


            };       

            this.Add(onscreenPass);
        }

        RenderPass OnCreateRenderPass()
        {
            VkFormat depthFormat = Device.GetSupportedDepthFormat();

            VkAttachmentDescription[] attachments =
            {
                new VkAttachmentDescription(VkFormat.R8G8B8A8UNorm, finalLayout : VkImageLayout.ShaderReadOnlyOptimal),
                new VkAttachmentDescription(VkFormat.R8G8B8A8UNorm, finalLayout : VkImageLayout.ShaderReadOnlyOptimal),
                new VkAttachmentDescription(depthFormat, finalLayout :  /*VkImageLayout.ShaderReadOnlyOptimal*/VkImageLayout.DepthStencilReadOnlyOptimal)
            };

            var colorAttachments = new[]
            {
                 new VkAttachmentReference(0, VkImageLayout.ColorAttachmentOptimal),
                 new VkAttachmentReference(1, VkImageLayout.ColorAttachmentOptimal)
            };

            var depthStencilAttachment = new[]
            {
                 new VkAttachmentReference(2, VkImageLayout.DepthStencilAttachmentOptimal)
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
