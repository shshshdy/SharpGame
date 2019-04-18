using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class Pipeline : DeviceObject
    {
        public PipelineRasterizationStateCreateInfo RasterizationStateCreateInfo { get; set; }
        public PipelineMultisampleStateCreateInfo MultisampleStateCreateInfo { get; set; }
        public PipelineColorBlendStateCreateInfo ColorBlendStateCreateInfo { get; set; }
        public PipelineDepthStencilStateCreateInfo DepthStencilStateCreateInfo { get; set; }

        Shader Shader;
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

        public VulkanCore.Pipeline GetGraphicsPipeline(View view, RenderPass renderPass)
        {
            var graphics = Get<Graphics>();
//             var pipelineCreateInfo = new GraphicsPipelineCreateInfo(
//                 pipelineLayout, renderPass, 0,
//                 Shader.GetShaderStageCreateInfos(),
//                 inputAssemblyStateCreateInfo,
//                 vertexInputStateCreateInfo,
//                 Shader.RasterizationStateCreateInfo,
//                 viewportState: viewportStateCreateInfo,
//                 multisampleState: Shader.MultisampleStateCreateInfo,
//                 colorBlendState: Shader.ColorBlendStateCreateInfo);
// 
//             pipeline = graphics.Device.CreateGraphicsPipeline(pipelineCreateInfo);

            return null;
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
