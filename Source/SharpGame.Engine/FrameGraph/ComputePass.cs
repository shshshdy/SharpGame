using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ComputePass : FrameGraphPass
    {
        public Action<ComputePass, RenderContext, CommandBuffer> OnDraw { get; set; }

        public ComputePass() : base(SubmitQueue.Compute)
        {
        }

        public ComputePass(Action<ComputePass, RenderContext, CommandBuffer> onDraw) : base(SubmitQueue.Compute)
        {
            OnDraw = onDraw;
        }

        public override void Draw(RenderContext rc, CommandBuffer cmd)
        {
            OnDraw?.Invoke(this, rc, cmd);
        }

        protected override void CreateRenderTargets()
        {
        }

        protected override void CreateRenderPass()
        {
        }
    }
}
