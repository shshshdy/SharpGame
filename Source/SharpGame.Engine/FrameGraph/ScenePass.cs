using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class ScenePass : GraphicsPass
    {
        private ResourceLayout perObjectLayout;
        private ResourceSet perObjectSet;

        public ScenePass(string name = "main")
        {
            Name = name;

            perObjectLayout = new ResourceLayout
            {
                new ResourceLayoutBinding(1, DescriptorType.UniformBuffer, ShaderStage.Vertex),
            };

            OnBegin = (view) =>
            {

            };

            OnDraw = (view)=>
            {
                foreach (var batch in view.batches)
                {
                    DrawBatch(batch, view.perFrameSet);
                }
            };
        }

    }
}
