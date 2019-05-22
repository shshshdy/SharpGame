﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using Vulkan;

namespace SharpGame
{
 
    public class PassHandler : Object
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

        protected CommandBuffer[] cmdBuffers = new CommandBuffer[2];

        protected CommandBuffer cmdBuffer;
        public CommandBuffer CmdBuffer => cmdBuffer;

        protected RenderPass renderPass;

        public PassHandler()
        {
        }

        public void DrawBatch(SourceBatch batch, Pipeline pipeline, ResourceSet resourceSet)
        {
            var shader = batch.material.Shader;
            if ((passID & shader.passFlags) == 0)
            {
                return;
            }

            var pass = shader.GetPass(passID);
            cmdBuffer.DrawGeometry(batch.geometry, pipeline, pass, resourceSet);
        }

        protected void BeginDraw()
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
                framebuffer = framebuffers[graphics.currentBuffer],
                renderPass = renderPass
            };

            cmdBuffer = graphics.WorkCmdPool.Get();
            cmdBuffer.renderPass = renderPass;
            var cmdBeginInfo = new CommandBufferBeginInfo(CommandBufferUsageFlags.OneTimeSubmit | CommandBufferUsageFlags.RenderPassContinue
                | CommandBufferUsageFlags.SimultaneousUse, inherit);
            cmdBuffer.Begin(ref cmdBeginInfo);

            this.SendGlobalEvent(new BeginRenderPass { renderPass = this});

            //System.Diagnostics.Debug.Assert(cmdBuffers_[imageIndex] == null);
            cmdBuffers[graphics.currentBuffer] = cmdBuffer;
            
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

            cmdBuffer?.End();
            cmdBuffer = null;
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
