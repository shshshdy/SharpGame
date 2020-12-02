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
            pass = ShaderUtil.CreatePass("shaders/post/fullscreen.vert", fs);
            pipelineResourceSet = new PipelineResourceSet(pass.PipelineLayout);
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
            var rt = FrameGraphPass.RenderTarget[0];
            pipelineResourceSet.SetResourceSet(0, rt);
        }

        public override void Draw(RenderContext rc, CommandBuffer cmd)
        {
            DrawFullScreenQuad(cmd, FrameGraphPass.RenderPass, subpassIndex, pass, pipelineResourceSet.ResourceSet);
        }

        public void DrawFullScreenQuad(CommandBuffer cb, RenderPass renderPass, uint subpass, Pass pass, Span<DescriptorSet> resourceSet)
        {
            var pipe = pass.GetGraphicsPipeline(renderPass, subpass, null);

            cb.BindPipeline(VkPipelineBindPoint.Graphics, pipe);

            foreach (var rs in resourceSet)
            {
                cb.BindGraphicsResourceSet(pass.PipelineLayout, 0, rs);
            }

            cb.Draw(3, 1, 0, 0);
        }

    }
}
