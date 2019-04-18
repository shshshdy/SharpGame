using System.Threading.Tasks;
using VulkanCore;

namespace SharpGame.Samples.ColoredTriangle
{
    public class ColoredTriangleApp : Application
    {
        private PipelineLayout _pipelineLayout;
        private Pipeline _pipeline;

        protected override void InitializePermanent()
        {
            _pipelineLayout = ToDispose(CreatePipelineLayout());
        }

        protected override void InitializeFrame()
        {
            _pipeline     = Renderer.CreateGraphicsPipeline(_pipelineLayout);
        }

        private PipelineLayout CreatePipelineLayout()
        {
            var layoutCreateInfo = new PipelineLayoutCreateInfo();
            return Graphics.Device.CreatePipelineLayout(layoutCreateInfo);
        }
        
        protected override void RecordCommandBuffer(CommandBuffer cmdBuffer, int imageIndex)
        {
            var renderPassBeginInfo = new RenderPassBeginInfo(
                Renderer._framebuffers[imageIndex],
                new Rect2D(Offset2D.Zero, new Extent2D(Platform.Width, Platform.Height)),
                new ClearColorValue(new ColorF4(0.39f, 0.58f, 0.93f, 1.0f)));

            cmdBuffer.CmdBeginRenderPass(renderPassBeginInfo);
            cmdBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, _pipeline.pipeline);
            cmdBuffer.CmdDraw(3);
            cmdBuffer.CmdEndRenderPass();
        }
    }
}
