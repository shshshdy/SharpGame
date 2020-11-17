using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class NewRenderPipeline : RenderPipeline
    {
        public NewRenderPipeline()
        {
            Add(new ShadowPass());
            Add(new FrameGraphPass
            {
                //renderPassCreator = () => Graphics.RenderPass,
                //frameBufferCreator = (rp) => Graphics.Framebuffers,
                new RenderTextureInfo(Graphics.Swapchain),

                new SceneSubpass()
                
            });
        }

    }
}
