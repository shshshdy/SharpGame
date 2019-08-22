using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGame
{
    public class Renderer : System<Renderer>
    {
        public RenderView MainView { get; private set; }

        private List<RenderView> views = new List<RenderView>();

        public Graphics Graphics => Graphics.Instance;

        public Renderer()
        {
            MainView = CreateRenderView();
        }

        public RenderView CreateRenderView(Camera camera = null, Scene scene = null, FrameGraph renderPath = null)
        {
            var view = new RenderView(camera, scene, renderPath);           
            views.Add(view);
            return view;
        }

        public void RenderUpdate()
        {
            var frameInfo = new FrameInfo
            {
                timeStep = Time.Delta,
                frameNumber = Time.FrameNum
            };

            foreach (var viewport in views)
            {
                viewport.Update(ref frameInfo);
            }

        }

        public void Render()
        {
            Profiler.BeginSample("Render");

            Graphics.BeginRender();

            CommandBuffer cmdBuffer = Graphics.RenderCmdBuffer;

            cmdBuffer.Begin();

            this.SendGlobalEvent(new BeginRender());

            int imageIndex = (int)Graphics.currentImage;

            foreach (var viewport in views)
            {
                viewport.Render(imageIndex);
            }
          
            this.SendGlobalEvent(new EndRender());

            cmdBuffer.End();

            Graphics.EndRender();

            Profiler.EndSample();

        }

    }
}
