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

        protected CommandBuffer[] cmdBuffers_ = new CommandBuffer[2];

        public Renderer Renderer => Get<Renderer>();
        internal VulkanCore.RenderPass renderPass_;

        protected override void Recreate()
        {
        }


        protected Framebuffer[] CreateFramebuffers()
        {
            var framebuffers = new Framebuffer[Graphics.SwapchainImages.Length];
            for (int i = 0; i < Graphics.SwapchainImages.Length; i++)
            {
                framebuffers[i] = renderPass_.CreateFramebuffer(
                    new FramebufferCreateInfo(
                        new[] {
                            Graphics.SwapchainImageViews[i],
                            Renderer.DepthStencilBuffer.View
                        },

                        Graphics.Width,
                        Graphics.Height
                    )
                );
            }

            return framebuffers;
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
