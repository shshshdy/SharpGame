#define USE_WORK_THREAD
#define USE_PRIMARY


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

        public RenderPass MainRenderPass { get; }

        private List<View> views_ = new List<View>();

        public Renderer()
        {
            CreateViewport();

            MainRenderPass = new RenderPass();
        }
        
        public View CreateViewport()
        {
            var view = new View();
            views_.Add(view);
            return view;
        }

        public void RenderUpdate()
        {

#if USE_PRIMARY
            SendGlobalEvent(new BeginRender());
            
            CommandBuffer cmdBuffer = Graphics.PrimaryCmdBuffers[ Graphics.WorkContext]; ;
            cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.SimultaneousUse));

            MainRenderPass.Begin(cmdBuffer,  Graphics.WorkContext);
            MainRenderPass.Draw(cmdBuffer,  Graphics.WorkContext);
            MainRenderPass.End(cmdBuffer,  Graphics.WorkContext);

            cmdBuffer.End();
            SendGlobalEvent(new RenderEnd());


#elif USE_WORK_THREAD

            SendGlobalEvent(new BeginRender());

            CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
            {
                Framebuffer = MainRenderPass.framebuffer_[1-Graphics.RenderContext],
                RenderPass = MainRenderPass.renderPass_
            };

            CommandBuffer cmdBuffer = Graphics.WorkingCmdBuffer;
            cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.OneTimeSubmit | CommandBufferUsages.RenderPassContinue | CommandBufferUsages.SimultaneousUse
                ,inherit
                ));
            MainRenderPass.Draw(cmdBuffer, 0);
            cmdBuffer.End();
            SendGlobalEvent(new RenderEnd());
#endif
            foreach (var view in views_)
            {
                view.Update();
            }
        }

        public void Render()
        {

#if USE_PRIMARY


            // Acquire an index of drawing image for this frame.
            int imageIndex = Graphics.Swapchain.AcquireNextImage(semaphore: Graphics.ImageAvailableSemaphore);

            Graphics.BeginRender();

            if (Graphics.currentFrame_ > 4)
            { 

                // Use a fence to wait until the command buffer has finished execution before using it again
                Graphics.SubmitFences[imageIndex].Wait();
            Graphics.SubmitFences[imageIndex].Reset();

            var subresourceRange = new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1);

            CommandBuffer cmdBuffer = Graphics.PrimaryCmdBuffers[imageIndex];

            // Submit recorded commands to graphics queue for execution.
            Graphics.GraphicsQueue.Submit(
                Graphics.ImageAvailableSemaphore,
                PipelineStages.ColorAttachmentOutput,
                Graphics.PrimaryCmdBuffers[imageIndex],
                Graphics.RenderingFinishedSemaphore,
                Graphics.SubmitFences[imageIndex]
            );
        }

            // Present the color output to screen.
            Graphics.PresentQueue.PresentKhr(Graphics.RenderingFinishedSemaphore, Graphics.Swapchain, imageIndex);

            Graphics.EndRender();

#elif USE_WORK_THREAD

            Graphics.BeginRender();
            // Acquire an index of drawing image for this frame.
            int imageIndex = Graphics.Swapchain.AcquireNextImage(semaphore: Graphics.ImageAvailableSemaphore);

            // Use a fence to wait until the command buffer has finished execution before using it again
            Graphics.SubmitFences[imageIndex].Wait();
            Graphics.SubmitFences[imageIndex].Reset();

            CommandBuffer cmdBuffer = Graphics.PrimaryCmdBuffers[imageIndex];

            cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.SimultaneousUse));
            MainRenderPass.Summit(imageIndex);
            cmdBuffer.End();
            
            // Submit recorded commands to graphics queue for execution.
            Graphics.GraphicsQueue.Submit(
                Graphics.ImageAvailableSemaphore,
                PipelineStages.ColorAttachmentOutput,
                Graphics.PrimaryCmdBuffers[imageIndex],
                Graphics.RenderingFinishedSemaphore,
                Graphics.SubmitFences[imageIndex]
            );

            // Present the color output to screen.
            Graphics.PresentQueue.PresentKhr(Graphics.RenderingFinishedSemaphore, Graphics.Swapchain, imageIndex);
            
            Graphics.EndRender();
#else

            SendGlobalEvent(new BeginRender());

            // Acquire an index of drawing image for this frame.
            int imageIndex = Graphics.Swapchain.AcquireNextImage(semaphore: Graphics.ImageAvailableSemaphore);

            // Use a fence to wait until the command buffer has finished execution before using it again
            Graphics.SubmitFences[imageIndex].Wait();
            Graphics.SubmitFences[imageIndex].Reset();

            var subresourceRange = new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1);

            CommandBuffer cmdBuffer = Graphics.PrimaryCmdBuffers[imageIndex];

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

            MainRenderPass.Begin(cmdBuffer, imageIndex);
            MainRenderPass.Draw(cmdBuffer, imageIndex);
            MainRenderPass.End(cmdBuffer, imageIndex);

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

            // Present the color output to screen.
            Graphics.PresentQueue.PresentKhr(Graphics.RenderingFinishedSemaphore, Graphics.Swapchain, imageIndex);

            SendGlobalEvent(new RenderEnd());
#endif

        }

    }
}
