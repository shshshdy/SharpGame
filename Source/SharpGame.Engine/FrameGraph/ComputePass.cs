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
            PassQueue = PassQueue.Compute;
        }

        public override void Draw(RenderView view)
        {
            cmdBuffer = Graphics.WorkComputeCmdBuffer;
            commandBuffers[Graphics.WorkContext] = cmdBuffer;
            OnDraw?.Invoke(this, view);            
        }
        
        public override void Submit(CommandBuffer cb, int imageIndex)
        {
            if (commandBuffers[Graphics.RenderContext] != null)
            {
                Graphics.SubmitComputeCmdBuffer(commandBuffers[Graphics.RenderContext]);
                commandBuffers[Graphics.RenderContext] = null;
            }
        }
    }
}
