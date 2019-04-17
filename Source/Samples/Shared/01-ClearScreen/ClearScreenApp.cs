using VulkanCore;
using VulkanCore.Khr;

namespace SharpGame.Samples.ClearScreen
{
    public class ClearScreenApp : Application
    {
        protected override void RecordCommandBuffer(CommandBuffer cmdBuffer, int imageIndex)
        {
            var imageSubresourceRange = new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1);

            var barrierFromPresentToClear = new ImageMemoryBarrier(
                Context.SwapchainImages[imageIndex], imageSubresourceRange,
                Accesses.None, Accesses.TransferWrite,
                ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
            var barrierFromClearToPresent = new ImageMemoryBarrier(
                Context.SwapchainImages[imageIndex], imageSubresourceRange,
                Accesses.TransferWrite, Accesses.MemoryRead,
                ImageLayout.TransferDstOptimal, ImageLayout.PresentSrcKhr);

            cmdBuffer.CmdPipelineBarrier(
                PipelineStages.Transfer, PipelineStages.Transfer,
                imageMemoryBarriers: new[] { barrierFromPresentToClear });
            cmdBuffer.CmdClearColorImage(
                Context.SwapchainImages[imageIndex], 
                ImageLayout.TransferDstOptimal,
                new ClearColorValue(new ColorF4(0.39f, 0.58f, 0.93f, 1.0f)),
                imageSubresourceRange);
            cmdBuffer.CmdPipelineBarrier(
                PipelineStages.Transfer, PipelineStages.Transfer,
                imageMemoryBarriers: new[] { barrierFromClearToPresent });
        }
        /*
        protected override void Draw(Timer timer)
        {
            // Acquire an index of drawing image for this frame.
            int imageIndex = Context.Swapchain.AcquireNextImage(semaphore: Context.ImageAvailableSemaphore);

            // Use a fence to wait until the command buffer has finished execution before using it again
            Context.SubmitFences[imageIndex].Wait();
            Context.SubmitFences[imageIndex].Reset();

            // Submit recorded commands to graphics queue for execution.
            Context.GraphicsQueue.Submit(
                Context.ImageAvailableSemaphore,
                PipelineStages.Transfer,
                Context.CommandBuffers[imageIndex],
                Context.RenderingFinishedSemaphore,
                Context.SubmitFences[imageIndex]
            );

            // Present the color output to screen.
            Context.PresentQueue.PresentKhr(Context.RenderingFinishedSemaphore, Context.Swapchain, imageIndex);
        }*/
    }
}
