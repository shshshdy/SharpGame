using System.Runtime.Serialization;

namespace SharpGame
{
 
    public class FrameGraphPass : Object
    {
        private StringID name;
        public StringID Name
        {
            get => name;
            set
            {
                name = value;
                passID = Pass.GetID(name);
            }
        }

        public ulong passID;

        [IgnoreDataMember]
        public FrameGraph FrameGraph { get; set; }

        [IgnoreDataMember]
        public Framebuffer[] framebuffers;

        protected CommandBuffer[] cmdBuffers = new CommandBuffer[3];

        protected CommandBuffer cmdBuffer;
        public CommandBuffer CmdBuffer => cmdBuffer;

        protected RenderPass renderPass;

        public FrameGraphPass()
        {
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

        protected void Begin(RenderView view)
        {
            var graphics = Graphics.Instance;

            if(renderPass == null)
            {
                renderPass = graphics.RenderPass;
            }

            if(framebuffers.IsNullOrEmpty())
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

            cmdBuffer.Begin(/*CommandBufferUsageFlags.OneTimeSubmit |*/ CommandBufferUsageFlags.RenderPassContinue
                /*| CommandBufferUsageFlags.SimultaneousUse*/, ref inherit);

            OnBegin(view);

            this.SendGlobalEvent(new BeginRenderPass { renderPass = this});

            //System.Diagnostics.Debug.Assert(cmdBuffers_[imageIndex] == null);
            cmdBuffers[workContext] = cmdBuffer;

        }

        public void Draw(RenderView view)
        {
            Begin(view);

            OnDraw(view);

            End(view);
        }

        protected void End(RenderView view)
        {
            this.SendGlobalEvent(new EndRenderPass { renderPass = this });

            OnEnd(view);

            cmdBuffer?.End();
            cmdBuffer = null;
        }

        protected virtual void OnBegin(RenderView view)
        {
        }

        protected virtual void OnDraw(RenderView view)
        {
        }

        protected virtual void OnEnd(RenderView view)
        {
        }

        public void Summit(int imageIndex)
        {
            var graphics = Graphics.Instance;
 
            CommandBuffer cb = graphics.RenderCmdBuffer;
            var fbs = framebuffers ?? graphics.Framebuffers;
            int renderContext = graphics.RenderContext;
            var fb = /*fbs[renderContext];*/ fbs[imageIndex];
            var renderPassBeginInfo = new RenderPassBeginInfo
            (
                fb.renderPass, fb,
                new Rect2D(0, 0, graphics.Width, graphics.Height),
                new ClearColorValue(0.25f, 0.25f, 0.25f, 1.0f),
                new ClearDepthStencilValue(1.0f, 0)
            );

            cb.BeginRenderPass(ref renderPassBeginInfo, SubpassContents.SecondaryCommandBuffers);
            if (cmdBuffers[renderContext] != null)
            {
                cmdBuffers[renderContext] = null;
            }

            cb.EndRenderPass();
        }
    }


}
