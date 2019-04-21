using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

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

        public void RecordCommandBuffer()
        {
            var subresourceRange = new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1);
            for (int i = 0; i < Graphics.CommandBuffers.Length; i++)
            {
                CommandBuffer cmdBuffer = Graphics.CommandBuffers[i];
                cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.SimultaneousUse));

                if (Graphics.PresentQueue != Graphics.GraphicsQueue)
                {
                    var barrierFromPresentToDraw = new ImageMemoryBarrier(
                        Graphics.SwapchainImages[i], subresourceRange,
                        Accesses.MemoryRead, Accesses.ColorAttachmentWrite,
                        ImageLayout.Undefined, ImageLayout.PresentSrcKhr,
                        Graphics.PresentQueue.FamilyIndex, Graphics.GraphicsQueue.FamilyIndex);

                    cmdBuffer.CmdPipelineBarrier(
                        PipelineStages.ColorAttachmentOutput,
                        PipelineStages.ColorAttachmentOutput,
                        imageMemoryBarriers: new[] { barrierFromPresentToDraw });
                }

                MainRenderPass.Begin(cmdBuffer, i);

                MainRenderPass.End(cmdBuffer);

                if (Graphics.PresentQueue != Graphics.GraphicsQueue)
                {
                    var barrierFromDrawToPresent = new ImageMemoryBarrier(
                        Graphics.SwapchainImages[i], subresourceRange,
                        Accesses.ColorAttachmentWrite, Accesses.MemoryRead,
                        ImageLayout.PresentSrcKhr, ImageLayout.PresentSrcKhr,
                        Graphics.GraphicsQueue.FamilyIndex, Graphics.PresentQueue.FamilyIndex);

                    cmdBuffer.CmdPipelineBarrier(
                        PipelineStages.ColorAttachmentOutput,
                        PipelineStages.BottomOfPipe,
                        imageMemoryBarriers: new[] { barrierFromDrawToPresent });
                }

                cmdBuffer.End();
            }


        }

        public View CreateViewport()
        {
            var view = new View();
            views_.Add(view);
            return view;
        }

        public void Update()
        {
            foreach(var view in views_)
            {
                view.Update();
            }
        }

        public void Render()
        {           
            Graphics.Draw();
        }

    }
}
