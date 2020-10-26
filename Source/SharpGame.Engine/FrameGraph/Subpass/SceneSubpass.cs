#define MULTI_THREAD

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace SharpGame
{
    public class SceneSubpass : GraphicsSubpass
    {
        public BlendFlags BlendFlags { get; set; } = BlendFlags.All;

        public ResourceSet[] Set0 { get; set; }
        public ResourceSet[] Set1 { get; set; }
        public ResourceSet[] Set2 { get; set; }

        public static bool MultiThreaded = false;

        FastList<Task> renderTasks = new FastList<Task>();

        protected int workCount = 16;
        protected FastList<CommandBufferPool[]> cmdBufferPools = new FastList<CommandBufferPool[]>();

        List<CommandBuffer> secondCmdBuffers = new List<CommandBuffer>();

        public SceneSubpass(string name = "main") : base(name, 16)
        {
            for (int i = 0; i < workCount; i++)
            {
                CreateCommandPool(1);
            }
        }

        public override void Init()
        {
            FrameGraphPass.UseSecondCmdBuffer = MultiThreaded;
        }

        protected void CreateCommandPool(uint numCmd = 1)
        {
            var cmdBufferPool = new CommandBufferPool[3];
            for (int i = 0; i < 3; i++)
            {
                cmdBufferPool[i] = new CommandBufferPool(Graphics.Swapchain.QueueNodeIndex, CommandPoolCreateFlags.ResetCommandBuffer);
                cmdBufferPool[i].Allocate(CommandBufferLevel.Secondary, numCmd);
            }

            cmdBufferPools.Add(cmdBufferPool);
        }

        public CommandBuffer GetCmdBuffer(int index)
        {
            int workContext = Graphics.WorkImage;
            var cb = cmdBufferPools[index][workContext].Get();

            if (!cb.IsOpen)
            {
                CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
                {
                    framebuffer = FrameGraphPass.Framebuffers[workContext],
                    renderPass = FrameGraphPass.RenderPass
                };

                cb.Begin(CommandBufferUsageFlags.OneTimeSubmit | CommandBufferUsageFlags.RenderPassContinue
                    | CommandBufferUsageFlags.SimultaneousUse, ref inherit);
            }

            return cb;
        }

        protected void Clear()
        {
            int workContext = Graphics.WorkImage;

            for (int i = 0; i < cmdBufferPools.Count; i++)
            {
                var cmd = cmdBufferPools[i][workContext];
                cmd.Clear();
            }

        }

        protected override void DrawImpl(RenderView view)
        {
            Clear();

            if (OnDraw != null)
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
            DrawScene(view, BlendFlags);
        }

        public void DrawScene(RenderView view, BlendFlags blendFlags)
        {
            var set0 = Set0?[Graphics.WorkImage] ?? view.Set0;
            var set1 = Set1?[Graphics.WorkImage] ?? view.Set1;
            var set2 = Set2?[Graphics.WorkImage];

            var cmd = CmdBuffer;
            cmd.SetViewport(view.Viewport);
            cmd.SetScissor(view.ViewRect);

            if ((blendFlags & BlendFlags.Solid) != 0)
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

            if ((blendFlags & BlendFlags.AlphaTest) != 0 && view.alphaTestBatches.Count > 0)
            {
                DrawBatches(view, view.alphaTestBatches, CmdBuffer, set0, set1, set2);
            }

            if ((blendFlags & BlendFlags.AlphaBlend) != 0 && view.translucentBatches.Count > 0)
            {
                DrawBatches(view, view.translucentBatches, CmdBuffer, set0, set1, set2);
            }

        }


        public void DrawBatches(RenderView view, FastList<SourceBatch> batches, CommandBuffer cb, ResourceSet set0, ResourceSet set1 = null, ResourceSet set2 = null)
        {
            var cmd = cb;

            foreach (var batch in batches)
            {
                DrawBatch(passID, cmd, batch, default, set0, set1, set2);
            }

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
                var cb = GetCmdBuffer(idx);
                secondCmdBuffers.Add(cb);
                var t = Task.Run(() =>
                {
                    cb.SetViewport(view.Viewport);
                    cb.SetScissor(view.ViewRect);
                    Draw(view, batches.AsSpan(from, to - from), cb, set0, set1, set2);
                    cb.End();
                });
                renderTasks.Add(t);
                idx++;
            }

            Task.WaitAll(renderTasks.ToArray());

            int workContext = Graphics.WorkImage;
            var cmd = CmdBuffer;
            foreach(var c in secondCmdBuffers)
            {
                cmd.ExecuteCommand(c);
            }
            secondCmdBuffers.Clear();

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
