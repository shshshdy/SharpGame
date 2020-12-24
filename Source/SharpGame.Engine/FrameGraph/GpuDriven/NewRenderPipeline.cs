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
                new AttachmentInfo(Graphics.ColorFormat),
                new AttachmentInfo(Graphics.DepthFormat),

                new SceneSubpass
                {
                    DisableDepthStencil = false
                }
                
            });
        }

    }
}
