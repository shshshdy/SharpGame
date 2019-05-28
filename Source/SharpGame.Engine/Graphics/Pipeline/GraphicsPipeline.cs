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

    public struct PushConstantRange
    {
        public ShaderStage stageFlags;
        public int offset;
        public int size;
        public PushConstantRange(ShaderStage shaderStage, int offset, int size)
        {
            this.stageFlags = shaderStage;
            this.offset = offset;
            this.size = size;
        }
    }

    public class GraphicsPipeline : Resource<GraphicsPipeline>
    {
        [IgnoreDataMember]
        public Shader Shader { get; set; }

        private RasterizationStateInfo rasterizationState = RasterizationStateInfo.Default;
        [IgnoreDataMember]
        public ref RasterizationStateInfo RasterizationState => ref rasterizationState;

        private MultisampleStateInfo multisampleState = MultisampleStateInfo.Default;
        [IgnoreDataMember]
        public ref MultisampleStateInfo MultisampleState => ref multisampleState;

        private DepthStencilStateInfo depthStencilState_ = DepthStencilStateInfo.Solid;
        [IgnoreDataMember]
        public ref DepthStencilStateInfo DepthStencilState => ref depthStencilState_;

        private ColorBlendStateInfo colorBlendState = ColorBlendStateInfo.Replace;
        [IgnoreDataMember]
        public ref ColorBlendStateInfo ColorBlendState => ref colorBlendState;

        public PolygonMode FillMode { get => rasterizationState.polygonMode; set => rasterizationState.polygonMode = value; }
        public CullMode CullMode { get => rasterizationState.cullMode; set => rasterizationState.cullMode = value; }
        public FrontFace FrontFace { get => rasterizationState.frontFace; set => rasterizationState.frontFace = value; }
        public bool DepthTestEnable { get => depthStencilState_.depthTestEnable; set => depthStencilState_.depthTestEnable = value; }
        public bool DepthWriteEnable { get => depthStencilState_.depthWriteEnable; set => depthStencilState_.depthWriteEnable = value; }

        private BlendMode blendMode = BlendMode.Replace;
        public BlendMode BlendMode { get => blendMode; set { blendMode = value; SetBlendMode(value); } } 
        public DynamicStateInfo DynamicStates {get; set;} = new DynamicStateInfo(DynamicState.Viewport, DynamicState.Scissor);
        public ResourceLayout[] ResourceLayout { get; set; }

        private PushConstantRange[] pushConstantRanges;
        public PushConstantRange[] PushConstantRanges { get => pushConstantRanges; set => pushConstantRanges = value; }

        [IgnoreDataMember]
        public PrimitiveTopology PrimitiveTopology { get; set; } = PrimitiveTopology.TriangleList;
        [IgnoreDataMember]
        public VertexLayout VertexLayout { get; set; }

        internal VkPipelineLayout pipelineLayout;
        internal VkPipeline handle;

        public GraphicsPipeline()
        {
        }
        
        public unsafe void SetBlendMode(BlendMode blendMode)
        {
            switch (blendMode)
            {
                case BlendMode.Replace:
                    ColorBlendState = ColorBlendStateInfo.Replace;
                    break;
                case BlendMode.Add:
                    ColorBlendState = ColorBlendStateInfo.Add;
                    break;
                case BlendMode.Multiply:
                    break;
                case BlendMode.Alpha:
                    ColorBlendState = ColorBlendStateInfo.AlphaBlend;
                    break;
                case BlendMode.AddAlpha:
                    break;
                case BlendMode.PremulAlpha:
                    ColorBlendState = ColorBlendStateInfo.PremulAlpha;
                    break;
                case BlendMode.InvdestAlpha:
                    break;
                case BlendMode.Subtract:
                    break;
                case BlendMode.SubtractAlpha:
                    break;
            }
        }

        internal unsafe VkPipeline GetGraphicsPipeline(RenderPass renderPass, Pass pass, Geometry geometry)
        {
            if(handle != 0)
            {
                return handle;
            }

            if(pass == null)
            {
                return 0;
            }

            VkDescriptorSetLayout* pSetLayouts = stackalloc VkDescriptorSetLayout[ResourceLayout.Length];
            for(int i = 0; i < ResourceLayout.Length; i++)
            {
                pSetLayouts[i] = ResourceLayout[i].DescriptorSetLayout;
            }

            var pipelineLayoutInfo = PipelineLayoutCreateInfo(pSetLayouts, ResourceLayout.Length);
            if(!pushConstantRanges.IsNullOrEmpty())
            {
                pipelineLayoutInfo.pushConstantRangeCount = (uint)pushConstantRanges.Length;
                pipelineLayoutInfo.pPushConstantRanges = (VkPushConstantRange*)Unsafe.AsPointer(ref pushConstantRanges[0]);
            }
            vkCreatePipelineLayout(Graphics.device, ref pipelineLayoutInfo, IntPtr.Zero, out pipelineLayout);

            unsafe
            {
                var pipelineCreateInfo = GraphicsPipelineCreateInfo(pipelineLayout, renderPass.handle, 0);//,
                var vertexInput = geometry != null ? geometry.VertexLayout : VertexLayout;
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

                var viewportStateCreateInfo = ViewportStateCreateInfo(1, 1);
                pipelineCreateInfo.pViewportState = &viewportStateCreateInfo;

                this.multisampleState.ToNative(out VkPipelineMultisampleStateCreateInfo multisampleState);
                pipelineCreateInfo.pMultisampleState = &multisampleState;

                depthStencilState_.ToNative(out VkPipelineDepthStencilStateCreateInfo depthStencilState);
                pipelineCreateInfo.pDepthStencilState = &depthStencilState;

                ColorBlendState.ToNative(out VkPipelineColorBlendStateCreateInfo colorBlendState);
                pipelineCreateInfo.pColorBlendState = &colorBlendState;

                VkPipelineDynamicStateCreateInfo dynamicStateCreateInfo;
                if (DynamicStates.HasValue)
                {
                    DynamicStates.ToNative(out dynamicStateCreateInfo);
                    pipelineCreateInfo.pDynamicState = &dynamicStateCreateInfo;
                }

                handle = Device.CreateGraphicsPipeline(ref pipelineCreateInfo);
            }

            return handle;
        }

        protected override void Destroy()
        {
            if(handle != 0)
            {
                Device.Destroy(ref handle);                
            }

            if(pipelineLayout != 0)
            {
                Device.DestroyPipelineLayout(pipelineLayout);
                pipelineLayout = 0;
            }

            base.Destroy();
        }


    }
}
