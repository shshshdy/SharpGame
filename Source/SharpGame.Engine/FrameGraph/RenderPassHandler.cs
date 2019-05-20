using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using Vulkan;

namespace SharpGame
{
 
    public class RenderPassHandler : Object
    {
        private StringID name_;
        public StringID Name
        {
            get => name_;
            set
            {
                name_ = value;
                passID = Pass.GetID(name_);
            }
        }

        public int passID;

        [IgnoreDataMember]
        public FrameGraph FrameGraph { get; set; }

        [IgnoreDataMember]
        public Framebuffer[] framebuffers;

        protected CommandBuffer[] cmdBuffers = new CommandBuffer[2];

        protected CommandBuffer cmdBuffer_;
        public CommandBuffer CmdBuffer => cmdBuffer_;

        internal RenderPass renderPass;

        public RenderPassHandler()
        {
        }

        public void DrawBatch(SourceBatch batch, Pipeline pipeline, ResourceSet resourceSet)
        {/*
            var shader = batch.material_.Shader;
            var pipe = pipeline.GetGraphicsPipeline(this, shader, batch.geometry_);
            cmdBuffer_.CmdBindDescriptorSet(PipelineBindPoint.Graphics, pipeline.pipelineLayout, resourceSet.descriptorSet);
            cmdBuffer_.CmdBindPipeline(PipelineBindPoint.Graphics, pipe);
            batch.geometry_.Draw(cmdBuffer_);*/
        }

        protected CommandBuffer BeginDraw()
        {
            /*
            int workContext = Graphics.WorkContext;

            CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
            {
                Framebuffer = framebuffer_[workContext],
                RenderPass = renderPass_
            };

            cmdBuffer_ = Graphics.SecondaryCmdBuffers[workContext].Get();
            cmdBuffer_.Begin(new CommandBufferBeginInfo(CommandBufferUsages.OneTimeSubmit | CommandBufferUsages.RenderPassContinue
                | CommandBufferUsages.SimultaneousUse, inherit));

            this.SendGlobalEvent(new BeginRenderPass { renderPass = this});

            //System.Diagnostics.Debug.Assert(cmdBuffers_[imageIndex] == null);
            cmdBuffers_[workContext] = cmdBuffer_;
            */

            return cmdBuffer_;
        }

        public void Draw(RenderView view)
        {
            BeginDraw();

            OnDraw(view);

            EndDraw();
        }

        protected void EndDraw()
        {
            this.SendGlobalEvent(new EndRenderPass { renderPass = this });

            cmdBuffer_?.End();
            cmdBuffer_ = null;
        }

        protected virtual void OnDraw(RenderView view)
        {
        }

        public void Summit(int imageIndex)
        {
            var graphics = Graphics.Instance;
 
            CommandBuffer cmdBuffer = graphics.RenderCmdBuffer;

            var fb = framebuffers ?? graphics.Framebuffers;

            var renderPassBeginInfo = new RenderPassBeginInfo
            (
                fb[imageIndex],
                new Rect2D(0, 0, Graphics.Width, Graphics.Height),
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
