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
                Graphics.SwapchainImages[imageIndex], imageSubresourceRange,
                Accesses.None, Accesses.TransferWrite,
                ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
            var barrierFromClearToPresent = new ImageMemoryBarrier(
                Graphics.SwapchainImages[imageIndex], imageSubresourceRange,
                Accesses.TransferWrite, Accesses.MemoryRead,
                ImageLayout.TransferDstOptimal, ImageLayout.PresentSrcKhr);

            cmdBuffer.CmdPipelineBarrier(
                PipelineStages.Transfer, PipelineStages.Transfer,
                imageMemoryBarriers: new[] { barrierFromPresentToClear });
            cmdBuffer.CmdClearColorImage(
                Graphics.SwapchainImages[imageIndex], 
                ImageLayout.TransferDstOptimal,
                new ClearColorValue(new ColorF4(0.39f, 0.58f, 0.93f, 1.0f)),
                imageSubresourceRange);
            cmdBuffer.CmdPipelineBarrier(
                PipelineStages.Transfer, PipelineStages.Transfer,
                imageMemoryBarriers: new[] { barrierFromClearToPresent });
        }
  
    }
}
