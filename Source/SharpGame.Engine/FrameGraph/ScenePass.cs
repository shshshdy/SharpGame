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
        private CommandBufferPool[][] cmdBufferPools = new CommandBufferPool[3][];
        FastList<Task> renderTasks = new FastList<Task>();

        const int WORK_COUNT = 16;
        public static bool[] multiThreaded = { true, true, true };

        public static bool MultiThreaded = true;

        public ScenePass(string name = "main")
        {
            Name = name;

            for (int i = 0; i < 3; i++)
            {
                cmdBufferPools[i] = new CommandBufferPool[WORK_COUNT];
                for (int j = 0; j < WORK_COUNT; j++)
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
            var cb = cmdBufferPools[workContext][index][0];
            cb.renderPass = renderPass;
            cmdBufferPools[workContext][index].currentIndex = 1;

            if (!cb.IsOpen)
            {
                CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
                {
                    framebuffer = framebuffers[Graphics.Instance.nextImage],
                    renderPass = renderPass
                };

                cb.Begin(CommandBufferUsageFlags.OneTimeSubmit | CommandBufferUsageFlags.RenderPassContinue
                    | CommandBufferUsageFlags.SimultaneousUse, ref inherit);
            }

            return cb;
        }

        protected override void DrawImpl(RenderView view)
        {
            DrawScene(view);
        }

        protected void DrawScene(RenderView view)
        {
            int workContext = Graphics.Instance.nextImage;// Graphics.Instance.WorkContext;
            var batches = view.batches.Items;
            multiThreaded[workContext] = MultiThreaded;

            if (MultiThreaded)
            {
                for (int i = 0; i < cmdBufferPools[workContext].Length; i++)
                {
                    var cmd = cmdBufferPools[workContext][i];
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

        public override void Submit(int imageIndex)
        {
            var g = Graphics.Instance;
            int renderContext = imageIndex;
            bool mt = multiThreaded[renderContext];

            if (mt)
            {
                CommandBuffer cb = g.RenderCmdBuffer;
                var fbs = framebuffers ?? g.Framebuffers;
                var fb = fbs[imageIndex];

                var renderPassBeginInfo = new RenderPassBeginInfo
                (
                    fb.renderPass, fb,
                    new Rect2D(0, 0, g.Width, g.Height),
                    ClearColorValue, ClearDepthStencilValue
                );

                cb.BeginRenderPass(ref renderPassBeginInfo, SubpassContents.SecondaryCommandBuffers);

                for (int i = 0; i < cmdBufferPools[renderContext].Length; i++)
                {
                    var cmd = cmdBufferPools[renderContext][i];
                    if (cmd.currentIndex > 0)
                        cb.ExecuteCommand(cmd.CommandBuffers[0]);
                }

                cb.EndRenderPass();

            }
            else
            {
                base.Submit(imageIndex);
            }

        }
    }
}
