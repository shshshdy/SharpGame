using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public partial class ClusterRenderer : RenderPipeline
    {      
        protected RenderTarget depthRT;
        protected RenderPass clusterRP;
        protected Framebuffer clusterFB;
        protected Format depthFormat = Device.GetSupportedDepthFormat();
        protected ResourceLayout clusterLayout1;
        protected ResourceSet[] clusterSet1 = new ResourceSet[2];

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

            clusterLayout1 = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Fragment),
                new ResourceLayoutBinding(1, DescriptorType.StorageTexelBuffer, ShaderStage.Fragment),
            };

            clusterSet1[0] = new ResourceSet(clusterLayout1, uboCluster[0], grid_flags);
            clusterSet1[1] = new ResourceSet(clusterLayout1, uboCluster[1], grid_flags);
        }

    }
}
