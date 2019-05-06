using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
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

    public class Pipeline : GPUObject
    {
        private PipelineRasterizationStateCreateInfo rasterizationState_;
        public PipelineRasterizationStateCreateInfo RasterizationState { get => rasterizationState_; set => rasterizationState_ = value; }

        public PipelineMultisampleStateCreateInfo MultisampleState { get; set; }
        PipelineDepthStencilStateCreateInfo depthStencilState_;
        public PipelineDepthStencilStateCreateInfo DepthStencilState { get => depthStencilState_; set => depthStencilState_ = value; }
        public PipelineColorBlendStateCreateInfo ColorBlendState { get; set; }
        public PrimitiveTopology PrimitiveTopology { get; set; } = PrimitiveTopology.TriangleList;

        public PipelineVertexInputStateCreateInfo VertexInputState { get; set; }

        PipelineViewportStateCreateInfo viewportStateCreateInfo;

        public PipelineLayoutCreateInfo PipelineLayoutInfo { get; set; }

        public PolygonMode FillMode { get => rasterizationState_.PolygonMode; set => rasterizationState_.PolygonMode = value; }
        public CullModes CullMode { get => rasterizationState_.CullMode; set => rasterizationState_.CullMode = value; }
        public FrontFace FrontFace { get => rasterizationState_.FrontFace; set => rasterizationState_.FrontFace = value; }

        public bool DepthTestEnable { get => depthStencilState_.DepthTestEnable; set => depthStencilState_.DepthTestEnable = value; }
        public bool DepthWriteEnable { get => depthStencilState_.DepthWriteEnable; set => depthStencilState_.DepthWriteEnable = value; }
        public BlendMode BlendMode { get; set; }

        public PipelineLayout pipelineLayout;

        public VulkanCore.Pipeline pipeline;

        Dictionary<Pass, VulkanCore.Pipeline> cachedPipeline_ = new Dictionary<Pass, VulkanCore.Pipeline>();

        public Pipeline()
        {
            Init();
        }
        
        public void Init()
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
                            SrcColorBlendFactor = BlendFactor.One,
                            DstColorBlendFactor = BlendFactor.One,
                            ColorBlendOp = BlendOp.Add,
                            SrcAlphaBlendFactor = BlendFactor.One,
                            DstAlphaBlendFactor = BlendFactor.Zero,
                            AlphaBlendOp = BlendOp.Add,
                            ColorWriteMask = ColorComponents.All
                        }
                    }, true);
                    break;
                case BlendMode.MultiplY:
                    break;
                case BlendMode.Alpha:
                    ColorBlendState = new PipelineColorBlendStateCreateInfo(new[]
                    {
                        new PipelineColorBlendAttachmentState
                        {
                            SrcColorBlendFactor = BlendFactor.SrcAlpha,
                            DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                            ColorBlendOp = BlendOp.Add,
                            SrcAlphaBlendFactor = BlendFactor.One,
                            DstAlphaBlendFactor = BlendFactor.Zero,
                            AlphaBlendOp = BlendOp.Add,
                            ColorWriteMask = ColorComponents.All
                        }
                    }, true);
                    break;
                case BlendMode.AddAlpha:
                    break;
                case BlendMode.PremulAlpha:
                    ColorBlendState = new PipelineColorBlendStateCreateInfo(new[]
                    {
                        new PipelineColorBlendAttachmentState
                        {
                            SrcColorBlendFactor = BlendFactor.One,
                            DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                            ColorBlendOp = BlendOp.Add,
                            SrcAlphaBlendFactor = BlendFactor.One,
                            DstAlphaBlendFactor = BlendFactor.Zero,
                            AlphaBlendOp = BlendOp.Add,
                            ColorWriteMask = ColorComponents.All
                        }
                    }, true);
                    break;
                case BlendMode.InvdestAlpha:
                    break;
                case BlendMode.Subtract:
                    break;
                case BlendMode.SubtractAlpha:
                    break;
            }
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
                geometry == null ? VertexInputState : geometry.VertexInputState,
                RasterizationState,
                viewportState: viewportStateCreateInfo,
                multisampleState: MultisampleState,
                depthStencilState: DepthStencilState,
                colorBlendState: ColorBlendState);

            pipeline = graphics.Device.CreateGraphicsPipeline(pipelineCreateInfo);
            Graphics.ToDisposeFrame(pipeline);
            return pipeline;
        }

        public VulkanCore.Pipeline GetComputePipeline(Pass shader)
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
            Graphics.ToDisposeFrame(pipeline);
            return pipeline;
        }
        
    }
}
