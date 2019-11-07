#define MULTI_THREAD

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace SharpGame
{
    public class ScenePass : GraphicsPass
    {
        public static bool MultiThreaded = true;

        public ScenePass() : this("main")
        {
        }

        public ScenePass(string name = "main") : base(name, 16)
        {
        }

        protected override void DrawImpl(RenderView view)
        {
            if(OnDraw != null)
            {
                OnDraw.Invoke(this, view);
            }
            else
            {
                DrawScene(view);
            }

        }

        protected void DrawScene(RenderView view)
        {
            BeginRenderPass(view);

            var batches = view.opaqueBatches;

            if (MultiThreaded)
            {
                DrawBatchesMT(view, batches, view.Set0, view.Set1);
            }
            else
            {
                DrawBatches(view, batches, CmdBuffer, view.Set0, view.Set1);
            }

            if (view.alphaTestBatches.Count > 0)
            {
                DrawBatches(view, view.alphaTestBatches, CmdBuffer, view.Set0, view.Set1);
            }

            if(view.translucentBatches.Count > 0)
            {
                DrawBatches(view, view.translucentBatches, CmdBuffer, view.Set0, view.Set1);
            }

            EndRenderPass(view);

        }


    }
}
