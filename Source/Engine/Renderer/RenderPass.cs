﻿using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class RenderPass : GPUObject
    {
        public AttachmentDescription[] attachments { get; set; }
        public SubpassDescription[] subpasses { get; set; }

        private Texture depthStencilBuffer_;
        private Framebuffer[] framebuffer_;

        internal VulkanCore.RenderPass renderPass_;

        public RenderPass()
        {
            Recreate();
        }

        protected override void Recreate()
        {
            depthStencilBuffer_ = Graphics.ToDisposeFrame(Texture.DepthStencil(Graphics.Width, Graphics.Height));

            attachments = new[]
            {
                // Color attachment.
                new AttachmentDescription
                {
                    Format = Graphics.Swapchain.Format,
                    Samples = SampleCounts.Count1,
                    LoadOp = AttachmentLoadOp.Clear,
                    StoreOp = AttachmentStoreOp.Store,
                    StencilLoadOp = AttachmentLoadOp.DontCare,
                    StencilStoreOp = AttachmentStoreOp.DontCare,
                    InitialLayout = ImageLayout.Undefined,
                    FinalLayout = ImageLayout.PresentSrcKhr
                },
                // Depth attachment.
                new AttachmentDescription
                {
                    Format = depthStencilBuffer_.Format,
                    Samples = SampleCounts.Count1,
                    LoadOp = AttachmentLoadOp.Clear,
                    StoreOp = AttachmentStoreOp.DontCare,
                    StencilLoadOp = AttachmentLoadOp.DontCare,
                    StencilStoreOp = AttachmentStoreOp.DontCare,
                    InitialLayout = ImageLayout.Undefined,
                    FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
                }
            };

            subpasses = new[]
            {
                new SubpassDescription(
                    new[] { new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal) },
                    new AttachmentReference(1, ImageLayout.DepthStencilAttachmentOptimal))
            };

            var dependencies = new[]
            {
                new SubpassDependency
                {
                    SrcSubpass = Constant.SubpassExternal,
                    DstSubpass = 0,
                    SrcStageMask = PipelineStages.BottomOfPipe,
                    DstStageMask = PipelineStages.ColorAttachmentOutput,
                    SrcAccessMask = Accesses.MemoryRead,
                    DstAccessMask = Accesses.ColorAttachmentRead | Accesses.ColorAttachmentWrite,
                    DependencyFlags = Dependencies.ByRegion
                },
                new SubpassDependency
                {
                    SrcSubpass = 0,
                    DstSubpass = Constant.SubpassExternal,
                    SrcStageMask = PipelineStages.ColorAttachmentOutput,
                    DstStageMask = PipelineStages.BottomOfPipe,
                    SrcAccessMask = Accesses.ColorAttachmentRead | Accesses.ColorAttachmentWrite,
                    DstAccessMask = Accesses.MemoryRead,
                    DependencyFlags = Dependencies.ByRegion
                }
            };

            var createInfo = new RenderPassCreateInfo(subpasses, attachments, dependencies);
            renderPass_ = Graphics.ToDisposeFrame(Graphics.Device.CreateRenderPass(createInfo));

            framebuffer_ = Graphics.ToDisposeFrame(CreateFramebuffers());

        }

        private Framebuffer[] CreateFramebuffers()
        {
            var framebuffers = new Framebuffer[Graphics.SwapchainImages.Length];
            for (int i = 0; i < Graphics.SwapchainImages.Length; i++)
            {
                framebuffers[i] = renderPass_.CreateFramebuffer(new FramebufferCreateInfo(
                    new[] { Graphics.SwapchainImageViews[i], depthStencilBuffer_.View },
                    Graphics.Width,
                    Graphics.Height));
            }
            return framebuffers;
        }

        public void Begin(CommandBuffer cmdBuffer, int imageIndex)
        {
            var renderPassBeginInfo = new RenderPassBeginInfo
            (
                framebuffer_[imageIndex], new Rect2D(Offset2D.Zero, new Extent2D(Graphics.Width, Graphics.Height)),
                new ClearColorValue(new ColorF4(0.39f, 0.58f, 0.93f, 1.0f)),
                new ClearDepthStencilValue(1.0f, 0)
            );

            cmdBuffer.CmdBeginRenderPass(renderPassBeginInfo);

            SendGlobalEvent(new RenderPassBegin { commandBuffer = cmdBuffer, imageIndex = imageIndex });
        }

        public void Draw(CommandBuffer cmdBuffer, int imageIndex)
        {
            Begin(cmdBuffer, imageIndex);
            OnDraw(cmdBuffer, imageIndex);
            End(cmdBuffer, imageIndex);
        }

        protected virtual void OnDraw(CommandBuffer cmdBuffer, int imageIndex)
        {

        }

        public void End(CommandBuffer cmdBuffer, int imageIndex)
        {
            cmdBuffer.CmdEndRenderPass();
            SendGlobalEvent(new RenderPassEnd { commandBuffer = cmdBuffer, imageIndex = imageIndex });
        }
    }
}
