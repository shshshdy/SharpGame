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

            OnDraw = DrawBatches;
        }

        void DrawBatches(RenderView view)
        {
            var batches = view.batches.Items;
            /*
            renderTasks.Clear();

            for (int i = 0; i < view.batches.Count; i += 400)
            {
                var cmd = this.GetCmdBuffer(view);
                int to = Math.Min(i + 400, view.batches.Count);                
                var t = Task.Run(() => Draw(view, batches, cmd, i, to));
                renderTasks.Add(t);
            }

            Task.WaitAll(renderTasks.Items);
            */
            /*
            foreach (var batch in view.batches)
            {
                DrawBatch(cmdBuffer, batch, view.perFrameSet);
            }*/

            Draw(view, batches, cmdBuffer, 0, 400);

            //var cmd = this.GetCmdBuffer(view);
            //Draw(view, batches, cmd, 400, 800);

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
