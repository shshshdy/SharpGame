using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class FGScenePass : FGDrawPass
    {
        private ResourceLayout perFrameResLayout;
        private ResourceSet perFrameSet;

        private ResourceLayout perObjectResLayout;
        private ResourceSet perObjectSet;

        public FGScenePass(string name = "main")
        {
            Name = name;

            perFrameResLayout = new ResourceLayout
            {
                new ResourceLayoutBinding(0, DescriptorType.UniformBuffer, ShaderStage.Vertex, 1),
            };

            perObjectResLayout = new ResourceLayout
            {
                new ResourceLayoutBinding(1, DescriptorType.UniformBuffer, ShaderStage.Vertex, 1),
            };

            ActionBegin = OnBegin;
            ActionDraw = OnDraw;
        }

        protected void OnBegin(RenderView view)
        {
            if (perFrameSet == null)
            {
                perFrameSet = new ResourceSet(perFrameResLayout, view.ubCameraVS);
            }

            cmdBuffer.SetViewport(ref view.Viewport);
            cmdBuffer.SetScissor(new Rect2D(0, 0, (int)view.Viewport.width, (int)view.Viewport.height));
        }

        private void OnDraw(RenderView view)
        {
            foreach (var batch in view.batches)
            {
                DrawBatch(batch, perFrameSet);
            }
        }
    }
}
