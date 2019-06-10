using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ScenePass : GraphicsPass
    {
        private ResourceLayout perObjectResLayout;
        private ResourceSet perObjectSet;

        public ScenePass(string name = "main")
        {
            Name = name;

            perObjectResLayout = new ResourceLayout
            {
                new ResourceLayoutBinding(1, DescriptorType.UniformBuffer, ShaderStage.Vertex, 1),
            };

            ActionDraw = (view)=>
            {
                foreach (var batch in view.batches)
                {
                    DrawBatch(batch, view.perFrameSet);
                }
            };
        }

    }
}
