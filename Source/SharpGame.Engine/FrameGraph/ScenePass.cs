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
        private ResourceLayout perObjectLayout;
        private ResourceSet perObjectSet;

        private CommandBufferPool[][] cmdBufferPools = new CommandBufferPool[2][];
        FastList<Task> renderTasks = new FastList<Task>();

        const int WORK_COUNT = 16;
        public ScenePass(string name = "main")
        {
            Name = name;

            perObjectLayout = new ResourceLayout
            {
                new ResourceLayoutBinding(1, DescriptorType.UniformBuffer, ShaderStage.Vertex),
            };

            for(int i = 0; i < 2; i++)
            {
                cmdBufferPools[i] = new CommandBufferPool[WORK_COUNT];
                for(int j = 0; j < WORK_COUNT; j++)
                {
                    cmdBufferPools[i][j] = new CommandBufferPool(Graphics.Instance.Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer);
                    cmdBufferPools[i][j].Allocate(CommandBufferLevel.Secondary, 1);
                }

            }
        }

        protected CommandBuffer GetCmdBufferAt(int index)
        {
            var g = Graphics.Instance;
            int workContext = g.WorkContext;
            var cb = cmdBufferPools[workContext][index][0];
            cb.renderPass = renderPass;
            cmdBufferPools[workContext][index].currentIndex = 1;
            if (!cb.IsOpen)
            {
                CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
                {
                    framebuffer = framebuffers[g.nextImage],
                    renderPass = renderPass
                };

                cb.Begin(CommandBufferUsageFlags.OneTimeSubmit | CommandBufferUsageFlags.RenderPassContinue
                    | CommandBufferUsageFlags.SimultaneousUse, ref inherit);
            }

            return cb;
        }

        protected override void DrawImpl(RenderView view)
        {
            var g = Graphics.Instance;
            var batches = view.batches.Items;
#if MULTI_THREAD

            int workContext = g.WorkContext;
            for (int i = 0; i < cmdBufferPools[workContext].Length; i++)
            {
                var cmd = cmdBufferPools[workContext][i];
                cmd.currentIndex = 0;
            }

            renderTasks.Clear();

            int dpPerBatch = (int)Math.Ceiling(view.batches.Count / (float)WORK_COUNT);
            if(dpPerBatch < 200)
            {
                dpPerBatch = 200;
            }

            int idx = 0;
            for (int i = 0; i < view.batches.Count; i += dpPerBatch)
            {
                int from = i;
                int to = Math.Min(i + dpPerBatch, view.batches.Count);
                int cmdIndex = idx;
                var t = Task.Run(
                    () => {
                        var cb = GetCmdBufferAt(cmdIndex);
                        cb.SetViewport(ref view.Viewport);
                        cb.SetScissor(new Rect2D(0, 0, (int)view.Viewport.width, (int)view.Viewport.height));
                        Draw(view, batches, cb, from, to);
                        cb.End();
                    });
                renderTasks.Add(t);
                idx++;
            }

            Task.WaitAll(renderTasks.ToArray());
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

        public override void Summit(int imageIndex)
        {
#if MULTI_THREAD
            var g = Graphics.Instance;
            CommandBuffer cb = g.RenderCmdBuffer;
            var fbs = framebuffers ?? g.Framebuffers;
            int renderContext = g.RenderContext;
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
                if(cmd.currentIndex > 0)
                cb.ExecuteCommand(cmd.CommandBuffers[0]);
            }
            cb.EndRenderPass();
#else
            base.Summit(imageIndex);
#endif
        }
    }
}
