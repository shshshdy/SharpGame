using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public partial class ClusterRenderer : RenderPipeline
    {
        const uint ATTACHMENT_REFERENCE_DEPTH = 0;
        const uint SUBPASS_DEPTH = 0;
        const uint SUBPASS_CLUSTER_FLAG = 1;
       
        RenderTarget depthRT;
        RenderPass clusterRP;
        Framebuffer clusterFB;

        Format depthFormat = Device.GetSupportedDepthFormat();// Format.D16Unorm;

        
        ResourceLayout clusterLayout1;
        ResourceSet[] clusterSet1 = new ResourceSet[2];

        private void InitCluster()
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

            clusterPass.RenderPass = clusterRP;
            clusterPass.Framebuffer= clusterFB;

            //Renderer.AddDebugImage(rtDepth.view);

            clusterLayout1 = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Fragment),
                new ResourceLayoutBinding(1, DescriptorType.StorageTexelBuffer, ShaderStage.Fragment),
            };

            clusterSet1[0] = new ResourceSet(clusterLayout1, uboCluster[0], grid_flags);
            clusterSet1[1] = new ResourceSet(clusterLayout1, uboCluster[1], grid_flags);
        }

        unsafe void DrawEarlyZ(GraphicsPass renderPass, RenderView view)
        {
            var cmd = renderPass.CmdBuffer;
            var batches = view.batches[0];
            var pass_id = Pass.GetID(Pass.EarlyZ);

            cmd.ResetQueryPool(QueryPool, 0, 2);

            foreach (var batch in batches)
            {
                renderPass.DrawBatch(pass_id, cmd, batch, default, view.Set0, null/*clusteringSet1[Graphics.WorkContext]*/);
            }

        }

        void DrawClustering(GraphicsPass renderPass, RenderView view)
        {
            var cmd = renderPass.CmdBuffer;
            var batches = view.batches[0];

            //cmd.ResetQueryPool(QueryPool, 2, 2);

            cmd.WriteTimestamp(PipelineStageFlags.TopOfPipe, QueryPool, QUERY_CLUSTERING * 2);

            var pass_id = Pass.GetID("clustering");

            foreach (var batch in batches)
            {
                renderPass.DrawBatch(pass_id, cmd, batch, default, view.Set0, clusterSet1[Graphics.WorkContext]);
            }

            foreach (var batch in view.batches[1])
            {
                renderPass.DrawBatch(pass_id, cmd, batch, default, view.Set0, clusterSet1[Graphics.WorkContext]);
            }

            foreach (var batch in view.batches[2])
            {
                renderPass.DrawBatch(pass_id, cmd, batch, default, view.Set0, clusterSet1[Graphics.WorkContext]);
            }

            cmd.WriteTimestamp(PipelineStageFlags.FragmentShader, QueryPool, QUERY_CLUSTERING * 2 + 1);

        }
    }
}
