using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using global::System.Runtime.Serialization;
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
        [IgnoreDataMember]
        public Shader Shader { get; set; }

        private RasterizationStateInfo rasterizationState;
        [IgnoreDataMember]
        public ref RasterizationStateInfo RasterizationState => ref rasterizationState;

        private MultisampleStateInfo multisampleState;
        [IgnoreDataMember]
        public ref MultisampleStateInfo MultisampleState => ref multisampleState;

        private DepthStencilStateInfo depthStencilState_;
        [IgnoreDataMember]
        public ref DepthStencilStateInfo DepthStencilState => ref depthStencilState_;

        private ColorBlendStateInfo colorBlendState;
        [IgnoreDataMember]
        public ref ColorBlendStateInfo ColorBlendState => ref colorBlendState;

        [IgnoreDataMember]
        public PrimitiveTopology PrimitiveTopology { get; set; } = PrimitiveTopology.TriangleList;

        private VertexLayout vertexlayout;
        [IgnoreDataMember]
        public ref VertexLayout VertexLayout => ref vertexlayout;

        public PolygonMode FillMode { get => rasterizationState.polygonMode; set => rasterizationState.polygonMode = value; }
        public CullMode CullMode { get => rasterizationState.cullMode; set => rasterizationState.cullMode = value; }
        public FrontFace FrontFace { get => rasterizationState.frontFace; set => rasterizationState.frontFace = value; }
        public bool DepthTestEnable { get => depthStencilState_.depthTestEnable; set => depthStencilState_.depthTestEnable = value; }
        public bool DepthWriteEnable { get => depthStencilState_.depthWriteEnable; set => depthStencilState_.depthWriteEnable = value; }
        public BlendMode BlendMode { set => SetBlendMode(value); }
        public DynamicStateInfo DynamicState {get; set;}

        public ResourceLayout[] ResourceLayout { get; set; }

        internal VkPipelineLayout pipelineLayout;
        internal VkPipeline pipeline;

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
                rasterizationSamples = SampleCountFlags.Count1,
                minSampleShading = 1.0f
            };

            DepthStencilState = new DepthStencilStateInfo
            {
                depthTestEnable = true,
                depthWriteEnable = true,
                depthCompareOp = CompareOp.LessOrEqual,
                back = new StencilOpState
                {
                    failOp = StencilOp.Keep,
                    passOp = StencilOp.Keep,
                    compareOp = CompareOp.Always
                },
                front = new StencilOpState
                {
                    failOp = StencilOp.Keep,
                    passOp = StencilOp.Keep,
                    compareOp = CompareOp.Always
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
                                srcColorBlendFactor = BlendFactor.One,
                                dstColorBlendFactor = BlendFactor.Zero,
                                colorBlendOp = BlendOp.Add,
                                srcAlphaBlendFactor = BlendFactor.One,
                                dstAlphaBlendFactor = BlendFactor.Zero,
                                alphaBlendOp = BlendOp.Add,
                                colorWriteMask = ColorComponentFlags.All
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
                                srcColorBlendFactor = BlendFactor.One,
                                dstColorBlendFactor = BlendFactor.One,
                                colorBlendOp = BlendOp.Add,
                                srcAlphaBlendFactor = BlendFactor.SrcAlpha,
                                dstAlphaBlendFactor = BlendFactor.DstAlpha,
                                alphaBlendOp = BlendOp.Add,
                                colorWriteMask = ColorComponentFlags.All
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
                                srcColorBlendFactor = BlendFactor.SrcAlpha,
                                dstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                                colorBlendOp = BlendOp.Add,
                                srcAlphaBlendFactor = BlendFactor.SrcAlpha,
                                dstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha,
                                alphaBlendOp = BlendOp.Add,
                                colorWriteMask = ColorComponentFlags.All
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
                                srcColorBlendFactor = BlendFactor.One,
                                dstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                                colorBlendOp = BlendOp.Add,
                                srcAlphaBlendFactor = BlendFactor.One,
                                dstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha,
                                alphaBlendOp = BlendOp.Add,
                                colorWriteMask = ColorComponentFlags.All
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

        internal unsafe VkPipeline GetGraphicsPipeline(RenderPass renderPass, Pass pass, Geometry geometry)
        {
            if(pipeline != 0)
            {
                return pipeline;
            }

            if(pass == null)
            {
                return 0;
            }

            VkDescriptorSetLayout* pSetLayouts = stackalloc VkDescriptorSetLayout[ResourceLayout.Length];
            for(int i = 0; i < ResourceLayout.Length; i++)
            {
                pSetLayouts[i] = ResourceLayout[i].descriptorSetLayout;
            }

            var pipelineLayoutInfo = PipelineLayoutCreateInfo(pSetLayouts, ResourceLayout.Length);
            vkCreatePipelineLayout(Graphics.device, ref pipelineLayoutInfo, IntPtr.Zero, out pipelineLayout);

            unsafe
            {
                var pipelineCreateInfo = GraphicsPipelineCreateInfo(pipelineLayout, renderPass.handle, 0);//,
                var vertexInput = geometry != null ? geometry.VertexLayout : vertexlayout;
                vertexInput.ToNative(out VkPipelineVertexInputStateCreateInfo vertexInputState);
                pipelineCreateInfo.pVertexInputState = &vertexInputState;

                VkPipelineShaderStageCreateInfo* shaderStageCreateInfos = stackalloc VkPipelineShaderStageCreateInfo[6];
                uint count = pass.GetShaderStageCreateInfos(shaderStageCreateInfos);
                pipelineCreateInfo.stageCount = count;
                pipelineCreateInfo.pStages = shaderStageCreateInfos;

                var inputAssemblyStateCreateInfo = InputAssemblyStateCreateInfo(geometry ? geometry.PrimitiveTopology : PrimitiveTopology);
                pipelineCreateInfo.pInputAssemblyState = &inputAssemblyStateCreateInfo;

                rasterizationState.ToNative(out VkPipelineRasterizationStateCreateInfo rasterizationStateCreateInfo);
                pipelineCreateInfo.pRasterizationState = &rasterizationStateCreateInfo;

                //var viewportStateCreateInfo = ViewportStateCreateInfo(1, 1);
               // pipelineCreateInfo.pViewportState = &viewportStateCreateInfo;

                this.multisampleState.ToNative(out VkPipelineMultisampleStateCreateInfo multisampleState);
                pipelineCreateInfo.pMultisampleState = &multisampleState;

                depthStencilState_.ToNative(out VkPipelineDepthStencilStateCreateInfo depthStencilState);
                pipelineCreateInfo.pDepthStencilState = &depthStencilState;

                ColorBlendState.ToNative(out VkPipelineColorBlendStateCreateInfo colorBlendState);
                pipelineCreateInfo.pColorBlendState = &colorBlendState;

                VkPipelineDynamicStateCreateInfo dynamicStateCreateInfo;
                if (DynamicState.HasValue)
                {
                    DynamicState.ToNative(out dynamicStateCreateInfo);
                    pipelineCreateInfo.pDynamicState = &dynamicStateCreateInfo;
                }

                pipeline = Device.CreateGraphicsPipeline(ref pipelineCreateInfo);
            }

            return pipeline;
        }

        internal unsafe VkPipeline GetComputePipeline(Pass pass)
        {
            if(!pass.IsComputeShader)
            {
                return 0;
            }

            VkDescriptorSetLayout* pSetLayouts = stackalloc VkDescriptorSetLayout[ResourceLayout.Length];
            for (int i = 0; i < ResourceLayout.Length; i++)
            {
                pSetLayouts[i] = ResourceLayout[i].descriptorSetLayout;
            }

            var pipelineLayoutInfo = PipelineLayoutCreateInfo(pSetLayouts, ResourceLayout.Length);
            vkCreatePipelineLayout(Graphics.device, ref pipelineLayoutInfo, IntPtr.Zero, out pipelineLayout);

            var pipelineCreateInfo = VkComputePipelineCreateInfo.New();
            pipelineCreateInfo.stage = pass.GetComputeStageCreateInfo();
            pipelineCreateInfo.layout = pipelineLayout;

            pipeline = Device.CreateComputePipeline(ref pipelineCreateInfo);
            return pipeline;
            
        }
        
    }
}
