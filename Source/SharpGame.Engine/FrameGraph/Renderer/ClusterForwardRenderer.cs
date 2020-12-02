using System;
using System.Collections.Generic;
using System.Text;


namespace SharpGame
{
    public class ClusterForwardRenderer : ClusterRenderer
    {
        protected FrameGraphPass clustering;
        protected FrameGraphPass mainPass;

        public ClusterForwardRenderer()
        {
        }

        protected override void CreateRenderPath()
        {
            Add(new ShadowPass());

            clustering = CreateClusteringPass();

            Add(clustering);

            lightCull = new ComputePass(ComputeLight);

            Add(lightCull);

            mainPass = new FrameGraphPass
            {
                new SceneSubpass("cluster_forward")
                {
                    Set1 = resourceSet0,
                    Set2 = resourceSet1,
                }

            };

            mainPass.renderPassCreator = () => Graphics.CreateDefaultRenderPass();       

            Add(mainPass);
        }

        protected override void OnBeginPass(FrameGraphPass renderPass, CommandBuffer cmd)
        {
            int imageIndex = Graphics.WorkContext;

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

        protected override void OnEndPass(FrameGraphPass renderPass, CommandBuffer cmd)
        {
            int imageIndex = Graphics.WorkContext;

            if (renderPass == clustering)
            {
                var queryPool = query_pool[imageIndex];
                //cb.WriteTimestamp(PipelineStageFlags.FragmentShader, queryPool, QUERY_CLUSTERING * 2 + 1);
            }
            else if (renderPass == mainPass)
            {
                var queryPool = query_pool[imageIndex];

                //cb.WriteTimestamp(PipelineStageFlags.ColorAttachmentOutput, queryPool, QUERY_ONSCREEN * 2 + 1);
                ClearBuffers(cmd, imageIndex);

            }
        }

    }
}
