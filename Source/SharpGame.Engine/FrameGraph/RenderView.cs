using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{

    public class RenderView : Object
    {
        public Scene Scene { get; set; }
        public Camera Camera { get; set; }
        public FrameGraph RenderPath { get; set; }

        private Viewport viewport;
        public ref Viewport Viewport => ref viewport;

        public uint ViewMask { get; set; }

        public PassHandler OverlayPass { get; set; }

        internal FastList<Drawable> drawables = new FastList<Drawable>();
        internal FastList<Light> lights = new FastList<Light>();

        private FrameInfo frameInfo;

        public RenderView(Camera camera = null, Scene scene = null, FrameGraph renderPath = null)
        {
            Attach(camera, scene, renderPath);
        }

        public void Attach(Camera camera, Scene scene, FrameGraph renderPath = null)
        {
            Scene = scene;
            Camera = camera;
            RenderPath = renderPath;

            if (RenderPath == null)
            {
                RenderPath = new FrameGraph();
                RenderPath.AddRenderPass(new ScenePassHandler());
            }
        }

        public void Update(ref FrameInfo frameInfo)
        {
            var graphics = Graphics.Instance;

            this.frameInfo = frameInfo;
            this.frameInfo.camera = Camera;
            this.frameInfo.viewSize = new Int2(graphics.Width, graphics.Height);

            Viewport.Define(0, 0, graphics.Width, graphics.Height);

            this.SendGlobalEvent(new BeginView { view = this });

            UpdateDrawables();

            RenderPath.Draw(this);

            OverlayPass?.Draw(this);

            this.SendGlobalEvent(new EndView { view = this });
        }

        private void UpdateDrawables()
        {
            drawables.Clear();

            if (Scene == null || Camera == null)
            {
                return;
            }
        
            FrustumOctreeQuery frustumOctreeQuery = new FrustumOctreeQuery
            {
                view = this,
                camera = Camera
            };

            Scene.GetDrawables(frustumOctreeQuery, drawables);

            //todo:multi thread
            foreach(var drawable in drawables)
            {
                drawable.UpdateGeometry(ref frameInfo);
            }

            foreach (var drawable in drawables)
            {
                drawable.UpdateBatches(ref frameInfo);
            }         

        }

        public void Render(int imageIndex)
        {
            RenderPath?.Summit(imageIndex);
            OverlayPass?.Summit(imageIndex);
        }
    }
}
