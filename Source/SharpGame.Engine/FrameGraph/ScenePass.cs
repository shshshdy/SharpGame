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
            var batches = view.batches.Items;
            
            var cmd = GetCmdBuffer();

            //cmd.SetViewport(ref view.Viewport);
            //cmd.SetScissor(new Rect2D(0, 0, (int)view.Viewport.width, (int)view.Viewport.height));
            /*
            renderTasks.Clear();

            for (int i = 0; i < view.batches.Count; i += 400)
            {
                int from = i;
                int to = Math.Min(i + 400, view.batches.Count);                
                var t = Task.Run(
                    () => {
                        var cb = GetCmdBuffer(view);
                        Draw(view, batches, cb, from, to);
                        cb.End();
                    });
                renderTasks.Add(t);
            }

            Task.WaitAll(renderTasks.Items);
            */
            
            foreach (var batch in view.batches)
            {
                DrawBatch(cmd, batch, view.perFrameSet);
            }
            cmd.End();
            

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
