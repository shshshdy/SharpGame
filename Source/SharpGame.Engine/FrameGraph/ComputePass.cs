using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ComputePass : FrameGraphPass
    {
        public Action<ComputePass, RenderView> OnDraw { get; set; }

        CommandBuffer[] commandBuffers = new CommandBuffer[3];
        public ComputePass()
        {
        }

        public override void Draw(RenderView view)
        {
            cmdBuffer = Graphics.Instance.WorkComputeBuffer;
            commandBuffers[Graphics.Instance.WorkContext] = cmdBuffer;
            OnDraw?.Invoke(this, view);            
        }
        
        public override void Submit(int imageIndex)
        {
            var g = Graphics.Instance;
            g.submitComputeCmdBuffer = commandBuffers[g.RenderContext];
            commandBuffers[Graphics.Instance.RenderContext] = null;
        }
    }
}
