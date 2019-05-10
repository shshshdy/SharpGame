using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class RenderPath : Resource
    {
        public Dictionary<string, Framebuffer> FrameBuffers = new Dictionary<string, Framebuffer>();
        public List<RenderPass> RenderPasses { get; set; } = new List<RenderPass>();

        public RenderPath()
        {
        }

        public void AddRenderPass(RenderPass renderPass)
        {
            renderPass.RenderPath = this;
            RenderPasses.Add(renderPass);
        }

        public void Draw(RenderView view)
        {
            var graphics = Get<Graphics>();

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
