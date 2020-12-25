#define MULTI_THREAD

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace SharpGame
{
    public class SceneSubpass : GraphicsSubpass
    {
        public BlendFlags BlendFlags { get; set; } = BlendFlags.All;

        public DescriptorSet Set0 { get; set; }
        public DescriptorSet Set1 { get; set; }
        public DescriptorSet Set2 { get; set; }

        public static bool MultiThreaded = false;

        FastList<Task> renderTasks = new FastList<Task>();

        protected int workCount = 16;
        protected FastList<CommandBufferPool[]> cmdBufferPools = new FastList<CommandBufferPool[]>();

        List<CommandBuffer> secondCmdBuffers = new List<CommandBuffer>();

        public SceneSubpass(string name = "main", int workCount = 16) : base(name)
        {
            this.workCount = workCount;

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
                cmdBufferPool[i] = new CommandBufferPool(Graphics.Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer);
                cmdBufferPool[i].Allocate(VkCommandBufferLevel.Secondary, numCmd);
            }

            cmdBufferPools.Add(cmdBufferPool);
        }

        public CommandBuffer GetCmdBuffer(int index)
        {
            int workContext = Graphics.WorkContext;
            var cb = cmdBufferPools[index][workContext].Get();

            if (!cb.IsOpen)
            {
                var inherit = new CommandBufferInheritanceInfo
                (
                    FrameGraphPass.Framebuffers[Graphics.WorkImage],
                    FrameGraphPass.RenderPass, SubpassIndex
                );

                cb.Begin(VkCommandBufferUsageFlags.OneTimeSubmit | VkCommandBufferUsageFlags.RenderPassContinue
                    | VkCommandBufferUsageFlags.SimultaneousUse, ref inherit);
            }

            return cb;
        }

        protected void Clear()
        {
            int workContext = Graphics.WorkContext;

            for (int i = 0; i < cmdBufferPools.Count; i++)
            {
                var cmd = cmdBufferPools[i][workContext];
                cmd.Clear();
            }

        }

        public override void Draw(RenderContext rc, CommandBuffer cb)
        {
            Clear();

            if (OnDraw != null)
            {
                OnDraw.Invoke(this, rc, cb);
            }
            else
            {
                DrawScene(cb);
            }

        }

        public void DrawScene(CommandBuffer cmd)
        {
            DrawScene(cmd, BlendFlags);
        }

        public void DrawScene(CommandBuffer cmd, BlendFlags blendFlags)
        {
            var set0 = Set0 ?? View.Set0;
            Span<DescriptorSet> set1 =  new [] { Set1 ?? View.Set1, Set2 };
             
            cmd.SetViewport(View.Viewport);
            cmd.SetScissor(View.ViewRect);

            if ((blendFlags & BlendFlags.Solid) != 0)
            {
                if (MultiThreaded)
                {
                    DrawBatchesMT(cmd, View.opaqueBatches, set0, set1);
                }
                else
                {
                    DrawBatches(cmd, View.opaqueBatches.AsSpan(), set0, set1);
                }
            }

            if ((blendFlags & BlendFlags.AlphaTest) != 0 && View.alphaTestBatches.Count > 0)
            {
                DrawBatches(cmd, View.alphaTestBatches.AsSpan(), set0, set1);
            }

            if ((blendFlags & BlendFlags.AlphaBlend) != 0 && View.translucentBatches.Count > 0)
            {
                DrawBatches(cmd, View.translucentBatches.AsSpan(), set0, set1);
            }

        }

        DescriptorSet[] tempSets = new DescriptorSet[8];
        public void DrawBatchesMT(CommandBuffer cmd, FastList<SourceBatch> batches, DescriptorSet set0, Span<DescriptorSet> set1)
        {
            renderTasks.Clear();

            int dpPerBatch = (int)Math.Ceiling(View.opaqueBatches.Count / (float)workCount);
            if (dpPerBatch < 200)
            {
                dpPerBatch = 200;
            }
            
            for(int i = 0; i < set1.Length; i++)
            {
                tempSets[i] = set1[i];
            }

            ArraySegment<DescriptorSet> setSegment = new ArraySegment<DescriptorSet>(tempSets, 0, set1.Length);

            int idx = 0;
            for (int i = 0; i < batches.Count; i += dpPerBatch)
            {
                int from = i;
                int to = Math.Min(i + dpPerBatch, batches.Count);
                var cb = GetCmdBuffer(idx);
                secondCmdBuffers.Add(cb);
                var t = Task.Run(() =>
                {
                    cb.SetViewport(View.Viewport);
                    cb.SetScissor(View.ViewRect);
                    DrawBatches(cb, batches.AsSpan(from, to - from), set0, setSegment);
                    cb.End();
                });
                renderTasks.Add(t);
                idx++;
            }

            Task.WaitAll(renderTasks.ToArray());

            int workContext = Graphics.WorkContext;           
            foreach(var c in secondCmdBuffers)
            {
                cmd.ExecuteCommand(c);
            }

            secondCmdBuffers.Clear();
            tempSets.Clear();
        }


    }
}
