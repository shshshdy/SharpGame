using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ClusterLighting : ScenePass
    {
        EarlyZPass earlyZPass;
        LightComputePass lightComputePass;
        public ClusterLighting()
        {

        }

        public override void Init()
        {
            base.Init();

            earlyZPass = FrameGraph.Get<EarlyZPass>();
            lightComputePass = FrameGraph.Get<LightComputePass>();
        }




    }
}
