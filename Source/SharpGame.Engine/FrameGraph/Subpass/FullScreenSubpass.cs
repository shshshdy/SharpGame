using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class FullScreenSubpass : Subpass
    {
        protected Pass pass;
        protected PipelineResourceSet pipelineResourceSet;
        public FullScreenSubpass(string fs)
        {
            pass = new Pass("shaders/common/fullscreen.vert", fs);
        }

        public override void DeviceReset()
        {
            BindResources();
        }

        protected virtual void CreateResources()
        {
        }

        protected virtual void BindResources()
        { 
        }

        public void DrawFullScreenQuad(CommandBuffer cb, RenderPass renderPass, uint subpass, Pass pass, Span<ResourceSet> resourceSet)
        {
            var pipe = pass.GetGraphicsPipeline(FrameGraphPass.RenderPass, subpass, null);

            cb.BindPipeline(PipelineBindPoint.Graphics, pipe);

            foreach (var rs in resourceSet)
            {
                cb.BindGraphicsResourceSet(pass.PipelineLayout, 0, rs);
            }

            cb.Draw(3, 1, 0, 0);
        }

    }
}
