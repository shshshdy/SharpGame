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
        public Framebuffer[] Framebuffers { get; set; }
        [IgnoreDataMember]
        public Framebuffer Framebuffer { set => Framebuffers = new[] { value, value, value }; }

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

        protected ResourceSet[] resourceSets = new ResourceSet[2];

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
        
        public void SetGlobalResourceSet(int index, ResourceSet rs)
        {
            resourceSets[index] = rs;
        }

        public CommandBuffer GetCmdBuffer(int index = -1)
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
            if (RenderPass == null)
            {
                RenderPass = Graphics.RenderPass;
            }

            if (Framebuffers == null)
            {
                Framebuffers = Graphics.Framebuffers;
            }

            Reset();
        }

        protected virtual void Reset()
        {
            uint workContext = Graphics.WorkImage;

            for (int i = 0; i < cmdBufferPools.Count; i++)
            {
                var cmd = cmdBufferPools[i][workContext];
                cmd.currentIndex = 0;
            }

            renderPassInfo[workContext].Clear();
        }

        bool inRenderPass = false;
        public void BeginRenderPass(RenderView view)
        {
            BeginRenderPass(Framebuffers[Graphics.WorkImage], view.ViewRect, ClearColorValue, ClearDepthStencilValue);
        }

        public void BeginRenderPass(Framebuffer framebuffer, Rect2D renderArea, params ClearValue[] clearValues)
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
                commandList = new List<FastList<CommandBuffer>> { commdListPool[Graphics.WorkImage].Request() }
            };

            renderPassInfo[Graphics.WorkImage].Add(rpInfo);
        }

        public ref RenderPassInfo CurrentRenderPass => ref renderPassInfo[Graphics.WorkImage].Back();

        public void NextSubpass()
        {
            CurrentRenderPass.NextSubpass(commdListPool[Graphics.WorkImage]);
        }

        public void EndRenderPass(RenderView view)
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


            for (int i = 0; i < Subpasses.Count; i++)
            {
                cmdBuffer = GetCmdBuffer();

                cmdBuffer.SetViewport(ref view.Viewport);
                cmdBuffer.SetScissor(view.ViewRect);
                var action = Subpasses[i];
                action.Invoke(this, view);

                if (i != Subpasses.Count - 1)
                {
                    NextSubpass();
                }

                cmdBuffer?.End();
                cmdBuffer = null;

            }

            EndRenderPass(view);
        }

        public void DrawBatches(RenderView view, FastList<SourceBatch> batches, CommandBuffer cb, ResourceSet set0, ResourceSet set1 = null, ResourceSet set2 = null)
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
                    cb.SetViewport(ref view.Viewport);
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

        public void DrawFullScreenQuad(CommandBuffer cb, Material material)
        {
            var shader = material.Shader;
            var pass = shader.GetPass(passID);
            var pipe = pass.GetGraphicsPipeline(RenderPass, null);

            cb.BindPipeline(PipelineBindPoint.Graphics, pipe);

            material.Bind(pass.passIndex, cb);

            cb.Draw(3, 1, 0, 0);
        }

        public void DrawBatch(ulong passID, CommandBuffer cb, SourceBatch batch, Span<ConstBlock> pushConsts,
            ResourceSet resourceSet, ResourceSet resourceSet1, ResourceSet resourceSet2 = null)
        {
            var shader = batch.material.Shader;
            if ((passID & shader.passFlags) == 0)
            {
                return;
            }
            
            var pass = shader.GetPass(passID);
            var pipe = pass.GetGraphicsPipeline(RenderPass, batch.geometry);

            cb.BindPipeline(PipelineBindPoint.Graphics, pipe);
            cb.BindGraphicsResourceSet(pass.PipelineLayout, 0, resourceSet, batch.offset);

            if (resourceSet1 != null)
            {
                cb.BindGraphicsResourceSet(pass.PipelineLayout, 1, resourceSet1, -1);
            }

            if (resourceSet2 != null)
            {
                cb.BindGraphicsResourceSet(pass.PipelineLayout, 2, resourceSet2, -1);
            }

            foreach (ConstBlock constBlock in pushConsts)
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
                    rp.Free(commdListPool[imageIndex]);
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

        public int currentSubpass;
        public List<FastList<CommandBuffer>> commandList;

        public void AddCommandBuffer(CommandBuffer cb)
        {
            lock (commandList)
            {
                commandList[currentSubpass].Add(cb);
            }
        }

        public void NextSubpass(FastListPool<CommandBuffer> pool)
        {
            commandList.Add(pool.Request());
            currentSubpass++;
        }

        public void Submit(int imageIndex)
        {
            CommandBuffer cb = Graphics.Instance.RenderCmdBuffer;

            System.Diagnostics.Debug.Assert(imageIndex == workImage);

            cb.BeginRenderPass(ref rpBeginInfo, SubpassContents.SecondaryCommandBuffers);

            int i = 0;
            foreach (var subpass in commandList)
            {
                foreach (var cmd in subpass)
                {
                    cb.ExecuteCommand(cmd);
                }

                if (i != commandList.Count - 1)
                {
                    cb.NextSubpass(SubpassContents.SecondaryCommandBuffers);
                }
                i++;
            }

            cb.EndRenderPass();

        }

        public void Free(FastListPool<CommandBuffer> pool)
        {
            foreach (var subpass in commandList)
            {
                pool.Free(subpass);
            }
        }
    }


}
