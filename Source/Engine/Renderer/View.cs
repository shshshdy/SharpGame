﻿using System;
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

        FastList<Drawable> drawables_ = new FastList<Drawable>();
        FastList<Light> lights_ = new FastList<Light>();

        public View(RenderPath renderPath = null)
        {
            RenderPath = renderPath;

            if(RenderPath == null)
            {
                RenderPath = new RenderPath();
                RenderPath.AddRenderPass(new ScenePass());
            }
        }

        public void Update()
        {
            GetDrawables();

            RenderPath.Draw(this);
        }

        private void GetDrawables()
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

        }

        public void Render(int imageIndex)
        {
            RenderPath?.Summit(imageIndex);
        }
    }
}
