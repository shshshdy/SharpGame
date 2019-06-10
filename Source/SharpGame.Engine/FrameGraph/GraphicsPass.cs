using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Vulkan;


namespace SharpGame
{
    public class SubPass
    {
        public Task t;
    }

    public class GraphicsPass : FrameGraphPass
    {
        [IgnoreDataMember]
        public Framebuffer[] framebuffers;

        protected CommandBuffer[] cmdBuffers = new CommandBuffer[3];

        public ClearColorValue ClearColorValue { get; set; } = new ClearColorValue(0.25f, 0.25f, 0.25f, 1);
        public ClearDepthStencilValue ClearDepthStencilValue { get; set; } = new ClearDepthStencilValue(1.0f, 0);

        public Action<RenderView> ActionBegin { get; set; }
        public Action<RenderView> ActionDraw { get; set; }
        public Action<RenderView> ActionEnd { get; set; }

        protected void Begin(RenderView view)
        {
            var graphics = Graphics.Instance;

            if (renderPass == null)
            {
                renderPass = graphics.RenderPass;
            }

            if (framebuffers.IsNullOrEmpty())
            {
                framebuffers = graphics.Framebuffers;
            }

            int workContext = graphics.WorkContext;
            CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
            {
                framebuffer = framebuffers[graphics.SingleLoop ? graphics.nextImage : workContext],
                renderPass = renderPass
            };

            cmdBuffer = graphics.WorkCmdPool.Get();
            cmdBuffer.renderPass = renderPass;

            cmdBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmit | CommandBufferUsageFlags.RenderPassContinue
                | CommandBufferUsageFlags.SimultaneousUse, ref inherit);

            ActionBegin?.Invoke(view);

            this.SendGlobalEvent(new BeginRenderPass { renderPass = this });

            //System.Diagnostics.Debug.Assert(cmdBuffers_[imageIndex] == null);
            cmdBuffers[workContext] = cmdBuffer;

        }

        public override void Draw(RenderView view)
        {
            Begin(view);

            ActionDraw?.Invoke(view);

            End(view);
        }

        protected void End(RenderView view)
        {
            this.SendGlobalEvent(new EndRenderPass { renderPass = this });

            ActionEnd?.Invoke(view);
            cmdBuffer?.End();
            cmdBuffer = null;
        }

        protected void OnBegin(RenderView view)
        {
            cmdBuffer.SetViewport(ref view.Viewport);
            cmdBuffer.SetScissor(new Rect2D(0, 0, (int)view.Viewport.width, (int)view.Viewport.height));
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
            var graphics = Graphics.Instance;
            CommandBuffer cb = graphics.RenderCmdBuffer;
            var fbs = framebuffers ?? graphics.Framebuffers;
            int renderContext = graphics.RenderContext;
            var fb = fbs[imageIndex];
            var renderPassBeginInfo = new RenderPassBeginInfo
            (
                fb.renderPass, fb,
                new Rect2D(0, 0, graphics.Width, graphics.Height),
                ClearColorValue, ClearDepthStencilValue
            );

            cb.BeginRenderPass(ref renderPassBeginInfo, SubpassContents.SecondaryCommandBuffers);

            ref CommandBuffer secondaryCB = ref cmdBuffers[renderContext];
            if (secondaryCB != null)
            {
                cb.ExecuteCommand(secondaryCB);
                secondaryCB = null;
            }

            cb.EndRenderPass();
        }

    }


}
