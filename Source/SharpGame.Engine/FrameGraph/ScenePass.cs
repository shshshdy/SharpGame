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
        public static bool MultiThreaded = false;

        public ScenePass(string name = "main") : base(name, 16)
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
            var batches = view.opaqueBatches;

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
