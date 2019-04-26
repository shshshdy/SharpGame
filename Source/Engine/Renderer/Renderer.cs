using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;
using VulkanCore.Khr;

namespace SharpGame
{
    public class Renderer : Object
    {
        public Graphics Graphics => Get<Graphics>();

        public RenderPass MainRenderPass => MainView.RenderPass;

        public View MainView { get; private set; }

        private List<View> views_ = new List<View>();

        public Texture DepthStencilBuffer => depthStencilBuffer_;
        private Texture depthStencilBuffer_;

        public Renderer()
        {
        }

        public void Inialize()
        {
            depthStencilBuffer_ = Graphics.ToDisposeFrame(Texture.CreateDepthStencil(Graphics.Width, Graphics.Height));
            MainView = CreateViewport();
        }
        
        public View CreateViewport()
        {
            var view = new View();
            views_.Add(view);
            return view;
        }

        public void RenderUpdate()
        {
            SendGlobalEvent(new BeginRender());


            foreach (var view in views_)
            {
                view.Update();
            }

            SendGlobalEvent(new EndRender());
        }

        public void Render()
        {
            // Acquire an index of drawing image for this frame.
            int imageIndex = Graphics.Swapchain.AcquireNextImage(semaphore: Graphics.ImageAvailableSemaphore);

            Graphics.BeginRender();
            // Use a fence to wait until the command buffer has finished execution before using it again
            Graphics.SubmitFences[imageIndex].Wait();
            Graphics.SubmitFences[imageIndex].Reset();

            CommandBuffer cmdBuffer = Graphics.PrimaryCmdBuffers[imageIndex];
            var subresourceRange = new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1);

            cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.SimultaneousUse));

            if (Graphics.PresentQueue != Graphics.GraphicsQueue)
            {
                var barrierFromPresentToDraw = new ImageMemoryBarrier(
                    Graphics.SwapchainImages[imageIndex], subresourceRange,
                    Accesses.MemoryRead, Accesses.ColorAttachmentWrite,
                    ImageLayout.Undefined, ImageLayout.PresentSrcKhr,
                    Graphics.PresentQueue.FamilyIndex, Graphics.GraphicsQueue.FamilyIndex);

                cmdBuffer.CmdPipelineBarrier(
                    PipelineStages.ColorAttachmentOutput,
                    PipelineStages.ColorAttachmentOutput,
                    imageMemoryBarriers: new[] { barrierFromPresentToDraw });
            }

            foreach (var view in views_)
            {
                view.Summit(imageIndex);
            }
            
            if (Graphics.PresentQueue != Graphics.GraphicsQueue)
            {
                var barrierFromDrawToPresent = new ImageMemoryBarrier(
                    Graphics.SwapchainImages[imageIndex], subresourceRange,
                    Accesses.ColorAttachmentWrite, Accesses.MemoryRead,
                    ImageLayout.PresentSrcKhr, ImageLayout.PresentSrcKhr,
                    Graphics.GraphicsQueue.FamilyIndex, Graphics.PresentQueue.FamilyIndex);

                cmdBuffer.CmdPipelineBarrier(
                    PipelineStages.ColorAttachmentOutput,
                    PipelineStages.BottomOfPipe,
                    imageMemoryBarriers: new[] { barrierFromDrawToPresent });
            }

            cmdBuffer.End();

            // Submit recorded commands to graphics queue for execution.
            Graphics.GraphicsQueue.Submit(
                Graphics.ImageAvailableSemaphore,
                PipelineStages.ColorAttachmentOutput,
                Graphics.PrimaryCmdBuffers[imageIndex],
                Graphics.RenderingFinishedSemaphore,
                Graphics.SubmitFences[imageIndex]
            );

            Graphics.EndRender();

            // Present the color output to screen.
            Graphics.PresentQueue.PresentKhr(Graphics.RenderingFinishedSemaphore, Graphics.Swapchain, imageIndex);


        }

    }
}
