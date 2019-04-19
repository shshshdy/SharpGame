using System.Threading.Tasks;
using VulkanCore;

namespace SharpGame.Samples.ColoredTriangle
{
    public class ColoredTriangleApp : Application
    {
        private PipelineLayout _pipelineLayout;
        private Pipeline _pipeline;
        Shader _shader;
        protected override void InitializePermanent()
        {
            _pipelineLayout = ToDispose(CreatePipelineLayout());

            _shader = new Shader
            {
                ShaderStageInfo = new[]
                {
                    new ShaderStageInfo
                    {
                        Stage = ShaderStages.Vertex,
                        FileName = "Shader.vert.spv",
                        FuncName = "main"
                    },

                    new ShaderStageInfo
                    {
                        Stage = ShaderStages.Fragment,
                        FileName = "Shader.frag.spv",
                        FuncName = "main"
                    }
                }
            };
            _shader.Load();
        }

        protected override void InitializeFrame()
        {
           // _pipeline     = Renderer.CreateGraphicsPipeline(_pipelineLayout);
            _pipeline = new Pipeline(_shader)
            {

            };

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
                new ClearColorValue(new ColorF4(0.39f, 0.58f, 0.93f, 1.0f)),
                new ClearDepthStencilValue(1.0f, 0));

            cmdBuffer.CmdBeginRenderPass(renderPassBeginInfo);
            var pipeline = _pipeline.GetGraphicsPipeline(Renderer.MainRenderPass);
            cmdBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);
            cmdBuffer.CmdDraw(3);
            cmdBuffer.CmdEndRenderPass();
        }
    }
}
