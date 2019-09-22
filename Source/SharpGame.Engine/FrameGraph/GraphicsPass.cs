#define NEW_RENDERPASS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Vulkan;


namespace SharpGame
{
    public class GraphicsPass : FrameGraphPass, IEnumerable<Action<GraphicsPass, RenderView>>
    {
        [IgnoreDataMember]
        public Framebuffer[] framebuffers;

        public ClearColorValue ClearColorValue { get; set; } = new ClearColorValue(0.25f, 0.25f, 0.25f, 1);
        public ClearDepthStencilValue ClearDepthStencilValue { get; set; } = new ClearDepthStencilValue(1.0f, 0);
        protected List<Action<GraphicsPass, RenderView>> Subpasses { get; } = new List<Action<GraphicsPass, RenderView>>();

        protected int workCount = 16;
        protected FastList<CommandBufferPool[]> cmdBufferPools = new FastList<CommandBufferPool[]>();

        protected FastList<RenderPassInfo>[] renderPassInfo = new []
        {
            new FastList<RenderPassInfo>(),
            new FastList<RenderPassInfo>(),
            new FastList<RenderPassInfo>()
        };

        FastListPool<CommandBuffer>[] commdListPool = new []
        {
            new FastListPool<CommandBuffer>(),
            new FastListPool<CommandBuffer>(),
            new FastListPool<CommandBuffer>()
        };


        FastList<Task> renderTasks = new FastList<Task>();


        public static bool MultiThreaded = false;

        public GraphicsPass(string name = "", int workCount = 0)
        {
            Name = name;

            CreateCommandPool(4);

            for (int i = 0; i < workCount; i++)
            {
                CreateCommandPool(1);
            }
        }

        protected void CreateCommandPool(uint numCmd = 1)
        {
            var cmdBufferPool = new CommandBufferPool[3];
            for (int i= 0; i < 3; i++)
            {
                cmdBufferPool[i] = new CommandBufferPool(Graphics.Swapchain.QueueNodeIndex, CommandPoolCreateFlags.ResetCommandBuffer);
                cmdBufferPool[i].Allocate(CommandBufferLevel.Secondary, numCmd);
            }

            cmdBufferPools.Add(cmdBufferPool);
        }

        public void Add(Action<GraphicsPass, RenderView> subpass)
        {
            Subpasses.Add(subpass);
        }

        protected CommandBuffer GetCmdBuffer(int index = -1)
        {
            uint workContext = Graphics.WorkImage;
            var cb = cmdBufferPools[index + 1][workContext].Get();
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

        protected void Begin(RenderView view)
        {
            if (renderPass == null)
            {
                renderPass = Graphics.RenderPass;
            }

            if (framebuffers.IsNullOrEmpty())
            {
                framebuffers = Graphics.Framebuffers;
            }

            uint workContext = Graphics.WorkImage;

            for (int i = 0; i < cmdBufferPools.Count; i++)
            {
                var cmd = cmdBufferPools[i][workContext];
                cmd.currentIndex = 0;
            }

            renderPassInfo[workContext].Clear();
        }


        bool inRenderPass = false;
        protected void BeginRenderPass(RenderView view)
        {
            BeginRenderPass(framebuffers[Graphics.WorkImage], view.ViewRect, ClearColorValue, ClearDepthStencilValue);
        }

        protected void BeginRenderPass(Framebuffer framebuffer, Rect2D renderArea, params ClearValue[] clearValues)
        {
            if(inRenderPass)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            inRenderPass = true;
            var rpInfo = new RenderPassInfo
            {
                workImage = Graphics.WorkImage,
                rpBeginInfo = new RenderPassBeginInfo(framebuffer.renderPass, framebuffer, renderArea, clearValues),
                commandList = commdListPool[Graphics.WorkImage].Request()
            };

            renderPassInfo[Graphics.WorkImage].Add(rpInfo);
        }

        protected ref RenderPassInfo CurrentRenderPass => ref renderPassInfo[Graphics.WorkImage].Back();

        protected void EndRenderPass(RenderView view)
        {
            if (!inRenderPass)
            {
                System.Diagnostics.Debug.Assert(false);
                return;
            }

            inRenderPass = false;
        }

        public override void Draw(RenderView view)
        {
            Begin(view);

            DrawImpl(view);

            End(view);
        }

        protected void End(RenderView view)
        {
        }

        protected virtual void DrawImpl(RenderView view)
        {
            BeginRenderPass(view);

            cmdBuffer = GetCmdBuffer();

            cmdBuffer.SetViewport(ref view.Viewport);
            cmdBuffer.SetScissor(view.ViewRect);

            for (int i = 0; i < Subpasses.Count; i++)
            {
                var action = Subpasses[i];
                action.Invoke(this, view);
                
                if(i != Subpasses.Count - 1)
                {
                    cmdBuffer.NextSubpass(SubpassContents.Inline);
                }
            }

            cmdBuffer?.End();
            cmdBuffer = null;

            EndRenderPass(view);
        }

        public void DrawBatches(RenderView view, FastList<SourceBatch> batches, CommandBuffer cb)
        {
            var cmd = cb;

            if (cmd == null)
            {
                cmd = GetCmdBuffer();
                cmd.SetViewport(ref view.Viewport);
                cmd.SetScissor(view.ViewRect);
            }

            foreach (var batch in batches)
            {
                DrawBatch(cmd, batch, default, view.VSSet, view.PSSet, batch.offset);
            }

            cmd.End();
        }

        public void DrawBatchesMT(RenderView view, FastList<SourceBatch> batches)
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

        protected void Draw(RenderView view, FastList<SourceBatch> sourceBatches, CommandBuffer commandBuffer, int from, int to)
        {
            for (int i = from; i < to; i++)
            {
                var batch = sourceBatches[i];
                DrawBatch(commandBuffer, batch, default, view.VSSet, view.PSSet, batch.offset);
            }

        }

        public void DrawFullScreenQuad(CommandBuffer cb, Material material)
        {
            var shader = material.Shader;
            var pass = shader.GetPass(passID);
            var pipe = pass.GetGraphicsPipeline(renderPass, null);

            cb.BindPipeline(PipelineBindPoint.Graphics, pipe);

            material.Bind(pass.passIndex, cb);

            cb.Draw(3, 1, 0, 0);
        }

        public void DrawBatch(CommandBuffer cb, SourceBatch batch, Span<ConstBlock> pushConsts, ResourceSet resourceSet, 
            ResourceSet resourceSet1, uint? offset = null, uint? offset1 = null)
        {
            var shader = batch.material.Shader;
            if ((passID & shader.passFlags) == 0)
            {
                return;
            }
            
            var pass = shader.GetPass(passID);
            var pipe = pass.GetGraphicsPipeline(renderPass, batch.geometry);

            cb.BindPipeline(PipelineBindPoint.Graphics, pipe);

            cb.BindGraphicsResourceSet(pass.PipelineLayout, 0, resourceSet, offset);

            if (resourceSet1 != null && (pass.PipelineLayout.DefaultResourcSet & DefaultResourcSet.PS) != 0)
            {
                cb.BindGraphicsResourceSet(pass.PipelineLayout, 1, resourceSet1, offset1);
            }

            foreach(ConstBlock constBlock in pushConsts)
            {
                cb.PushConstants(pass.PipelineLayout, constBlock.range.stageFlags, constBlock.range.offset, constBlock.range.size, constBlock.data);
            }

            batch.material.Bind(pass.passIndex, cb);
            batch.geometry.Draw(cb);
        }

        public override void Submit(int imageIndex)
        {
            var rpInfo = renderPassInfo[imageIndex];
            if(rpInfo.Count > 0)
            {
                foreach (var rp in rpInfo)
                {
                    rp.Submit(imageIndex);

                    commdListPool[imageIndex].Free(rp.commandList);
                }
            }
            else
            {
                // clear pass
                CommandBuffer cb = Graphics.Instance.RenderCmdBuffer;

                var fb = Graphics.Framebuffers[imageIndex];

                RenderPassBeginInfo rpBeginInfo = new RenderPassBeginInfo
                (
                    fb.renderPass, fb,
                    new Rect2D(0, 0, Graphics.Width, Graphics.Height), ClearColorValue, ClearDepthStencilValue
                );
                cb.BeginRenderPass(ref rpBeginInfo, SubpassContents.SecondaryCommandBuffers);
                cb.EndRenderPass();
            }
        
        }

        public IEnumerator<Action<GraphicsPass, RenderView>> GetEnumerator()
        {
            return ((IEnumerable<Action<GraphicsPass, RenderView>>)Subpasses).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Action<GraphicsPass, RenderView>>)Subpasses).GetEnumerator();
        }
    }

    public struct RenderPassInfo
    {
        public uint workImage;
        public RenderPassBeginInfo rpBeginInfo;
        public Framebuffer Framebuffer => rpBeginInfo.framebuffer;
        public RenderPass RenderPass => rpBeginInfo.renderPass;

        public FastList<CommandBuffer> commandList;

        public void AddCommandBuffer(CommandBuffer cb)
        {
            lock (commandList)
            {
                commandList.Add(cb);
            }
        }

        public void Submit(int imageIndex)
        {
            CommandBuffer cb = Graphics.Instance.RenderCmdBuffer;

            System.Diagnostics.Debug.Assert(imageIndex == workImage);

            cb.BeginRenderPass(ref rpBeginInfo, SubpassContents.SecondaryCommandBuffers);

            foreach (var cmd in commandList)
            {
                cb.ExecuteCommand(cmd);
            }

            cb.EndRenderPass();

        }
    }


}
