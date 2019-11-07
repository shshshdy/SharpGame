using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class HybridRenderer : ClusterRenderer
    {
        protected RenderTarget depthRT;
        protected RenderPass clusterRP;
        protected Framebuffer clusterFB;

        public HybridRenderer()
        {
        }

        protected override IEnumerator<FrameGraphPass> CreateRenderPass()
        {
            yield return new ShadowPass();

            clusterPass = new ScenePass("gbuffer")
            {
                PassQueue = PassQueue.EarlyGraphics,                
                RenderPass = clusterRP,
                Framebuffer = clusterFB,
                Set1 = clusterSet1
            };

            yield return clusterPass;

            lightPass = new ComputePass(ComputeLight);
            yield return lightPass;

            mainPass = new ScenePass("cluster_forward")
            {
#if NO_DEPTHWRITE
                RenderPass = Graphics.CreateRenderPass(true, false),
#endif
                Set1 = resourceSet0,
                Set2 = resourceSet1,
            };

            yield return mainPass;
        }

    }
}
