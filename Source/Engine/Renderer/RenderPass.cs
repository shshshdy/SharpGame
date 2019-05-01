using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public struct Batch
    {
        public Material material;
        public Geometry geometry;
    }

    public class RenderPass : GPUObject
    {
        public StringID Name { get; set; }

        [IgnoreDataMember]
        public RenderPath RenderPath { get; set; }

        [IgnoreDataMember]
        public Framebuffer[] framebuffer_;

        public Renderer Renderer => Get<Renderer>();

        protected CommandBuffer[] cmdBuffers_ = new CommandBuffer[2];

        internal VulkanCore.RenderPass renderPass_;

        public RenderPass()
        {
        }

        protected override void Recreate()
        {
        }

        public Framebuffer CreateFramebuffer(ImageView[] attachments, int width, int height, int layers = 1)
        {
            return renderPass_.CreateFramebuffer(
                    new FramebufferCreateInfo(attachments, width, height)
                );
        }

        protected virtual CommandBuffer BeginDraw()
        {
            int workContext = Graphics.WorkContext;

            CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
            {
                Framebuffer = framebuffer_[workContext],
                RenderPass = renderPass_
            };

            CommandBuffer cmdBuffer = Graphics.SecondaryCmdBuffers[workContext].Get();
            cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.OneTimeSubmit | CommandBufferUsages.RenderPassContinue
                | CommandBufferUsages.SimultaneousUse, inherit));

            SendGlobalEvent(new BeginRenderPass { renderPass = this, commandBuffer = cmdBuffer });

            //System.Diagnostics.Debug.Assert(cmdBuffers_[imageIndex] == null);
            cmdBuffers_[workContext] = cmdBuffer;

            return cmdBuffer;
        }

        public void Draw(View view)
        {
            CommandBuffer cmdBuffer = BeginDraw();

            OnDraw(view, cmdBuffer);

            EndDraw(cmdBuffer);
        }

        protected virtual void EndDraw(CommandBuffer cmdBuffer)
        {
            SendGlobalEvent(new EndRenderPass { renderPass = this, commandBuffer = cmdBuffer });

            cmdBuffer.End();
        }

        protected virtual void OnDraw(View view, CommandBuffer cmdBuffer)
        {
        }

        public void Summit(int imageIndex)
        {
            CommandBuffer cmdBuffer = Graphics.PrimaryCmdBuffers[imageIndex];

            var renderPassBeginInfo = new RenderPassBeginInfo
            (
                framebuffer_[imageIndex], new Rect2D(Offset2D.Zero, new Extent2D(Graphics.Width, Graphics.Height)),
                new ClearColorValue(new ColorF4(0.0f, 0.0f, 0.0f, 1.0f)),
                new ClearDepthStencilValue(1.0f, 0)
            );

            cmdBuffer.CmdBeginRenderPass(renderPassBeginInfo, SubpassContents.SecondaryCommandBuffers);
            if (cmdBuffers_[imageIndex] != null)
            {
                cmdBuffer.CmdExecuteCommand(cmdBuffers_[imageIndex]);
                cmdBuffers_[imageIndex] = null;
            }

            cmdBuffer.CmdEndRenderPass();
        }
    }
}
