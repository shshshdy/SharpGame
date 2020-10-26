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
            Queue = SubmitQueue.Compute;
        }

        public ComputePass(Action<ComputePass, RenderView> onDraw)
        {
            Queue = SubmitQueue.Compute;
            OnDraw = onDraw;
        }

        public override void Draw(RenderView view)
        {
            OnDraw?.Invoke(this, view);
        }
        
    }
}
