using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ComputePass : FrameGraphPass
    {
        public Action<ComputePass, CommandBuffer> OnDraw { get; set; }

        public ComputePass()
        {
            Queue = SubmitQueue.Compute;
        }

        public ComputePass(Action<ComputePass, CommandBuffer> onDraw)
        {
            Queue = SubmitQueue.Compute;
            OnDraw = onDraw;
        }

        public override void Draw()
        {
            OnDraw?.Invoke(this, CmdBuffer);
        }
        
    }
}
