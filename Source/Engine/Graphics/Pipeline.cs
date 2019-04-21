using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class Pipeline : GPUObject
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
        public VulkanCore.Pipeline pipeline;

        public Pipeline()
        {
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
            pipeline?.Dispose();
            pipeline = null;

            base.Dispose();
        }

        protected override void Recreate()
        {
            pipeline?.Dispose();
            pipeline = null;
        }

        public VulkanCore.Pipeline GetGraphicsPipeline(RenderPass renderPass, Shader shader, Geometry geometry)
        {
            if(pipeline != null)
            {
                return pipeline;
            }
            var graphics = Get<Graphics>();

            pipelineLayout = graphics.Device.CreatePipelineLayout(PipelineLayoutInfo);
            var shaderStageCreateInfos = shader.GetShaderStageCreateInfos();

            viewportStateCreateInfo = new PipelineViewportStateCreateInfo(
            new Viewport(0, 0, graphics.Width, graphics.Height),
            new Rect2D(0, 0, graphics.Width, graphics.Height));

            var inputAssemblyStateCreateInfo = new PipelineInputAssemblyStateCreateInfo(PrimitiveTopology);

            var pipelineCreateInfo = new GraphicsPipelineCreateInfo(
                pipelineLayout, renderPass.renderPass_, 0,
                shaderStageCreateInfos,
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

        public VulkanCore.Pipeline GetComputePipeline(RenderPass renderPass, ComputeShader shader)
        {
            var graphics = Get<Graphics>();

            pipelineLayout = graphics.Device.CreatePipelineLayout(PipelineLayoutInfo);

            var pipelineCreateInfo = new ComputePipelineCreateInfo(
                shader.GetShaderStageCreateInfo(), pipelineLayout);

            pipeline = graphics.Device.CreateComputePipeline(pipelineCreateInfo);
            graphics.ToDisposeFrame(pipeline);
            return pipeline;
        }
        
    }
}
