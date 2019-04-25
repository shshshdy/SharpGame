#define USE_WORK_THREAD
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

        int lastIndex = -1;
        public void RenderUpdate()
        {
#if USE_WORK_THREAD

            int index = Graphics.WorkContext;// 1 - imageIndex;
            while (lastIndex == index)
            {
                System.Threading.Thread.Sleep(0);
               // index = 1 - imageIndex;
            }

            lastIndex = index;

            SendGlobalEvent(new BeginRender());

            CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
            {
                Framebuffer = MainRenderPass.framebuffer_[index],
                RenderPass = MainRenderPass.renderPass_
            };

            CommandBuffer cmdBuffer = Graphics.SecondaryCmdBuffers[index].Get();
            cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.OneTimeSubmit | CommandBufferUsages.RenderPassContinue | CommandBufferUsages.SimultaneousUse
                ,inherit
                ));
            MainRenderPass.Draw(cmdBuffer, index);
            cmdBuffer.End();
            SendGlobalEvent(new RenderEnd());
#endif
            foreach (var view in views_)
            {
                view.Update();
            }
        }

        int imageIndex;
        public void Render()
        {
#if USE_WORK_THREAD
            // Acquire an index of drawing image for this frame.
            imageIndex = Graphics.Swapchain.AcquireNextImage(semaphore: Graphics.ImageAvailableSemaphore);

            Graphics.BeginRender();
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


            Graphics.EndRender();
            // Present the color output to screen.
            Graphics.PresentQueue.PresentKhr(Graphics.RenderingFinishedSemaphore, Graphics.Swapchain, imageIndex);

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
            cmdBuffer.CmdEndRenderPass();

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
