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
        public ScenePass(string name = "main") : base(name)
        {
        }

        protected override void DrawImpl(RenderView view)
        {
            BeginRenderPass(view);

            DrawScene(view);

            EndRenderPass(view);
        }

        protected void DrawScene(RenderView view)
        {
            var batches = view.batches;

            if (MultiThreaded)
            {
                DrawBatchesMT(view, batches);
            }
            else
            {
                DrawBatches(view, batches, CmdBuffer);
            }

        }


    }
}
