using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using System.Runtime.CompilerServices;
    using static Builder;
    using static VulkanNative;

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

    public class Pipeline : Resource
    {
        private VkPipelineRasterizationStateCreateInfo rasterizationState_;
        public ref VkPipelineRasterizationStateCreateInfo RasterizationState => ref rasterizationState_;

        private VkPipelineMultisampleStateCreateInfo multisampleState;
        public ref VkPipelineMultisampleStateCreateInfo MultisampleState => ref multisampleState;

        private VkPipelineDepthStencilStateCreateInfo depthStencilState_;
        public ref VkPipelineDepthStencilStateCreateInfo DepthStencilState => ref depthStencilState_;

        private VkPipelineColorBlendStateCreateInfo colorBlendState;
        public ref VkPipelineColorBlendStateCreateInfo ColorBlendState => ref colorBlendState;
        public PrimitiveTopology PrimitiveTopology { get; set; } = PrimitiveTopology.TriangleList;

        private VertexLayout vertexlayout;
        public ref VertexLayout Vertexlayout => ref vertexlayout;

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

        public Pipeline()
        {
            Init();
        }
        
        public void Init()
        {
            vertexlayout = new VertexLayout();

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

            BlendMode = BlendMode.Replace;

        }

        protected override void Destroy()
        {
            vkDestroyPipeline(Graphics.device, pipeline, IntPtr.Zero);
            pipeline = 0;
            base.Destroy();
        }

        public unsafe void SetBlendMode(BlendMode blendMode)
        {
            VkPipelineColorBlendAttachmentState colorBlendAttachmentState;
            switch (blendMode)
            {
                case BlendMode.Replace:

                    colorBlendAttachmentState = new VkPipelineColorBlendAttachmentState
                    {
                        srcColorBlendFactor = VkBlendFactor.One,
                        dstColorBlendFactor = VkBlendFactor.Zero,
                        colorBlendOp = VkBlendOp.Add,
                        srcAlphaBlendFactor = VkBlendFactor.One,
                        dstAlphaBlendFactor = VkBlendFactor.Zero,
                        alphaBlendOp = VkBlendOp.Add,
                        colorWriteMask = (VkColorComponentFlags)0xf
                    };

                    ColorBlendState = ColorBlendStateCreateInfo(1, ref colorBlendAttachmentState);
                    break;
                case BlendMode.Add:

                    colorBlendAttachmentState = new VkPipelineColorBlendAttachmentState
                    {
                        srcColorBlendFactor = VkBlendFactor.One,
                        dstColorBlendFactor = VkBlendFactor.One,
                        colorBlendOp = VkBlendOp.Add,
                        srcAlphaBlendFactor = VkBlendFactor.SrcAlpha,
                        dstAlphaBlendFactor = VkBlendFactor.DstAlpha,
                        alphaBlendOp = VkBlendOp.Add,
                        colorWriteMask = (VkColorComponentFlags)0xf
                    };

                    ColorBlendState = ColorBlendStateCreateInfo(1, ref colorBlendAttachmentState);

                    break;
                case BlendMode.MultiplY:
                    break;
                case BlendMode.Alpha:
                    colorBlendAttachmentState = new VkPipelineColorBlendAttachmentState
                    {
                        blendEnable = true,
                        srcColorBlendFactor = VkBlendFactor.SrcAlpha,
                        dstColorBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                        colorBlendOp = VkBlendOp.Add,
                        srcAlphaBlendFactor = VkBlendFactor.SrcAlpha,
                        dstAlphaBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                        alphaBlendOp = VkBlendOp.Add,
                        colorWriteMask = (VkColorComponentFlags)0xf
                    };

                    ColorBlendState = ColorBlendStateCreateInfo(1, ref colorBlendAttachmentState);
                    break;
                case BlendMode.AddAlpha:
                    break;
                case BlendMode.PremulAlpha:
                    colorBlendAttachmentState = new VkPipelineColorBlendAttachmentState
                    {
                        blendEnable = true,
                        srcColorBlendFactor = VkBlendFactor.One,
                        dstColorBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                        colorBlendOp = VkBlendOp.Add,
                        srcAlphaBlendFactor = VkBlendFactor.One,
                        dstAlphaBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                        alphaBlendOp = VkBlendOp.Add,
                        colorWriteMask = (VkColorComponentFlags)0xf
                    };

                    ColorBlendState = ColorBlendStateCreateInfo(1, ref colorBlendAttachmentState);
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
            vkDestroyPipeline(Graphics.device, pipeline, IntPtr.Zero);
            pipeline = 0;
        }

        public VkPipeline GetGraphicsPipeline(RenderPass renderPass, Shader shader, Geometry geometry)
        {
            if(pipeline != null)
            {
                return pipeline;
            }

            var pass = shader.GetPass(renderPass.passID);
            if(pass == null)
            {
                return 0;
            }
      
            var pipelineLayoutInfo = PipelineLayoutCreateInfo(ref pass.ResourceLayout.descriptorSetLayout, 1);
            vkCreatePipelineLayout(Graphics.device, ref pipelineLayoutInfo, IntPtr.Zero, out pipelineLayout);

            unsafe
            {
                VkPipelineShaderStageCreateInfo* shaderStageCreateInfos = stackalloc VkPipelineShaderStageCreateInfo[6];
                uint count = pass.GetShaderStageCreateInfos(shaderStageCreateInfos);
                var viewportStateCreateInfo = ViewportStateCreateInfo(1, 1);
                var inputAssemblyStateCreateInfo = InputAssemblyStateCreateInfo(geometry ? geometry.PrimitiveTopology : PrimitiveTopology);
                var pipelineCreateInfo = GraphicsPipelineCreateInfo(pipelineLayout, renderPass.renderPass, 0);//,

                var vertexInputState = vertexlayout.ToNative();

                pipelineCreateInfo.stageCount = count;
                pipelineCreateInfo.pStages = shaderStageCreateInfos;
                pipelineCreateInfo.pInputAssemblyState = &inputAssemblyStateCreateInfo;
                pipelineCreateInfo.pVertexInputState = &vertexInputState;
                pipelineCreateInfo.pRasterizationState = (VkPipelineRasterizationStateCreateInfo*)Unsafe.AsPointer(ref rasterizationState_);
                pipelineCreateInfo.pViewportState = &viewportStateCreateInfo;
                pipelineCreateInfo.pMultisampleState = (VkPipelineMultisampleStateCreateInfo*)Unsafe.AsPointer(ref multisampleState);
                pipelineCreateInfo.pDepthStencilState = (VkPipelineDepthStencilStateCreateInfo*)Unsafe.AsPointer(ref depthStencilState_);
                pipelineCreateInfo.pColorBlendState = (VkPipelineColorBlendStateCreateInfo*)Unsafe.AsPointer(ref colorBlendState);

                VkPipelineDynamicStateCreateInfo dynamicStateCreateInfo;
                if (DynamicStateCreateInfo.HasValue)
                {
                    dynamicStateCreateInfo = DynamicStateCreateInfo.Value;
                    pipelineCreateInfo.pDynamicState = &dynamicStateCreateInfo;
                }

                pipeline = Device.CreateGraphicsPipeline(ref pipelineCreateInfo);
            }

            return pipeline;
        }

        public VkPipeline GetComputePipeline(Pass shaderPass)
        {
            if(!shaderPass.IsComputeShader)
            {
                return 0;
            }

            var pipelineLayoutInfo = PipelineLayoutCreateInfo(ref shaderPass.ResourceLayout.descriptorSetLayout, 1);
            vkCreatePipelineLayout(Graphics.device, ref pipelineLayoutInfo, IntPtr.Zero, out pipelineLayout);

            var pipelineCreateInfo = VkComputePipelineCreateInfo.New();
            pipelineCreateInfo.stage = shaderPass.GetComputeStageCreateInfo();
            pipelineCreateInfo.layout = pipelineLayout;

            pipeline = Device.CreateComputePipeline(ref pipelineCreateInfo);
            return pipeline;
            
        }
        
    }
}
