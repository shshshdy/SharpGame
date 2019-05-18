using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{

    public class RenderView : Object
    {
        public Scene Scene { get; set; }
        public Camera Camera { get; set; }
        public RenderPath RenderPath { get; set; }
        public Viewport Viewport { get; set; }
        public uint ViewMask { get; set; }
        public RenderPass OverlayPass { get; set; }

        internal FastList<Drawable> drawables_ = new FastList<Drawable>();
        internal FastList<Light> lights_ = new FastList<Light>();

        private FrameInfo frame_;

        public RenderView(Camera camera = null, Scene scene = null, RenderPath renderPath = null)
        {
            Scene = scene;            
            Camera = camera;
            RenderPath = renderPath;

            if (RenderPath == null)
            {
                RenderPath = new RenderPath();
                RenderPath.AddRenderPass(new ScenePass());
            }

        }

        public void Update(ref FrameInfo frameInfo)
        {
            frame_ = frameInfo;
            frame_.camera_ = Camera;
            frame_.viewSize_ = new Int2((int)Graphics.Width, (int)Graphics.Height);

            this.SendGlobalEvent(new BeginView { view = this });

            UpdateDrawables();

            RenderPath.Draw(this);

            OverlayPass?.Draw(this);

            this.SendGlobalEvent(new EndView { view = this });
        }

        private void UpdateDrawables()
        {
            drawables_.Clear();

            if (Scene == null || Camera == null)
            {
                return;
            }
            /*
            FrustumOctreeQuery frustumOctreeQuery = new FrustumOctreeQuery
            {
                view = this,
                camera = Camera
            };

            Scene.GetDrawables(frustumOctreeQuery, drawables_);

            //todo:multi thread
            foreach(var drawable in drawables_)
            {
                drawable.UpdateGeometry(ref frame_);
            }

            foreach (var drawable in drawables_)
            {
                drawable.UpdateBatches(ref frame_);
            }
           */

        }

        public void Render(int imageIndex)
        {
            RenderPath?.Summit(imageIndex);
            OverlayPass?.Summit(imageIndex);
        }
    }
}
