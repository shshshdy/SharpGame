using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;


namespace SharpGame
{
    public class FrameGraph : Resource
    {
        public Dictionary<string, Framebuffer> FrameBuffers = new Dictionary<string, Framebuffer>();
        public List<RenderPass> RenderPasses { get; set; } = new List<RenderPass>();

        public FrameGraph()
        {
        }

        public void AddRenderPass(RenderPass renderPass)
        {
            renderPass.RenderPath = this;
            RenderPasses.Add(renderPass);
        }

        public void Draw(RenderView view)
        {

            foreach (var renderPass in RenderPasses)
            {
                renderPass.Draw(view);
            }
            
        }

        public void Summit(int imageIndex)
        {
            foreach (var renderPass in RenderPasses)
            {
                renderPass.Summit(imageIndex);
            }

        }

    }

}
