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

        protected CommandBufferPool[] cmdBufferPool;

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

        public GraphicsPass(string name = "")
        {
            Name = name;

            cmdBufferPool = new CommandBufferPool[3];

            for (int i = 0; i < 3; i++)
            {
                cmdBufferPool[i] = new CommandBufferPool(Graphics.Instance.Swapchain.QueueNodeIndex, CommandPoolCreateFlags.ResetCommandBuffer);
                cmdBufferPool[i].Name = $"GraphicsPass {Name} CmdPool {i}";
                cmdBufferPool[i].Allocate(CommandBufferLevel.Secondary, 8);
            }
        }

        public void Add(Action<GraphicsPass, RenderView> subpass)
        {
            Subpasses.Add(subpass);
        }

        protected CommandBuffer GetCmdBuffer()
        {
            int workContext = Graphics.nextImage;
            var cb = cmdBufferPool[workContext].Get();
            cb.renderPass = renderPass;

            CurrentRenderPass.AddCommandBuffer(cb);

            CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
            {
                framebuffer = CurrentRenderPass.Framebuffer,
                renderPass = renderPass
            };

            cb.Begin(CommandBufferUsageFlags.OneTimeSubmit | CommandBufferUsageFlags.RenderPassContinue
                | CommandBufferUsageFlags.SimultaneousUse, ref inherit);

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

            int workContext = Graphics.nextImage;

            cmdBufferPool[workContext].currentIndex = 0;
            renderPassInfo[workContext].Clear();
        }


        bool inRenderPass = false;
        protected void BeginRenderPass(RenderView view)
        {
            BeginRenderPass(framebuffers[Graphics.nextImage], view.ViewRect, ClearColorValue, ClearDepthStencilValue);
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
                nextImage = Graphics.nextImage,
                rpBeginInfo = new RenderPassBeginInfo(framebuffer.renderPass, framebuffer, renderArea, clearValues),
                commandList = commdListPool[Graphics.nextImage].Request()
            };

            renderPassInfo[Graphics.nextImage].Add(rpInfo);
        }

        protected ref RenderPassInfo CurrentRenderPass => ref renderPassInfo[Graphics.nextImage].Back();

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

        public void DrawFullScreenQuad(CommandBuffer cb, Material material)
        {
            var shader = material.Shader;
            var pass = shader.GetPass(passID);
            var pipe = pass.GetGraphicsPipeline(renderPass, null);

            cb.BindPipeline(PipelineBindPoint.Graphics, pipe);

            material.Bind(pass.passIndex, cb);

            cb.Draw(3, 1, 0, 0);
        }

        public void DrawBatch(CommandBuffer cb, SourceBatch batch, ResourceSet resourceSet, ResourceSet resourceSet1, uint? offset = null, uint? offset1 = null)
        {
            var shader = batch.material.Shader;
            if ((passID & shader.passFlags) == 0)
            {
                return;
            }
            
            var pass = shader.GetPass(passID);
            var pipe = pass.GetGraphicsPipeline(renderPass, batch.geometry);

            cb.BindPipeline(PipelineBindPoint.Graphics, pipe);

            cb.BindGraphicsResourceSet(pass.PipelineLayout, resourceSet.Set, resourceSet, offset);

            if (resourceSet1 != null && (pass.PipelineLayout.DefaultResourcSet & DefaultResourcSet.PS) != 0)
            {
                cb.BindGraphicsResourceSet(pass.PipelineLayout, resourceSet1.Set, resourceSet1, offset1);
            }

            batch.material.Bind(pass.passIndex, cb);
            batch.geometry.Draw(cb);
        }

        public override void Submit(int imageIndex)
        {
            var rpInfo = renderPassInfo[imageIndex];
            foreach (var rp in rpInfo)
            {
                rp.Submit(imageIndex);

                commdListPool[imageIndex].Free(rp.commandList);
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
        public int nextImage;
        public RenderPassBeginInfo rpBeginInfo;
        public Framebuffer Framebuffer => rpBeginInfo.framebuffer;
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

            System.Diagnostics.Debug.Assert(imageIndex == nextImage);

            cb.BeginRenderPass(ref rpBeginInfo, SubpassContents.SecondaryCommandBuffers);

            foreach (var cmd in commandList)
            {
                cb.ExecuteCommand(cmd);
            }

            cb.EndRenderPass();

        }
    }


}
