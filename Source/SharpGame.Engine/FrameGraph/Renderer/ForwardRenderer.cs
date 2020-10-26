using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ForwardRenderer : RenderPipeline
    {
        public ForwardRenderer()
        {
            Add(new ShadowPass())
            .Add(new FrameGraphPass
            {
                RenderPass = Graphics.RenderPass,
                Subpasses = new[]
                {
                    new SceneSubpass()
                }
            });
        }

    }
}
