using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Vulkan;


namespace SharpGame
{
    public struct DebugInfo
    {
        public int nextImage;
        public Framebuffer framebuffer;
    }

    public class GraphicsPass : FrameGraphPass
    {
        [IgnoreDataMember]
        public Framebuffer[] framebuffers;

        public ClearColorValue ClearColorValue { get; set; } = new ClearColorValue(0.25f, 0.25f, 0.25f, 1);
        public ClearDepthStencilValue ClearDepthStencilValue { get; set; } = new ClearDepthStencilValue(1.0f, 0);
        public Action<GraphicsPass, RenderView> OnDraw { get; set; }

        protected CommandBufferPool[] cmdBufferPool;

        protected DebugInfo[] debugInfo = new DebugInfo[3];

        public GraphicsPass(string name = "")
        {
            Name = name;

            cmdBufferPool = new CommandBufferPool[3];

            for (int i = 0; i < 3; i++)
            {
                cmdBufferPool[i] = new CommandBufferPool(Graphics.Instance.Swapchain.QueueNodeIndex, CommandPoolCreateFlags.ResetCommandBuffer);
                cmdBufferPool[i].Name = "GraphicsPass" + i;
                cmdBufferPool[i].Allocate(CommandBufferLevel.Secondary, 8);
            }
        }

        protected CommandBuffer GetCmdBuffer()
        {
            var g = Graphics.Instance;
            int workContext = g.nextImage;
            var cb = cmdBufferPool[workContext].Get();
            cb.renderPass = renderPass;

            CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
            {
                framebuffer = framebuffers[g.nextImage],
                renderPass = renderPass
            };

            cb.Begin(CommandBufferUsageFlags.OneTimeSubmit | CommandBufferUsageFlags.RenderPassContinue
                | CommandBufferUsageFlags.SimultaneousUse, ref inherit);

            ref DebugInfo info = ref debugInfo[g.nextImage];
            info.nextImage = g.nextImage;
            info.framebuffer = framebuffers[g.nextImage];
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

            int workContext = g.nextImage;
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
            cmdBuffer.SetScissor(view.ViewRect);
            OnDraw?.Invoke(this, view);
            cmdBuffer?.End();
            cmdBuffer = null;
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
            var g = Graphics.Instance;
            CommandBuffer cb = g.RenderCmdBuffer;
            var fbs = framebuffers ?? g.Framebuffers;
            int renderContext = imageIndex;// g.RenderContext;
            var fb = fbs[imageIndex];

            var renderPassBeginInfo = new RenderPassBeginInfo
            (
                fb.renderPass, fb,
                new Rect2D(0, 0, g.Width, g.Height),
                ClearColorValue, ClearDepthStencilValue
            );

            cb.BeginRenderPass(ref renderPassBeginInfo, SubpassContents.SecondaryCommandBuffers);
            var cmdPool = cmdBufferPool[renderContext];
            if(cmdPool.currentIndex > 0)
            {
                System.Diagnostics.Debug.Assert(imageIndex == debugInfo[imageIndex].nextImage);
                //System.Diagnostics.Debug.Assert(fb == debugInfo[renderContext].framebuffer);
            
                cb.ExecuteCommand(cmdPool[0]);
            }
            
            cb.EndRenderPass();
        }

    }


}
