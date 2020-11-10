using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ForwardRenderer : RenderPipeline
    {
        public ForwardRenderer()
        {
            Add(new ShadowPass());
            Add(new FrameGraphPass
            {
                renderPassCreator = () => Graphics.RenderPass,
                frameBufferCreator = (rp) => Graphics.Framebuffers,

                Subpasses = new[]
                {
                    new SceneSubpass()
                }
            });
        }

    }
}
