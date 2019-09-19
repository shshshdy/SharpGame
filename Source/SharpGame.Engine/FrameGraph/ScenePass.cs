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
        public const int WORK_COUNT = 16;
        private CommandBufferPool[][] cmdBufferPools = new CommandBufferPool[WORK_COUNT][];

        FastList<Task> renderTasks = new FastList<Task>();

        public static bool[] multiThreaded = { true, true, true };

        public static bool MultiThreaded = false;

        public ScenePass(string name = "main")
        {
            Name = name;

            for (int i = 0; i < WORK_COUNT; i++)
            {
                cmdBufferPools[i] = new CommandBufferPool[3];
                for (int j = 0; j < 3; j++)
                {
                    cmdBufferPools[i][j] = new CommandBufferPool(Graphics.Instance.Swapchain.QueueNodeIndex, CommandPoolCreateFlags.ResetCommandBuffer);
                    cmdBufferPools[i][j].Allocate(CommandBufferLevel.Secondary, 1);
                    cmdBufferPools[i][j].Name = $"ScenePass_{i}_{j}";
                }
            }
        }

        protected CommandBuffer GetCmdBufferAt(int index)
        {
            int workContext = Graphics.Instance.nextImage;
            var cb = cmdBufferPools[index][workContext].Get();
            cb.renderPass = CurrentRenderPass.RenderPass;

            CurrentRenderPass.AddCommandBuffer(cb);
            if (!cb.IsOpen)
            {
                CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
                {
                    framebuffer = CurrentRenderPass.Framebuffer,
                    renderPass = CurrentRenderPass.RenderPass
                };

                cb.Begin(CommandBufferUsageFlags.OneTimeSubmit | CommandBufferUsageFlags.RenderPassContinue
                    | CommandBufferUsageFlags.SimultaneousUse, ref inherit);
            }

            return cb;
        }

        protected override void DrawImpl(RenderView view)
        {
            BeginRenderPass(view);

            DrawScene(view);

            EndRenderPass(view);
        }

        protected void DrawScene(RenderView view)
        {
            int workContext = Graphics.nextImage;
            var batches = view.batches.Items;
            multiThreaded[workContext] = MultiThreaded;

            if (MultiThreaded)
            {
                for (int i = 0; i < cmdBufferPools.Length; i++)
                {
                    var cmd = cmdBufferPools[i][workContext];
                    cmd.currentIndex = 0;
                }
                renderTasks.Clear();

                int dpPerBatch = (int)Math.Ceiling(view.batches.Count / (float)WORK_COUNT);
                if (dpPerBatch < 200)
                {
                    dpPerBatch = 200;
                }

                int idx = 0;
                for (int i = 0; i < view.batches.Count; i += dpPerBatch)
                {
                    int from = i;
                    int to = Math.Min(i + dpPerBatch, view.batches.Count);
                    int cmdIndex = idx;
                    var t = Task.Run(() =>
                    {
                        var cb = GetCmdBufferAt(cmdIndex);
                        cb.SetViewport(ref view.Viewport);
                        cb.SetScissor(view.ViewRect);
                        Draw(view, batches, cb, from, to);
                        cb.End();
                    });
                    renderTasks.Add(t);
                    idx++;
                }

                Task.WaitAll(renderTasks.ToArray());
            }
            else
            {
                var cmd = GetCmdBuffer();

                cmd.SetViewport(ref view.Viewport);
                cmd.SetScissor(view.ViewRect);

                foreach (var batch in view.batches)
                {
                    DrawBatch(cmd, batch, view.VSSet, view.PSSet, batch.offset);
                }

                cmd.End();
            }


        }

        protected void Draw(RenderView view, SourceBatch[] sourceBatches, CommandBuffer commandBuffer, int from, int to)
        {
            for(int i = from; i < to; i++)
            {
                var batch = sourceBatches[i];
                DrawBatch(commandBuffer, batch, view.VSSet, view.PSSet, batch.offset);
            }

        }

    }
}
