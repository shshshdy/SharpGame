using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class Pipeline : DeviceObject
    {
        public PipelineRasterizationStateCreateInfo RasterizationStateCreateInfo { get; set; }
        public PipelineMultisampleStateCreateInfo MultisampleStateCreateInfo { get; set; } = new PipelineMultisampleStateCreateInfo
        {
            RasterizationSamples = SampleCounts.Count1,
            MinSampleShading = 1.0f
        };

        public PipelineColorBlendStateCreateInfo ColorBlendStateCreateInfo { get; set; }
        public PipelineDepthStencilStateCreateInfo DepthStencilStateCreateInfo { get; set; }

        public PrimitiveTopology PrimitiveTopology { get; set; } = PrimitiveTopology.TriangleList;

        public PipelineVertexInputStateCreateInfo VertexInputStateCreateInfo { get; set; }
        PipelineViewportStateCreateInfo viewportStateCreateInfo;

        public PipelineLayoutCreateInfo PipelineLayoutInfo { get; set; }

        public PipelineLayout pipelineLayout;

        public Shader Shader { get; set; }

        ComputeShader ComputeShader;

        public VulkanCore.Pipeline pipeline;
        public Pipeline()
        {
        }

        public Pipeline(Shader shader)
        {
            Shader = shader;
        }

        public void SetDefault()
        {
            VertexInputStateCreateInfo = new PipelineVertexInputStateCreateInfo();

            RasterizationStateCreateInfo = new PipelineRasterizationStateCreateInfo
            {
                PolygonMode = PolygonMode.Fill,
                CullMode = CullModes.Back,
                FrontFace = FrontFace.CounterClockwise,
                LineWidth = 1.0f
            };

            MultisampleStateCreateInfo = new PipelineMultisampleStateCreateInfo
            {
                RasterizationSamples = SampleCounts.Count1,
                MinSampleShading = 1.0f
            };

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
            };

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
            });

        }

        public override void Dispose()
        {
            pipeline.Dispose();

            base.Dispose();
        }

        public VulkanCore.Pipeline GetGraphicsPipeline(RenderPass renderPass)
        {
            if(pipeline != null)
            {
                return pipeline;
            }

            var graphics = Get<Graphics>();

            pipelineLayout = graphics.Device.CreatePipelineLayout(PipelineLayoutInfo);

            viewportStateCreateInfo = new PipelineViewportStateCreateInfo(
            new Viewport(0, 0, graphics.Width, graphics.Height),
            new Rect2D(0, 0, graphics.Width, graphics.Height));

            var inputAssemblyStateCreateInfo = new PipelineInputAssemblyStateCreateInfo(PrimitiveTopology);

            var pipelineCreateInfo = new GraphicsPipelineCreateInfo(
                pipelineLayout, renderPass, 0,
                Shader.GetShaderStageCreateInfos(),
                inputAssemblyStateCreateInfo,
                VertexInputStateCreateInfo,
                RasterizationStateCreateInfo,
                viewportState: viewportStateCreateInfo,
                multisampleState: MultisampleStateCreateInfo,
                depthStencilState: DepthStencilStateCreateInfo,
                colorBlendState: ColorBlendStateCreateInfo);

            pipeline = graphics.Device.CreateGraphicsPipeline(pipelineCreateInfo);
            graphics.ToDisposeFrame(pipeline);
            return pipeline;
        }

        public VulkanCore.Pipeline GetComputePipeline(View view, RenderPass renderPass)
        {
            return null;
        }

        protected override void Recreate()
        {
        }

    }
}
