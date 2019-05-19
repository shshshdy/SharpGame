using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;


namespace SharpGame
{
    public class FrameGraph : Resource//, IEnum
    {
        public RenderTarget[] RenderTargets { get; set; }


        public List<RenderPass> RenderPassList { get; set; } = new List<RenderPass>();

        public RenderPass[] RenderPasses
        {
            set
            {
                foreach(var rp in value)
                {
                    AddRenderPass(rp);
                }
            }
        }

        public FrameGraph()
        {
        }

        public void AddRenderPass(RenderPass renderPass)
        {
            renderPass.RenderPath = this;
            RenderPassList.Add(renderPass);
        }

        public void Draw(RenderView view)
        {
            foreach (var renderPass in RenderPassList)
            {
                renderPass.Draw(view);
            }
            
        }

        public void Summit(int imageIndex)
        {
            foreach (var renderPass in RenderPassList)
            {
                renderPass.Summit(imageIndex);
            }

        }

    }

}
