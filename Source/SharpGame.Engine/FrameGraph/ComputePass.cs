using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ComputePass : FrameGraphPass
    {
        public Action<ComputePass, RenderView> OnDraw { get; set; }

        public ComputePass()
        {
            PassQueue = PassQueue.Compute;
        }

        public override void Draw(RenderView view)
        {
            cmdBuffer = Renderer.WorkComputeCmdBuffer;
            OnDraw?.Invoke(this, view);
            cmdBuffer = null;
        }
        
    }
}
