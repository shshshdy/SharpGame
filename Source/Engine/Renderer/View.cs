using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{

    public class View : Object
    {
        public uint ViewMask { get; set; }

        public Scene scene;
        public Camera camera;
        public RenderPath RenderPath { get; set; }

        public Graphics Graphics => Get<Graphics>();
        public Renderer Renderer => Get<Renderer>();

        internal FastList<Drawable> drawables_ = new FastList<Drawable>();
        internal FastList<Light> lights_ = new FastList<Light>();

        FrameInfo frame_;

        public View(RenderPath renderPath = null)
        {
            RenderPath = renderPath;

            if(RenderPath == null)
            {
                RenderPath = new RenderPath();
                RenderPath.AddRenderPass(new ScenePass());
            }
        }

        public void Update(ref FrameInfo frameInfo)
        {
            frame_ = frameInfo;
            frame_.camera_ = camera;
            //frame_.viewSize_ = View

            UpdateDrawables();

            RenderPath.Draw(this);
        }

        private void UpdateDrawables()
        {
            if (scene && camera)
            {
                FrustumOctreeQuery frustumOctreeQuery = new FrustumOctreeQuery
                {
                    view = this,
                    camera = camera
                };

                scene.GetDrawables(frustumOctreeQuery, drawables_);
            }

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
