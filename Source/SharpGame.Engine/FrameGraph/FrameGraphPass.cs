using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using Vulkan;

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
            var shader = batch.material.Shader;
            if ((passID & shader.passFlags) == 0)
            {
                return;
            }

            var pass = shader.GetPass(passID);
            var pipeline = batch.material.Pipeline;

            var pipe = pipeline.GetGraphicsPipeline(renderPass, pass, batch.geometry);
            cmdBuffer.BindPipeline(PipelineBindPoint.Graphics, pipe);
            cmdBuffer.PushConstants(pipeline, ShaderStage.Vertex, 0, Utilities.SizeOf<Matrix>(), batch.worldTransform);
            cmdBuffer.BindGraphicsResourceSet(pipeline, 0, resourceSet);
            cmdBuffer.BindGraphicsResourceSet(pipeline, 1, batch.material.ResourceSet);

            batch.geometry.Draw(cmdBuffer);
        }

        protected void BeginDraw(RenderView view)
        {
            var graphics = Graphics.Instance;
            int workContext = graphics.workThread;

            if(renderPass == null)
            {
                renderPass = graphics.RenderPass;
            }

            if(framebuffers.IsNullOrEmpty())
            {
                framebuffers = graphics.Framebuffers;
            }

            CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
            {
                framebuffer = framebuffers[graphics.currentImage],
                renderPass = renderPass
            };

            cmdBuffer = graphics.WorkCmdPool.Get();
            cmdBuffer.renderPass = renderPass;

            cmdBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmit | CommandBufferUsageFlags.RenderPassContinue
                | CommandBufferUsageFlags.SimultaneousUse, ref inherit);

            OnBeginDraw(view);

            this.SendGlobalEvent(new BeginRenderPass { renderPass = this});

            //System.Diagnostics.Debug.Assert(cmdBuffers_[imageIndex] == null);
            cmdBuffers[graphics.currentImage] = cmdBuffer;

        }

        public void Draw(RenderView view)
        {
            BeginDraw(view);

            OnDraw(view);

            EndDraw(view);
        }

        protected void EndDraw(RenderView view)
        {
            this.SendGlobalEvent(new EndRenderPass { renderPass = this });

            OnEndDraw(view);

            cmdBuffer?.End();
            cmdBuffer = null;
        }

        protected virtual void OnBeginDraw(RenderView view)
        {
        }

        protected virtual void OnDraw(RenderView view)
        {
        }

        protected virtual void OnEndDraw(RenderView view)
        {
        }

        public void Summit(int imageIndex)
        {
            var graphics = Graphics.Instance;
 
            CommandBuffer cmdBuffer = graphics.RenderCmdBuffer;

            var fb = framebuffers ?? graphics.Framebuffers;

            var renderPassBeginInfo = new RenderPassBeginInfo
            (
                fb[imageIndex].renderPass,
                fb[imageIndex],
                new Rect2D(0, 0, graphics.Width, graphics.Height),
                new ClearColorValue(0.25f, 0.25f, 0.25f, 1.0f),
                new ClearDepthStencilValue(1.0f, 0)
            );

            cmdBuffer.BeginRenderPass(ref renderPassBeginInfo, SubpassContents.SecondaryCommandBuffers);
      
            if (cmdBuffers[imageIndex] != null)
            {
                cmdBuffer.ExecuteCommand(cmdBuffers[imageIndex]);
                cmdBuffers[imageIndex] = null;
            }

            cmdBuffer.EndRenderPass();
        }
    }


}
