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
        Multiply,
        Alpha,
        AddAlpha,
        PremulAlpha,
        InvdestAlpha,
        Subtract,
        SubtractAlpha,
    }

    public class Pipeline : Resource
    {
        private RasterizationStateInfo rasterizationState;
        public ref RasterizationStateInfo RasterizationState => ref rasterizationState;

        private MultisampleStateInfo multisampleState;
        public ref MultisampleStateInfo MultisampleState => ref multisampleState;

        private DepthStencilStateInfo depthStencilState_;
        public ref DepthStencilStateInfo DepthStencilState => ref depthStencilState_;

        private ColorBlendStateInfo colorBlendState;
        public ref ColorBlendStateInfo ColorBlendState => ref colorBlendState;
        public PrimitiveTopology PrimitiveTopology { get; set; } = PrimitiveTopology.TriangleList;

        private VertexLayout vertexlayout;
        public ref VertexLayout VertexLayout => ref vertexlayout;

        public PolygonMode FillMode { get => rasterizationState.polygonMode; set => rasterizationState.polygonMode = value; }
        public CullMode CullMode { get => rasterizationState.cullMode; set => rasterizationState.cullMode = value; }
        public FrontFace FrontFace { get => rasterizationState.frontFace; set => rasterizationState.frontFace = value; }
        public bool DepthTestEnable { get => depthStencilState_.depthTestEnable; set => depthStencilState_.depthTestEnable = value; }
        public bool DepthWriteEnable { get => depthStencilState_.depthWriteEnable; set => depthStencilState_.depthWriteEnable = value; }
        public BlendMode BlendMode { set => SetBlendMode(value); }
        public DynamicStateInfo? DynamicState {get; set;}

        public VkPipelineLayout pipelineLayout;
        public VkPipeline pipeline;

        public Pipeline()
        {
            Init();
        }
        
        public void Init()
        {
            vertexlayout = new VertexLayout();

            RasterizationState = new RasterizationStateInfo
            {
                polygonMode = PolygonMode.Fill,
                cullMode = CullMode.Back,
                frontFace = FrontFace.CounterClockwise,
                lineWidth = 1.0f
            };

            MultisampleState = new MultisampleStateInfo
            {
                rasterizationSamples = VkSampleCountFlags.Count1,
                minSampleShading = 1.0f
            };

            DepthStencilState = new DepthStencilStateInfo
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
            Device.DestroyPipeline(pipeline);
            pipeline = 0;
            base.Destroy();
        }

        public unsafe void SetBlendMode(BlendMode blendMode)
        {
            switch (blendMode)
            {
                case BlendMode.Replace:

                    ColorBlendState = new ColorBlendStateInfo
                    {
                        attachments = new[]
                        {
                            new ColorBlendAttachment
                            {
                                srcColorBlendFactor = VkBlendFactor.One,
                                dstColorBlendFactor = VkBlendFactor.Zero,
                                colorBlendOp = VkBlendOp.Add,
                                srcAlphaBlendFactor = VkBlendFactor.One,
                                dstAlphaBlendFactor = VkBlendFactor.Zero,
                                alphaBlendOp = VkBlendOp.Add,
                                colorWriteMask = (VkColorComponentFlags)0xf
                            }
                        }
                    };
                    break;
                case BlendMode.Add:

                    ColorBlendState = new ColorBlendStateInfo
                    {
                        attachments = new[]
                        {
                            new ColorBlendAttachment
                            {
                                srcColorBlendFactor = VkBlendFactor.One,
                                dstColorBlendFactor = VkBlendFactor.One,
                                colorBlendOp = VkBlendOp.Add,
                                srcAlphaBlendFactor = VkBlendFactor.SrcAlpha,
                                dstAlphaBlendFactor = VkBlendFactor.DstAlpha,
                                alphaBlendOp = VkBlendOp.Add,
                                colorWriteMask = (VkColorComponentFlags)0xf
                            }
                        }
                    };

                    break;
                case BlendMode.Multiply:
                    break;
                case BlendMode.Alpha:
                   
                    ColorBlendState = new ColorBlendStateInfo
                    {
                        attachments = new[]
                        {
                            new ColorBlendAttachment
                            {
                                blendEnable = true,
                                srcColorBlendFactor = VkBlendFactor.SrcAlpha,
                                dstColorBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                                colorBlendOp = VkBlendOp.Add,
                                srcAlphaBlendFactor = VkBlendFactor.SrcAlpha,
                                dstAlphaBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                                alphaBlendOp = VkBlendOp.Add,
                                colorWriteMask = (VkColorComponentFlags)0xf
                            }
                        }
                    };
                    break;
                case BlendMode.AddAlpha:
                    break;
                case BlendMode.PremulAlpha:
                   
                    ColorBlendState = new ColorBlendStateInfo
                    {
                        attachments = new[]
                        {
                            new ColorBlendAttachment
                            {
                                blendEnable = true,
                                srcColorBlendFactor = VkBlendFactor.One,
                                dstColorBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                                colorBlendOp = VkBlendOp.Add,
                                srcAlphaBlendFactor = VkBlendFactor.One,
                                dstAlphaBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                                alphaBlendOp = VkBlendOp.Add,
                                colorWriteMask = (VkColorComponentFlags)0xf
                            }
                        }
                    };
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
            Device.DestroyPipeline(pipeline);
            pipeline = 0;
        }

        public unsafe VkPipeline GetGraphicsPipeline(RenderPass renderPass, Pass pass, Geometry geometry)
        {
            if(pipeline != 0)
            {
                return pipeline;
            }

            if(pass == null)
            {
                return 0;
            }

            VkDescriptorSetLayout* pSetLayouts = stackalloc VkDescriptorSetLayout[pass.ResourceLayout.Length];
            for(int i = 0; i < pass.ResourceLayout.Length; i++)
            {
                pSetLayouts[i] = pass.ResourceLayout[i].descriptorSetLayout;
            }

            var pipelineLayoutInfo = PipelineLayoutCreateInfo(pSetLayouts, pass.ResourceLayout.Length);
            vkCreatePipelineLayout(Graphics.device, ref pipelineLayoutInfo, IntPtr.Zero, out pipelineLayout);

            unsafe
            {
                var pipelineCreateInfo = GraphicsPipelineCreateInfo(pipelineLayout, renderPass.handle, 0);//,

                vertexlayout.ToNative(out VkPipelineVertexInputStateCreateInfo vertexInputState);
                pipelineCreateInfo.pVertexInputState = &vertexInputState;

                VkPipelineShaderStageCreateInfo* shaderStageCreateInfos = stackalloc VkPipelineShaderStageCreateInfo[6];
                uint count = pass.GetShaderStageCreateInfos(shaderStageCreateInfos);
                pipelineCreateInfo.stageCount = count;
                pipelineCreateInfo.pStages = shaderStageCreateInfos;

                var inputAssemblyStateCreateInfo = InputAssemblyStateCreateInfo(geometry ? geometry.PrimitiveTopology : PrimitiveTopology);
                pipelineCreateInfo.pInputAssemblyState = &inputAssemblyStateCreateInfo;

                rasterizationState.ToNative(out VkPipelineRasterizationStateCreateInfo rasterizationStateCreateInfo);
                pipelineCreateInfo.pRasterizationState = &rasterizationStateCreateInfo;

                var viewportStateCreateInfo = ViewportStateCreateInfo(1, 1);
                pipelineCreateInfo.pViewportState = &viewportStateCreateInfo;

                this.multisampleState.ToNative(out VkPipelineMultisampleStateCreateInfo multisampleState);
                pipelineCreateInfo.pMultisampleState = &multisampleState;

                depthStencilState_.ToNative(out VkPipelineDepthStencilStateCreateInfo depthStencilState);
                pipelineCreateInfo.pDepthStencilState = &depthStencilState;

                ColorBlendState.ToNative(out VkPipelineColorBlendStateCreateInfo colorBlendState);
                pipelineCreateInfo.pColorBlendState = &colorBlendState;

                VkPipelineDynamicStateCreateInfo dynamicStateCreateInfo;
                if (DynamicState.HasValue)
                {
                    DynamicState.Value.ToNative(out dynamicStateCreateInfo);
                    pipelineCreateInfo.pDynamicState = &dynamicStateCreateInfo;
                }

                pipeline = Device.CreateGraphicsPipeline(ref pipelineCreateInfo);
            }

            return pipeline;
        }

        public unsafe VkPipeline GetComputePipeline(Pass pass)
        {
            if(!pass.IsComputeShader)
            {
                return 0;
            }


            VkDescriptorSetLayout* pSetLayouts = stackalloc VkDescriptorSetLayout[pass.ResourceLayout.Length];
            for (int i = 0; i < pass.ResourceLayout.Length; i++)
            {
                pSetLayouts[i] = pass.ResourceLayout[i].descriptorSetLayout;
            }

            var pipelineLayoutInfo = PipelineLayoutCreateInfo(pSetLayouts, pass.ResourceLayout.Length);
            vkCreatePipelineLayout(Graphics.device, ref pipelineLayoutInfo, IntPtr.Zero, out pipelineLayout);

            var pipelineCreateInfo = VkComputePipelineCreateInfo.New();
            pipelineCreateInfo.stage = pass.GetComputeStageCreateInfo();
            pipelineCreateInfo.layout = pipelineLayout;

            pipeline = Device.CreateComputePipeline(ref pipelineCreateInfo);
            return pipeline;
            
        }
        
    }
}
