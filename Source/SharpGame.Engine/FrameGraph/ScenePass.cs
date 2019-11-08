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
        public BlendFlags BlendFlags { get; set; } = BlendFlags.All;

        public ResourceSet[] Set0 { get; set; }
        public ResourceSet[] Set1 { get; set; }
        public ResourceSet[] Set2 { get; set; }

        public static bool MultiThreaded = true;

        FastList<Task> renderTasks = new FastList<Task>();

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

        public void DrawScene(RenderView view)
        {
            BeginRenderPass(view);

            var set0 = Set0?[Graphics.WorkContext] ?? view.Set0;
            var set1 = Set1?[Graphics.WorkContext] ?? view.Set1;
            var set2 = Set2?[Graphics.WorkContext];

            if((BlendFlags & BlendFlags.Solid) != 0)
            {
                if (MultiThreaded)
                {
                    DrawBatchesMT(view, view.opaqueBatches, set0, set1, set2);
                }
                else
                {
                    DrawBatches(view, view.opaqueBatches, CmdBuffer, set0, set1, set2);
                }
            }
        

            if ((BlendFlags & BlendFlags.AlphaTest) != 0 && view.alphaTestBatches.Count > 0)
            {
                DrawBatches(view, view.alphaTestBatches, CmdBuffer, set0, set1, set2);
            }

            if((BlendFlags & BlendFlags.AlphaBlend) != 0 && view.translucentBatches.Count > 0)
            {
                DrawBatches(view, view.translucentBatches, CmdBuffer, set0, set1, set2);
            }

            EndRenderPass(view);

        }


        public void DrawBatches(RenderView view, FastList<SourceBatch> batches, CommandBuffer cb, ResourceSet set0, ResourceSet set1 = null, ResourceSet set2 = null)
        {
            var cmd = cb;

            if (cmd == null)
            {
                cmd = GetCmdBuffer();
                cmd.SetViewport(view.Viewport);
                cmd.SetScissor(view.ViewRect);
            }

            foreach (var batch in batches)
            {
                DrawBatch(passID, cmd, batch, default, set0, set1, set2);
            }

            cmd.End();
        }

        public void DrawBatchesMT(RenderView view, FastList<SourceBatch> batches, ResourceSet set0, ResourceSet set1 = null, ResourceSet set2 = null)
        {
            renderTasks.Clear();

            int dpPerBatch = (int)Math.Ceiling(view.opaqueBatches.Count / (float)workCount);
            if (dpPerBatch < 200)
            {
                dpPerBatch = 200;
            }

            int idx = 0;
            for (int i = 0; i < batches.Count; i += dpPerBatch)
            {
                int from = i;
                int to = Math.Min(i + dpPerBatch, batches.Count);
                int cmdIndex = idx;
                var t = Task.Run(() =>
                {
                    var cb = GetCmdBuffer(cmdIndex);
                    cb.SetViewport(view.Viewport);
                    cb.SetScissor(view.ViewRect);
                    Draw(view, batches.AsSpan(from, to - from), cb, set0, set1, set2);
                    cb.End();
                });
                renderTasks.Add(t);
                idx++;
            }

            Task.WaitAll(renderTasks.ToArray());
        }

        protected void Draw(RenderView view, Span<SourceBatch> sourceBatches, CommandBuffer commandBuffer, ResourceSet set0, ResourceSet set1, ResourceSet set2)
        {
            foreach (var batch in sourceBatches)
            {
                DrawBatch(passID, commandBuffer, batch, default, set0, set1, set2);
            }
        }

    }
}
