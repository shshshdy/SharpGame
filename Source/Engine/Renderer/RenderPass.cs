using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class RenderPass : GPUObject
    {
        public string Name { get; set; }

        [IgnoreDataMember]
        public RenderPath RenderPath { get; private set; }

        [IgnoreDataMember]
        public Framebuffer[] framebuffer_;

        public Renderer Renderer => Get<Renderer>();

        protected CommandBuffer[] cmdBuffers_ = new CommandBuffer[2];

        internal VulkanCore.RenderPass renderPass_;

        protected override void Recreate()
        {
        }

        public Framebuffer CreateFramebuffer(ImageView[] attachments, int width, int height, int layers = 1)
        {
            return renderPass_.CreateFramebuffer(
                    new FramebufferCreateInfo(attachments, width, height)
                );
        }

        public virtual void Draw(CommandBuffer cmdBuffer, int imageIndex)
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
