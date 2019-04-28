using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class Pipeline : GPUObject
    {
        public PipelineRasterizationStateCreateInfo RasterizationState { get; set; }
        public PipelineMultisampleStateCreateInfo MultisampleState { get; set; } = new PipelineMultisampleStateCreateInfo
        {
            RasterizationSamples = SampleCounts.Count1,
            MinSampleShading = 1.0f
        };

        public PipelineColorBlendStateCreateInfo ColorBlendState { get; set; }
        public PipelineDepthStencilStateCreateInfo DepthStencilState { get; set; }

        public PrimitiveTopology PrimitiveTopology { get; set; } = PrimitiveTopology.TriangleList;

        public PipelineVertexInputStateCreateInfo VertexInputState { get; set; }

        PipelineViewportStateCreateInfo viewportStateCreateInfo;

        public PipelineLayoutCreateInfo PipelineLayoutInfo { get; set; }

        public PipelineLayout pipelineLayout;
        public VulkanCore.Pipeline pipeline;

        public Pipeline()
        {
        }
        
        public void SetDefault()
        {
            VertexInputState = new PipelineVertexInputStateCreateInfo();

            RasterizationState = new PipelineRasterizationStateCreateInfo
            {
                PolygonMode = PolygonMode.Fill,
                CullMode = CullModes.Back,
                FrontFace = FrontFace.CounterClockwise,
                LineWidth = 1.0f
            };

            MultisampleState = new PipelineMultisampleStateCreateInfo
            {
                RasterizationSamples = SampleCounts.Count1,
                MinSampleShading = 1.0f
            };

            DepthStencilState = new PipelineDepthStencilStateCreateInfo
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

            ColorBlendState = new PipelineColorBlendStateCreateInfo(new[]
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

            var pass = shader.GetPass(renderPass.Name);
            if(pass == null)
            {
                return null;
            }

            pipelineLayout = graphics.Device.CreatePipelineLayout(PipelineLayoutInfo);
            var shaderStageCreateInfos = pass.GetShaderStageCreateInfos();

            viewportStateCreateInfo = new PipelineViewportStateCreateInfo(
            new VulkanCore.Viewport(0, 0, graphics.Width, graphics.Height),
            new Rect2D(0, 0, graphics.Width, graphics.Height));

            var inputAssemblyStateCreateInfo = new PipelineInputAssemblyStateCreateInfo(
                geometry ? geometry.PrimitiveTopology : PrimitiveTopology);

            var pipelineCreateInfo = new GraphicsPipelineCreateInfo(
                pipelineLayout, renderPass.renderPass_, 0,
                shaderStageCreateInfos,
                inputAssemblyStateCreateInfo,
                geometry ? geometry.VertexInputState : VertexInputState,
                RasterizationState,
                viewportState: viewportStateCreateInfo,
                multisampleState: MultisampleState,
                depthStencilState: DepthStencilState,
                colorBlendState: ColorBlendState);

            pipeline = graphics.Device.CreateGraphicsPipeline(pipelineCreateInfo);
            graphics.ToDisposeFrame(pipeline);
            return pipeline;
        }

        public VulkanCore.Pipeline GetComputePipeline(RenderPass renderPass, Pass shader)
        {
            if(!shader.IsComputeShader)
            {
                return null;
            }

            var graphics = Get<Graphics>();

            pipelineLayout = graphics.Device.CreatePipelineLayout(PipelineLayoutInfo);

            var pipelineCreateInfo = new ComputePipelineCreateInfo(
                shader.GetComputeStageCreateInfo(), pipelineLayout);

            pipeline = graphics.Device.CreateComputePipeline(pipelineCreateInfo);
            graphics.ToDisposeFrame(pipeline);
            return pipeline;
        }
        
    }
}
