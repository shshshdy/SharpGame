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
        }

        public override void Draw(RenderView view)
        {
            cmdBuffer = Graphics.Instance.ComputeCmdBuffer;
            OnDraw?.Invoke(this, view);
            //cmdBuffer = null;
        }
        
        public override void Submit(int imageIndex)
        {
            var g = Graphics.Instance;
            CommandBuffer cb = g.ComputeCmdBuffer;
            int renderContext = g.RenderContext;
            g.submitComputeCmdBuffer = cmdBuffer;
            //cb.ExecuteCommand(cmdBufferPool[renderContext].CommandBuffers[0]);

        }
    }
}
