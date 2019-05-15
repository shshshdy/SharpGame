using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using static Initializers;

    public enum BlendMode
    {
        Replace = 0,
        Add,
        MultiplY,
        Alpha,
        AddAlpha,
        PremulAlpha,
        InvdestAlpha,
        Subtract,
        SubtractAlpha,
    }

    public class Pipeline : DisposeBase
    {
        private VkPipelineRasterizationStateCreateInfo rasterizationState_;
        public VkPipelineRasterizationStateCreateInfo RasterizationState { get => rasterizationState_; set => rasterizationState_ = value; }

        public VkPipelineMultisampleStateCreateInfo MultisampleState { get; set; }

        VkPipelineDepthStencilStateCreateInfo depthStencilState_;
        public VkPipelineDepthStencilStateCreateInfo DepthStencilState { get => depthStencilState_; set => depthStencilState_ = value; }
        public VkPipelineColorBlendStateCreateInfo ColorBlendState { get; set; }
        public VkPrimitiveTopology PrimitiveTopology { get; set; } = VkPrimitiveTopology.TriangleList;

        public VkPipelineVertexInputStateCreateInfo VertexInputState { get; set; }

        VkPipelineViewportStateCreateInfo viewportStateCreateInfo;

        public VkPipelineLayoutCreateInfo PipelineLayoutInfo { get; set; }

        public VkPolygonMode FillMode { get => rasterizationState_.polygonMode; set => rasterizationState_.polygonMode = value; }
        public VkCullModeFlags CullMode { get => rasterizationState_.cullMode; set => rasterizationState_.cullMode = value; }
        public VkFrontFace FrontFace { get => rasterizationState_.frontFace; set => rasterizationState_.frontFace = value; }

        public bool DepthTestEnable { get => depthStencilState_.depthTestEnable; set => depthStencilState_.depthTestEnable = value; }
        public bool DepthWriteEnable { get => depthStencilState_.depthWriteEnable; set => depthStencilState_.depthWriteEnable = value; }
        public BlendMode BlendMode { set => SetBlendMode(value); }

        public VkPipelineDynamicStateCreateInfo? DynamicStateCreateInfo {get; set;}

        public VkPipelineLayout pipelineLayout;

        public VkPipeline pipeline;

      //  Dictionary<(Pass, Geometry), VkPipeline> cachedPipeline_ = new Dictionary<(Pass, Geometry), Vulkan.Pipeline>();

        public Pipeline()
        {
            Init();
        }
        
        public void Init()
        {
            VertexInputState = new VkPipelineVertexInputStateCreateInfo();

            RasterizationState = new VkPipelineRasterizationStateCreateInfo
            {
                polygonMode = VkPolygonMode.Fill,
                cullMode = VkCullModeFlags.Back,
                frontFace = VkFrontFace.CounterClockwise,
                lineWidth = 1.0f
            };

            MultisampleState = new VkPipelineMultisampleStateCreateInfo
            {
                rasterizationSamples = VkSampleCountFlags.Count1,
                minSampleShading = 1.0f
            };

            DepthStencilState = new VkPipelineDepthStencilStateCreateInfo
            {
                depthTestEnable = true,
                depthWriteEnable = true,
                depthCompareOp = VkCompareOp.LessOrEqual,
                back = new VkStencilOpState
                {
                    failOp = VkStencilOp.Keep,
                    passOp = VkStencilOp.Keep,
                    compareOp = VkCompareOp.Always
                },
                front = new VkStencilOpState
                {
                    failOp = VkStencilOp.Keep,
                    passOp = VkStencilOp.Keep,
                    compareOp = VkCompareOp.Always
                }
            };

            ColorBlendState = new VkPipelineColorBlendStateCreateInfo(new[]
            {
                new VkPipelineColorBlendAttachmentState
                {
                    srcColorBlendFactor = VkBlendFactor.One,
                    dstColorBlendFactor = VkBlendFactor.Zero,
                    colorBlendOp = VkBlendOp.Add,
                    srcAlphaBlendFactor = VkBlendFactor.One,
                    dstAlphaBlendFactor = VkBlendFactor.Zero,
                    alphaBlendOp = VkBlendOp.Add,
                    colorWriteMask = (VkColorComponentFlags)0xf
                }
            });

        }

        protected override void Destroy()
        {
            pipeline?.Dispose();
            pipeline = null;

            base.Destroy();
        }

        public void SetBlendMode(BlendMode blendMode)
        {
            switch(blendMode)
            {
                case BlendMode.Replace:
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
                    break;
                case BlendMode.Add:
                    ColorBlendState = new PipelineColorBlendStateCreateInfo(new[]
                    {
                        new PipelineColorBlendAttachmentState
                        {
                            BlendEnable = true,
                            SrcColorBlendFactor = BlendFactor.One,
                            DstColorBlendFactor = BlendFactor.One,
                            ColorBlendOp = BlendOp.Add,
                            SrcAlphaBlendFactor = BlendFactor.SrcAlpha,
                            DstAlphaBlendFactor = BlendFactor.DstAlpha,
                            AlphaBlendOp = BlendOp.Add,
                            ColorWriteMask = ColorComponents.All
                        }
                    });
                    break;
                case BlendMode.MultiplY:
                    break;
                case BlendMode.Alpha:
                    ColorBlendState = new PipelineColorBlendStateCreateInfo(new[]
                    {
                        new PipelineColorBlendAttachmentState
                        {
                            BlendEnable = true,
                            SrcColorBlendFactor = BlendFactor.SrcAlpha,
                            DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                            ColorBlendOp = BlendOp.Add,
                            SrcAlphaBlendFactor = BlendFactor.One,
                            DstAlphaBlendFactor = BlendFactor.Zero,
                            AlphaBlendOp = BlendOp.Add,
                            ColorWriteMask = ColorComponents.All
                        }
                    });
                    break;
                case BlendMode.AddAlpha:
                    break;
                case BlendMode.PremulAlpha:
                    ColorBlendState = new PipelineColorBlendStateCreateInfo(new[]
                    {
                        new PipelineColorBlendAttachmentState
                        {
                            BlendEnable = true,
                            SrcColorBlendFactor = BlendFactor.One,
                            DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                            ColorBlendOp = BlendOp.Add,
                            SrcAlphaBlendFactor = BlendFactor.One,
                            DstAlphaBlendFactor = BlendFactor.Zero,
                            AlphaBlendOp = BlendOp.Add,
                            ColorWriteMask = ColorComponents.All
                        }
                    });
                    break;
                case BlendMode.InvdestAlpha:
                    break;
                case BlendMode.Subtract:
                    break;
                case BlendMode.SubtractAlpha:
                    break;
            }
        }

        protected /*override*/ void Recreate()
        {
          //todo  pipeline?.Dispose();
           // pipeline = null;
        }

        public VkPipeline GetGraphicsPipeline(RenderPass renderPass, Shader shader, Geometry geometry)
        {
            if(pipeline != null)
            {
                return pipeline;
            }

            var graphics = Get<Graphics>();

            var pass = shader.GetPass(renderPass.passID);
            if(pass == null)
            {
                return 0;
            }

            var pipelineLayoutInfo = new VkPipelineLayoutCreateInfo(new[]
            { pass.ResourceLayout.descriptorSetLayout });

            pipelineLayout = Graphics.Device.CreatePipelineLayout(pipelineLayoutInfo);
            var shaderStageCreateInfos = pass.GetShaderStageCreateInfos();

            viewportStateCreateInfo = pipelineViewportStateCreateInfo(
            viewport(0, 0, graphics.Width, graphics.Height),
            rect2D(0, 0, graphics.Width, graphics.Height));

            var inputAssemblyStateCreateInfo = pipelineInputAssemblyStateCreateInfo(
                geometry ? geometry.PrimitiveTopology : PrimitiveTopology);

            var pipelineCreateInfo = GraphicsPipelineCreateInfo(
                pipelineLayout, renderPass.renderPass_, 0,
                shaderStageCreateInfos,
                inputAssemblyStateCreateInfo,
                geometry == null ? VertexInputState : geometry.VertexLayout,
                RasterizationState,
                viewportState: viewportStateCreateInfo,
                multisampleState: MultisampleState,
                depthStencilState: DepthStencilState,
                colorBlendState: ColorBlendState,
                dynamicState : DynamicStateCreateInfo);

            pipeline = Graphics.Device.CreateGraphicsPipeline(pipelineCreateInfo);
            Graphics.ToDisposeFrame(pipeline);
            return pipeline;
        }

        public VkPipeline GetComputePipeline(Pass shaderPass)
        {
            if(!shaderPass.IsComputeShader)
            {
                return 0;
            }

            var pipelineLayoutInfo = new VkPipelineLayoutCreateInfo(new[]
            { shaderPass.ResourceLayout.descriptorSetLayout });
            pipelineLayout = Graphics.Device.CreatePipelineLayout(pipelineLayoutInfo);

            var pipelineCreateInfo = new ComputePipelineCreateInfo(
                shaderPass.GetComputeStageCreateInfo(), pipelineLayout);

            pipeline = Graphics.Device.CreateComputePipeline(pipelineCreateInfo);
            Graphics.ToDisposeFrame(pipeline);
            return pipeline;
        }
        
    }
}
