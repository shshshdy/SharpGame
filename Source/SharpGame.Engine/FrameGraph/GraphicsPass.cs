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

        CommandBufferPool[] cmdBufferPool;

        public ClearColorValue ClearColorValue { get; set; } = new ClearColorValue(0.25f, 0.25f, 0.25f, 1);
        public ClearDepthStencilValue ClearDepthStencilValue { get; set; } = new ClearDepthStencilValue(1.0f, 0);

        public Action<RenderView> OnDraw { get; set; }

        public GraphicsPass()
        {
            cmdBufferPool = new CommandBufferPool[3]
            {
                new CommandBufferPool(Graphics.Instance.Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer),
                new CommandBufferPool(Graphics.Instance.Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer),
                new CommandBufferPool(Graphics.Instance.Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer)
            };

            cmdBufferPool[0].Allocate(CommandBufferLevel.Secondary, 8);
            cmdBufferPool[1].Allocate(CommandBufferLevel.Secondary, 8);
            cmdBufferPool[2].Allocate(CommandBufferLevel.Secondary, 8);
        }

        protected CommandBuffer GetCmdBuffer()
        {
            var g = Graphics.Instance;
            int workContext = g.WorkContext;
            var cb = cmdBufferPool[workContext].Get();
            cb.renderPass = renderPass;

            CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
            {
                framebuffer = framebuffers[/*g.SingleLoop ?*/ g.nextImage/* : workContext*/],
                renderPass = renderPass
            };

            cb.Begin(CommandBufferUsageFlags.OneTimeSubmit | CommandBufferUsageFlags.RenderPassContinue
                | CommandBufferUsageFlags.SimultaneousUse, ref inherit);

            //cb.SetViewport(ref view.Viewport);
            //cb.SetScissor(new Rect2D(0, 0, (int)view.Viewport.width, (int)view.Viewport.height));
            return cb;
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
            cmdBufferPool[workContext].currentIndex = 0;
            //this.SendGlobalEvent(new BeginRenderPass { renderPass = this });
        }

        public override void Draw(RenderView view)
        {
            Begin(view);

            DrawImpl(view);

            End(view);
        }

        protected void End(RenderView view)
        {
            //this.SendGlobalEvent(new EndRenderPass { renderPass = this });
        }

        protected virtual void DrawImpl(RenderView view)
        {
            cmdBuffer = GetCmdBuffer();

            OnDraw?.Invoke(view);
            cmdBuffer?.End();
            cmdBuffer = null;
        }

        public void DrawBatch(CommandBuffer cb, SourceBatch batch, ResourceSet resourceSet)
        {
            var pipeline = batch.material.Pipeline;
            var shader = pipeline.Shader;
            if ((passID & shader.passFlags) == 0)
            {
                return;
            }

            var pass = shader.GetPass(passID);
            var pipe = pipeline.GetGraphicsPipeline(renderPass, pass, batch.geometry);

            cb.BindPipeline(PipelineBindPoint.Graphics, pipe);
            cb.PushConstants(pipeline, ShaderStage.Vertex, 0, Utilities.SizeOf<Matrix>(), batch.worldTransform);
            cb.BindGraphicsResourceSet(pipeline, 0, resourceSet);
            cb.BindGraphicsResourceSet(pipeline, 1, batch.material.ResourceSet);

            batch.geometry.Draw(cb);
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
            /*
            for (int i = 0; i < cmdBufferPool[renderContext].currentIndex; i++)
            {
                cb.ExecuteCommand(cmdBufferPool[renderContext].CommandBuffers[i]);
            }*/
            cb.EndRenderPass();
        }

    }


}
