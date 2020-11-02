using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ComputePass : FrameGraphPass
    {
        public Action<ComputePass, RenderContext, CommandBuffer> OnDraw { get; set; }

        public ComputePass()
        {
            Queue = SubmitQueue.Compute;
        }

        public ComputePass(Action<ComputePass, RenderContext, CommandBuffer> onDraw)
        {
            Queue = SubmitQueue.Compute;
            OnDraw = onDraw;
        }

        public override void Draw(RenderContext rc, CommandBuffer cmd)
        {
            OnDraw?.Invoke(this, rc, cmd);
        }
        
    }
}
