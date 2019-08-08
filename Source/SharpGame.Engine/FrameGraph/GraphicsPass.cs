using System;
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
        public Framebuffer[] framebuffers;

        public ClearColorValue ClearColorValue { get; set; } = new ClearColorValue(0.25f, 0.25f, 0.25f, 1);
        public ClearDepthStencilValue ClearDepthStencilValue { get; set; } = new ClearDepthStencilValue(1.0f, 0);
        public Action<GraphicsPass, RenderView> OnDraw { get; set; }

        private CommandBufferPool[] cmdBufferPool;


        public GraphicsPass()
        {
            cmdBufferPool = new CommandBufferPool[3];

            for (int i = 0; i < 3; i++)
            {
                cmdBufferPool[i] = new CommandBufferPool(Graphics.Instance.Swapchain.QueueNodeIndex, VkCommandPoolCreateFlags.ResetCommandBuffer);
                cmdBufferPool[i].Allocate(CommandBufferLevel.Secondary, 8);
            }
        }

        protected CommandBuffer GetCmdBuffer()
        {
            var g = Graphics.Instance;
            int workContext = g.WorkContext;
            var cb = cmdBufferPool[workContext].Get();
            cb.renderPass = renderPass;

            CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
            {
                framebuffer = framebuffers[g.nextImage],
                renderPass = renderPass
            };

            cb.Begin(CommandBufferUsageFlags.OneTimeSubmit | CommandBufferUsageFlags.RenderPassContinue
                | CommandBufferUsageFlags.SimultaneousUse, ref inherit);

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
            cmdBuffer = GetCmdBuffer();

            cmdBuffer.SetViewport(ref view.Viewport);
            cmdBuffer.SetScissor(new Rect2D(0, 0, (int)view.Viewport.width, (int)view.Viewport.height));
            OnDraw?.Invoke(this, view);
            cmdBuffer?.End();
            cmdBuffer = null;
        }

        public void DrawBatch(CommandBuffer cb, SourceBatch batch, ResourceSet resourceSet, uint? offset = null)
        {
            var shader = batch.material.Shader;
            if ((passID & shader.passFlags) == 0)
            {
                return;
            }
            
            var pass = shader.GetPass(passID);
            var pipe = pass.GetGraphicsPipeline(renderPass, batch.geometry);

            cb.BindPipeline(PipelineBindPoint.Graphics, pipe);
            cb.BindGraphicsResourceSet(pass, resourceSet.Set, resourceSet, offset);

            //cb.PushConstants(pass, ShaderStage.Vertex, 0, Utilities.SizeOf<Matrix>(), batch.worldTransform);
            foreach(var rs in batch.material.ResourceSet)
            {
                cb.BindGraphicsResourceSet(pass, rs.Set, rs);
            }

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

            cb.ExecuteCommand(cmdBufferPool[renderContext].CommandBuffers[0]);
            
            cb.EndRenderPass();
        }

    }


}
