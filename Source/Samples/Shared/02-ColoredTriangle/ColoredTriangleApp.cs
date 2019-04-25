using System.Threading.Tasks;
using VulkanCore;

namespace SharpGame.Samples.ColoredTriangle
{
    public class ColoredTriangleApp : Application
    {
        private Pipeline pipeline_;
        private Shader shader_;

        protected override void OnInit()
        {
            SubscribeToEvent<BeginRenderPass>(Handle);

            shader_ = new Shader
            {
                VertexShader = new ShaderModule(ShaderStages.Vertex, "Test.vert.spv"),
                PixelShader = new ShaderModule(ShaderStages.Fragment, "Test.frag.spv"),
            };

            shader_.Build();

            pipeline_ = new Pipeline
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

        public override void Dispose()
        {
            shader_.Dispose();
            pipeline_.Dispose();

            base.Dispose();
        }


        void Handle(BeginRenderPass e)
        {
            var cmdBuffer = e.commandBuffer;

            var pipeline = pipeline_.GetGraphicsPipeline(e.renderPass, shader_, null);
            cmdBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);
            cmdBuffer.CmdDraw(3);
        }
    }
}
