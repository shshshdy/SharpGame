using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Vulkan;


namespace SharpGame
{
    public class PassSumitInfo
    {
        public Framebuffer frameBuffer;
        public CommandBuffer commandBuffers;
    }
    
    public class GraphicsPass : FrameGraphPass
    {
        [IgnoreDataMember]
        public Framebuffer[] framebuffers;

        protected CommandBuffer[] cmdBuffers = new CommandBuffer[3];

        CommandBufferPool[] cmdBufferPool = new CommandBufferPool[2];

        public ClearColorValue ClearColorValue { get; set; } = new ClearColorValue(0.25f, 0.25f, 0.25f, 1);
        public ClearDepthStencilValue ClearDepthStencilValue { get; set; } = new ClearDepthStencilValue(1.0f, 0);

        public Action<RenderView> OnBegin { get; set; }
        public Action<RenderView> OnDraw { get; set; }
        public Action<RenderView> OnEnd { get; set; }

        public GraphicsPass()
        {
            cmdBufferPool = new CommandBufferPool[2]
            {
                new CommandBufferPool(Graphics.Instance.Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer),
                new CommandBufferPool(Graphics.Instance.Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer)
            };

            cmdBufferPool[0].Allocate(CommandBufferLevel.Secondary, 8);
            cmdBufferPool[1].Allocate(CommandBufferLevel.Secondary, 8);
        }

        protected void Begin(RenderView view)
        {
            var g = Graphics.Instance;

            if (renderPass == null)
            {
                renderPass = g.RenderPass;
            }

            if (framebuffers.IsNullOrEmpty())
            {
                framebuffers = g.Framebuffers;
            }

            int workContext = g.WorkContext;

            CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
            {
                framebuffer = framebuffers[g.SingleLoop ? g.nextImage : workContext],
                renderPass = renderPass
            };

            cmdBufferPool[workContext].currentIndex = 0;
            cmdBuffer = cmdBufferPool[workContext].Get(); 

            cmdBuffer.renderPass = renderPass;

            cmdBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmit | CommandBufferUsageFlags.RenderPassContinue
                | CommandBufferUsageFlags.SimultaneousUse, ref inherit);

            cmdBuffer.SetViewport(ref view.Viewport);
            cmdBuffer.SetScissor(new Rect2D(0, 0, (int)view.Viewport.width, (int)view.Viewport.height));

            OnBegin?.Invoke(view);

            this.SendGlobalEvent(new BeginRenderPass { renderPass = this });

            //System.Diagnostics.Debug.Assert(cmdBuffers_[imageIndex] == null);
            cmdBuffers[workContext] = cmdBuffer;

        }

        public override void Draw(RenderView view)
        {
            Begin(view);

            OnDraw?.Invoke(view);

            End(view);
        }

        protected void End(RenderView view)
        {
            this.SendGlobalEvent(new EndRenderPass { renderPass = this });

            OnEnd?.Invoke(view);
            cmdBuffer?.End();
            cmdBuffer = null;
        }

        public void DrawBatch(SourceBatch batch, ResourceSet resourceSet)
        {
            var pipeline = batch.material.Pipeline;
            var shader = pipeline.Shader;
            if ((passID & shader.passFlags) == 0)
            {
                return;
            }

            var pass = shader.GetPass(passID);

            var pipe = pipeline.GetGraphicsPipeline(renderPass, pass, batch.geometry);
            cmdBuffer.BindPipeline(PipelineBindPoint.Graphics, pipe);
            cmdBuffer.PushConstants(pipeline, ShaderStage.Vertex, 0, Utilities.SizeOf<Matrix>(), batch.worldTransform);
            cmdBuffer.BindGraphicsResourceSet(pipeline, 0, resourceSet);
            cmdBuffer.BindGraphicsResourceSet(pipeline, 1, batch.material.ResourceSet);

            batch.geometry.Draw(cmdBuffer);
        }

        public override void Summit(int imageIndex)
        {
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
            cb.ExecuteCommand(cmdBufferPool[renderContext]);
            cb.EndRenderPass();
        }

    }


}
