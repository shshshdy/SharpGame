#define MULTI_THREAD
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    public class ScenePass : GraphicsPass
    {
        private ResourceLayout perObjectLayout;
        private ResourceSet perObjectSet;

        FastList<Task> renderTasks = new FastList<Task>();
        public ScenePass(string name = "main")
        {
            Name = name;

            perObjectLayout = new ResourceLayout
            {
                new ResourceLayoutBinding(1, DescriptorType.UniformBuffer, ShaderStage.Vertex),
            };

        }

        protected override void DrawImpl(RenderView view)
        {
            var g = Graphics.Instance;
            var batches = view.batches.Items;

#if MULTI_THREAD
            renderTasks.Clear();

            int idx = 0;
            for (int i = 0; i < view.batches.Count; i += 400)
            {
                int from = i;
                int to = Math.Min(i + 400, view.batches.Count);                
                var t = Task.Run(
                    () => {
                        var cb = GetCmdBufferAt(idx);
                        cb.SetViewport(ref view.Viewport);
                        cb.SetScissor(new Rect2D(0, 0, (int)view.Viewport.width, (int)view.Viewport.height));
                        Draw(view, batches, cb, from, to);
                        cb.End();
                    });
                renderTasks.Add(t);
                idx++;
            }

            WorkCount = renderTasks.Count;
            int workContext = g.WorkContext;
            //cmdBufferPool[workContext].currentIndex = renderTasks.Count;
            Task.WaitAll(renderTasks.Items);
#else

            var cmd = GetCmdBuffer();

            cmd.SetViewport(ref view.Viewport);
            cmd.SetScissor(new Rect2D(0, 0, (int)view.Viewport.width, (int)view.Viewport.height));

            foreach (var batch in view.batches)
            {
                DrawBatch(cmd, batch, view.perFrameSet);
            }

            cmd.End();
            
#endif
        }

        void Draw(RenderView view, SourceBatch[] sourceBatches, CommandBuffer commandBuffer, int from, int to)
        {
            for(int i = from; i < to; i++)
            {
                DrawBatch(commandBuffer, sourceBatches[i], view.perFrameSet);
            }

        }

    }
}
