using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class PostProcessPass : FrameGraphPass
    {

        public void DrawFullScreenQuad(CommandBuffer cb, Material material)
        {
            var shader = material.Shader;
            var pass = shader.GetPass(passID);
            var pipe = pass.GetGraphicsPipeline(RenderPass, Subpass, null);

            cb.BindPipeline(PipelineBindPoint.Graphics, pipe);

            material.Bind(pass.passIndex, cb);

            cb.Draw(3, 1, 0, 0);
        }
    }
}
