using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class Pipeline : Object, IDeviceObject
    {
        public VulkanCore.Pipeline pipeline;
        public override void Dispose()
        {

        }

        public void Recreate()
        {
        }

        public static Pipeline Create(PipelineLayout pipelineLayout, RenderPass renderPass)
        {
            var resourceCache = Get<ResourceCache>();
            var graphics = Get<Graphics>();

            ShaderModule vertexShader = resourceCache.Load<ShaderModule>("Shader.vert.spv");
            ShaderModule fragmentShader = resourceCache.Load<ShaderModule>("Shader.frag.spv");
            var shaderStageCreateInfos = new[]
            {
                new PipelineShaderStageCreateInfo(ShaderStages.Vertex, vertexShader, "main"),
                new PipelineShaderStageCreateInfo(ShaderStages.Fragment, fragmentShader, "main")
            };

            var vertexInputStateCreateInfo = new PipelineVertexInputStateCreateInfo();
            var inputAssemblyStateCreateInfo = new PipelineInputAssemblyStateCreateInfo(PrimitiveTopology.TriangleList);
            var viewportStateCreateInfo = new PipelineViewportStateCreateInfo(
                new Viewport(0, 0, graphics.Width, graphics.Height),
                new Rect2D(0, 0, graphics.Width, graphics.Height));
            var rasterizationStateCreateInfo = new PipelineRasterizationStateCreateInfo
            {
                PolygonMode = PolygonMode.Fill,
                CullMode = CullModes.Back,
                FrontFace = FrontFace.CounterClockwise,
                LineWidth = 1.0f
            };
            var multisampleStateCreateInfo = new PipelineMultisampleStateCreateInfo
            {
                RasterizationSamples = SampleCounts.Count1,
                MinSampleShading = 1.0f
            };
            var colorBlendAttachmentState = new PipelineColorBlendAttachmentState
            {
                SrcColorBlendFactor = BlendFactor.One,
                DstColorBlendFactor = BlendFactor.Zero,
                ColorBlendOp = BlendOp.Add,
                SrcAlphaBlendFactor = BlendFactor.One,
                DstAlphaBlendFactor = BlendFactor.Zero,
                AlphaBlendOp = BlendOp.Add,
                ColorWriteMask = ColorComponents.All
            };
            var colorBlendStateCreateInfo = new PipelineColorBlendStateCreateInfo(
                new[] { colorBlendAttachmentState });

            var pipelineCreateInfo = new GraphicsPipelineCreateInfo(
                pipelineLayout, renderPass, 0,
                shaderStageCreateInfos,
                inputAssemblyStateCreateInfo,
                vertexInputStateCreateInfo,
                rasterizationStateCreateInfo,
                viewportState: viewportStateCreateInfo,
                multisampleState: multisampleStateCreateInfo,
                colorBlendState: colorBlendStateCreateInfo);

            var pipeline = new Pipeline { pipeline = graphics.Device.CreateGraphicsPipeline(pipelineCreateInfo) };

            return pipeline;
        }
    }
}
