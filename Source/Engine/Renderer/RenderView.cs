using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{

    public class RenderView : Object
    {
        public Scene Scene { get; set; }
        public Camera Camera { get; set; }
        public RenderPath RenderPath { get; set; }
        public VulkanCore.Viewport Viewport { get; set; }
        public uint ViewMask { get; set; }

        public Graphics Graphics => Get<Graphics>();
        public Renderer Renderer => Get<Renderer>();

        internal FastList<Drawable> drawables_ = new FastList<Drawable>();
        internal FastList<Light> lights_ = new FastList<Light>();

        private FrameInfo frame_;

        public RenderView()
        {
        }

        public void Update(ref FrameInfo frameInfo)
        {
            frame_ = frameInfo;
            frame_.camera_ = Camera;
            frame_.viewSize_ = new Int2(Graphics.Width, Graphics.Height);

            SendGlobalEvent(new BeginView { view = this });

            if (RenderPath == null)
            {
                RenderPath = new RenderPath();
                RenderPath.AddRenderPass(new ScenePass());
            }

            CommandBuffer cmdBuffer = Graphics.WorkCmdBuffer;

            UpdateDrawables();

            RenderPath.Draw(this);

            SendGlobalEvent(new EndView { view = this });
        }

        private void UpdateDrawables()
        {
            if (Scene == null || Camera == null)
            {
                return;
            }

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
           

        }

        public void Render(int imageIndex)
        {
            RenderPath?.Summit(imageIndex);
        }
    }
}
