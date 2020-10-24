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
    public class GraphicsPass : FrameGraphPass
    {
        [IgnoreDataMember]
        public Framebuffer[] Framebuffers { get; set; }
        [IgnoreDataMember]
        public Framebuffer Framebuffer { set => Framebuffers = new[] { value, value, value }; }
        public ClearColorValue[] ClearColorValue { get; set; } = { new ClearColorValue(0.25f, 0.25f, 0.25f, 1) };
        public ClearDepthStencilValue? ClearDepthStencilValue { get; set; } = new ClearDepthStencilValue(1.0f, 0);

        public Action<GraphicsPass, RenderView> OnDraw { get; set; }

        private ClearValue[] clearValues = new ClearValue[5];
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

        public GraphicsPass(string name = "", int workCount = 0)
        {
            PassQueue = PassQueue.Graphics;
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

        public CommandBuffer GetCmdBuffer(int index = -1)
        {
            int workContext = Graphics.WorkImage;
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

            Clear();
        }

        protected virtual void Clear()
        {
            int workContext = Graphics.WorkImage;

            for (int i = 0; i < cmdBufferPools.Count; i++)
            {
                var cmd = cmdBufferPools[i][workContext];
                cmd.currentIndex = 0;
            }

            renderPassInfo[workContext].Clear();
        }

        bool inRenderPass = false;
        Viewport viewport;
        Rect2D renderArea;
        public void BeginRenderPass(RenderView view)
        {
            Framebuffer framebuffer = null;
            if (Framebuffers == null)
            {
                framebuffer = Graphics.Framebuffers[Graphics.WorkImage];
            }
            else
            {
                framebuffer = Framebuffers[Graphics.WorkImage];
            }

            if (view != null)
            {
                viewport = view.Viewport;
                renderArea = view.ViewRect;
            }
            else
            {
                viewport = new Viewport(0, 0, Graphics.Width, Graphics.Height);
                renderArea = new Rect2D(0, 0, Graphics.Width, Graphics.Height);
            }

            int clearValuesCount = 0;
            if(ClearColorValue != null)
            {
                clearValuesCount = ClearColorValue.Length;
            }

            if(ClearDepthStencilValue.HasValue)
            {
                clearValuesCount += 1;
            }

            if(clearValues.Length != clearValuesCount)
            {
                Array.Resize(ref clearValues, clearValuesCount);
            }

            if (ClearColorValue != null)
            {
                for (int i = 0; i < ClearColorValue.Length; i++)
                {
                    clearValues[i] = ClearColorValue[i];
                }
            }

            if (ClearDepthStencilValue.HasValue)
            {
                clearValues[clearValues.Length - 1] = ClearDepthStencilValue.Value;
            }

            BeginRenderPass(framebuffer, renderArea, clearValues);
        }

        public void BeginRenderPass(Framebuffer framebuffer, Rect2D renderArea, ClearValue[] clearValues)
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
                commandList = new FastList<CommandBuffer>()
            };

            renderPassInfo[Graphics.WorkImage].Add(rpInfo);
            Subpass = 0;
        }

        public ref RenderPassInfo CurrentRenderPass => ref renderPassInfo[Graphics.WorkImage].Back();

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
            cmdBuffer = null;


            Submit(FrameGraph.GetWorkCmdBuffer(PassQueue), (int)Graphics.WorkImage);
        }

        protected virtual void DrawImpl(RenderView view)
        {
            BeginRenderPass(view);

            cmdBuffer = GetCmdBuffer();

            cmdBuffer.SetViewport(in viewport);
            cmdBuffer.SetScissor(in renderArea);

            OnDraw?.Invoke(this, view);

            cmdBuffer.End();
            cmdBuffer = null;


            EndRenderPass(view);
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
            var pipe = pass.GetGraphicsPipeline(RenderPass, Subpass, batch.geometry);

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

        public void DrawFullScreenQuad(Pass pass, CommandBuffer cb, ResourceSet resourceSet, ResourceSet resourceSet1, ResourceSet resourceSet2 = null)
        {
            var pipe = pass.GetGraphicsPipeline(RenderPass, Subpass, null);

            cb.BindPipeline(PipelineBindPoint.Graphics, pipe);
            cb.BindGraphicsResourceSet(pass.PipelineLayout, 0, resourceSet);

            if (resourceSet1 != null)
            {
                cb.BindGraphicsResourceSet(pass.PipelineLayout, 1, resourceSet1);
            }

            if (resourceSet2 != null)
            {
                cb.BindGraphicsResourceSet(pass.PipelineLayout, 2, resourceSet2, -1);
            }

            cb.Draw(3, 1, 0, 0);
        }

        protected override void Submit(CommandBuffer cb, int imageIndex)
        {
            var rpInfo = renderPassInfo[imageIndex];
            if(rpInfo.Count > 0)
            {
                foreach (var rp in rpInfo)
                {
                    rp.Submit(cb, imageIndex);
                    rp.Free(commdListPool[imageIndex]);
                }
            }

        }

    }

    public struct RenderPassInfo
    {
        public int workImage;
        public RenderPassBeginInfo rpBeginInfo;
        public int currentSubpass;
        public FastList<CommandBuffer> commandList;

        public Framebuffer Framebuffer => rpBeginInfo.framebuffer;
        public RenderPass RenderPass => rpBeginInfo.renderPass;

        public void AddCommandBuffer(CommandBuffer cb)
        {
            lock (commandList)
            {
                commandList.Add(cb);
            }
        }

        public void Submit(CommandBuffer cb, int imageIndex)
        {
            System.Diagnostics.Debug.Assert(imageIndex == workImage);

            cb.BeginRenderPass(in rpBeginInfo, SubpassContents.SecondaryCommandBuffers);

            foreach (var cmd in commandList)
            {
                cb.ExecuteCommand(cmd);
            }

            cb.EndRenderPass();

        }

        public void Free(FastListPool<CommandBuffer> pool)
        {
            pool.Free(commandList);
        }
    }


}
