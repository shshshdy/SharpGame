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
       
        RenderTarget rtDepth;
        RenderPass rpEarlyZ;
        Framebuffer fbEarlyZ;

        Format depthFormat = Format.D16Unorm;
        ResourceLayout clusteringSet1;
        ResourceSet[] set1 = new ResourceSet[2];

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

            rtDepth = new RenderTarget(width, height, 1, depthFormat, ImageUsageFlags.DepthStencilAttachment, ImageAspectFlags.Depth, SampleCountFlags.Count1);

            fbEarlyZ = Framebuffer.Create(rpEarlyZ, width, height, 1, new[] { rtDepth.view });

            earlyZPass.RenderPass = rpEarlyZ;
            earlyZPass.Framebuffer= fbEarlyZ;

            Renderer.AddDebugImage(rtDepth.view);


            clusteringSet1 = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Fragment),
                new ResourceLayoutBinding(1, DescriptorType.StorageTexelBuffer, ShaderStage.Fragment),
            };

            set1[0] = new ResourceSet(clusteringSet1, uboCluster[0], grid_flags);
            set1[1] = new ResourceSet(clusteringSet1, uboCluster[1], grid_flags);
        }

        void DrawEarlyZ(GraphicsPass renderPass, RenderView view)
        {
            var cmd = renderPass.CmdBuffer;
            var batches = view.batches[0];
            foreach (var batch in batches)
            {
                renderPass.DrawBatch(cmd, batch, default, view.Set0, set1[Graphics.WorkContext]);
            }

            //renderPass.DrawBatches(view, view.batches[0], renderPass.CmdBuffer, view.Set0, set1[Graphics.WorkContext]);
        }
    }
}
