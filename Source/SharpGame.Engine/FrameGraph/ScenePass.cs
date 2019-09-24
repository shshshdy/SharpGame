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
        RenderTarget colorRT;
        public ScenePass(string name = "main") : base(name, 16)
        {
            /*
            RenderPass = Graphics.CreateRenderPass(true, true);

            colorRT = new RenderTarget(Graphics.Width, Graphics.Height, 1, Format.R8g8b8a8Unorm,
                        ImageUsageFlags.DepthStencilAttachment | ImageUsageFlags.Sampled, ImageAspectFlags.Depth,
                        SampleCountFlags.Count1, ImageLayout.DepthStencilReadOnlyOptimal);

            Framebuffer = Framebuffer.Create(RenderPass,
                Graphics.Width, Graphics.Height, 1, new[] { colorRT.view, Graphics.RTDepth.view });
                */
            //Renderer.AddDebugImage(colorRT.view);
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
