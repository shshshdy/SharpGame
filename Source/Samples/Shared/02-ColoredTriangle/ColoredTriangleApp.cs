using System.Threading.Tasks;
using VulkanCore;

namespace SharpGame.Samples.ColoredTriangle
{
    public class ColoredTriangleApp : Application
    {
        private PipelineLayout _pipelineLayout;
        private Pipeline _pipeline;
        private Shader _shader;
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
                        FileName = "Test.vert.spv",
                        FuncName = "main"
                    },

                    new ShaderStageInfo
                    {
                        Stage = ShaderStages.Fragment,
                        FileName = "Test.frag.spv",
                        FuncName = "main"
                    }
                }
            };
            _shader.Load();
        }

        protected override void InitializeFrame()
        {
            _pipeline = new Pipeline(_shader)
            {

                RasterizationStateCreateInfo = new PipelineRasterizationStateCreateInfo
                {
                    PolygonMode = PolygonMode.Fill,
                    CullMode = CullModes.Back,
                    FrontFace = FrontFace.CounterClockwise,
                    LineWidth = 1.0f
                },

                MultisampleStateCreateInfo = new PipelineMultisampleStateCreateInfo
                {
                    RasterizationSamples = SampleCounts.Count1,
                    MinSampleShading = 1.0f
                },

                DepthStencilStateCreateInfo = new PipelineDepthStencilStateCreateInfo
                {
                    DepthTestEnable = true,
                    DepthWriteEnable = true,
                    DepthCompareOp = CompareOp.LessOrEqual,
                    Back = new StencilOpState
                    {
                        FailOp = StencilOp.Keep,
                        PassOp = StencilOp.Keep,
                        CompareOp = CompareOp.Always
                    },
                    Front = new StencilOpState
                    {
                        FailOp = StencilOp.Keep,
                        PassOp = StencilOp.Keep,
                        CompareOp = CompareOp.Always
                    }
                },

                ColorBlendStateCreateInfo = new PipelineColorBlendStateCreateInfo(new[]
                {
                    new PipelineColorBlendAttachmentState
                    {
                        SrcColorBlendFactor = BlendFactor.One,
                        DstColorBlendFactor = BlendFactor.Zero,
                        ColorBlendOp = BlendOp.Add,
                        SrcAlphaBlendFactor = BlendFactor.One,
                        DstAlphaBlendFactor = BlendFactor.Zero,
                        AlphaBlendOp = BlendOp.Add,
                        ColorWriteMask = ColorComponents.All
                    }
                })
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
