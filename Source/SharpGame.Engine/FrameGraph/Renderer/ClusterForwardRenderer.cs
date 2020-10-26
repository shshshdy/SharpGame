using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class ClusterForwardRenderer : ClusterRenderer
    {
        protected Framebuffer clusterFB;

        protected FrameGraphPass clustering;
        protected FrameGraphPass mainPass;

        public ClusterForwardRenderer()
        {
        }

        protected override void CreateResources()
        {
            base.CreateResources();

            uint width = (uint)Graphics.Width;
            uint height = (uint)Graphics.Height;

            var depthRT = Graphics.DepthRT;// new RenderTarget(width, height, 1, depthFormat, ImageUsageFlags.DepthStencilAttachment | ImageUsageFlags.Sampled, ImageAspectFlags.Depth, SampleCountFlags.Count1, ImageLayout.ShaderReadOnlyOptimal);
            clusterFB = Framebuffer.Create(clusterRP, width, height, 1, new[] { depthRT.view });

            //FrameGraph.AddDebugImage(depthRT.view);

        }

        protected override IEnumerator<FrameGraphPass> CreateRenderPass()
        {
            yield return new ShadowPass();

            clustering = new FrameGraphPass
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

            yield return clustering;

            lightCull = new ComputePass(ComputeLight);
            yield return lightCull;

            mainPass = new FrameGraphPass
            {
            //#if NO_DEPTHWRITE
                RenderPass = Graphics.RenderPass,// Graphics.CreateRenderPass(false, false),
            //#endif
                OnEnd = (cb) => ClearBuffers(cb, Graphics.WorkContext),

                Subpasses = new[]
                {
                    new SceneSubpass("cluster_forward")
                    {
                        Set1 = resourceSet0,
                        Set2 = resourceSet1,
                    }
                }
            };

            yield return mainPass;
        }

        protected override void OnBeginPass(FrameGraphPass renderPass)
        {
            CommandBuffer cb = renderPass.CmdBuffer;
            int imageIndex = Graphics.WorkImage;

            if (renderPass == clustering)
            {
                 var queryPool = query_pool[imageIndex];
                 //cb.ResetQueryPool(queryPool, 0, 4);
                 //cb.WriteTimestamp(PipelineStageFlags.TopOfPipe, queryPool, QUERY_CLUSTERING * 2);

            }
            else if (renderPass == mainPass)
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

            if (renderPass == clustering)
            {
                var queryPool = query_pool[imageIndex];
                //cb.WriteTimestamp(PipelineStageFlags.FragmentShader, queryPool, QUERY_CLUSTERING * 2 + 1);
            }
            else if (renderPass == mainPass)
            {
                var queryPool = query_pool[imageIndex];

                //cb.WriteTimestamp(PipelineStageFlags.ColorAttachmentOutput, queryPool, QUERY_ONSCREEN * 2 + 1);

            }
        }

    }
}
