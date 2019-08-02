using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using global::System.Runtime.CompilerServices;
    using static VulkanNative;
    using static Builder;

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

    public partial class Pass : DisposeBase
    {
        public static readonly StringID Shadow = "shadow";
        public static readonly StringID Depth = "depth";
        public static readonly StringID Clear = "clear";
        public static readonly StringID Main = "main";

        private static List<StringID> passList = new List<StringID>();

        static Pass()
        {
            passList.Add(Main);
        }

        public static ulong GetID(StringID pass)
        {
            if (pass.IsNullOrEmpty)
            {
                return 1;
            }

            for (int i = 0; i < passList.Count; i++)
            {
                if (passList[i] == pass)
                {
                    return (ulong)(1 << i);
                }
            }

            if (passList.Count >= 64)
            {
                return 1;
            }

            passList.Add(pass);
            return (ulong)(1 << (passList.Count - 1));
        }

        private StringID name_;
        public StringID Name
        {
            get => name_;
            set
            {
                name_ = value;
                passID = GetID(value);
            }
        }

        [IgnoreDataMember]
        public ulong passID;

        [DataMember]
        public ShaderModule[] ShaderModels { get; set; } = new ShaderModule[6];

        [IgnoreDataMember]
        public bool IsComputeShader => ShaderModels[5] != null;

        private bool builded_ = false;

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
        public DynamicStateInfo DynamicStates { get; set; } = new DynamicStateInfo(DynamicState.Viewport, DynamicState.Scissor);
        public ResourceLayout[] ResourceLayout { get; set; }

        private PushConstantRange[] pushConstantRanges;
        public PushConstantRange[] PushConstantRanges { get => pushConstantRanges; set => pushConstantRanges = value; }

        [IgnoreDataMember]
        public PrimitiveTopology PrimitiveTopology { get; set; } = PrimitiveTopology.TriangleList;
        [IgnoreDataMember]
        public VertexLayout VertexLayout { get; set; }

        internal VkPipelineLayout pipelineLayout;
        internal VkPipeline handle;

        public Pass()
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

        public Pass(string vertexShader = null, string pixelShader = null, string geometryShader = null,
            string hullShader = null, string domainShader = null, string computeShader = null)
        {
            if (!string.IsNullOrEmpty(vertexShader))
            {
                ShaderModels[0] = new ShaderModule(ShaderStage.Vertex, vertexShader);
            }

            if (!string.IsNullOrEmpty(pixelShader))
            {
                ShaderModels[4] = new ShaderModule(ShaderStage.Fragment, pixelShader);
            }

            if (!string.IsNullOrEmpty(geometryShader))
            {
                ShaderModels[1] = new ShaderModule(ShaderStage.Geometry, geometryShader);
            }

            if (!string.IsNullOrEmpty(hullShader))
            {
                ShaderModels[2] = new ShaderModule(ShaderStage.TessellationControl, hullShader);
            }

            if (!string.IsNullOrEmpty(domainShader))
            {
                ShaderModels[3] = new ShaderModule(ShaderStage.TessellationEvaluation, domainShader);
            }

            if (!string.IsNullOrEmpty(computeShader))
            {
                ShaderModels[5] = new ShaderModule(ShaderStage.Compute, computeShader);
            }

            Build();
        }
                
        public void Build()
        {
            if (builded_)
            {
                return;
            }

            builded_ = true;
            passID = GetID(Name);

            foreach(var sm in ShaderModels)
            {
                sm?.Build();
            }

        }

        protected override void Destroy()
        {
            foreach (var stage in ShaderModels)
            {
                stage?.Dispose();
            }

            if (handle != 0)
            {
                Device.Destroy(ref handle);
            }

            if (pipelineLayout != 0)
            {
                Device.DestroyPipelineLayout(pipelineLayout);
                pipelineLayout = 0;
            }

            base.Destroy();
        }

        public unsafe uint GetShaderStageCreateInfos(VkPipelineShaderStageCreateInfo* shaderStageCreateInfo)
        {
            uint count = 0;
            foreach (var sm in ShaderModels)
            {
                if (sm != null)
                {
                    var shaderStage = VkPipelineShaderStageCreateInfo.New();
                    shaderStage.stage = (VkShaderStageFlags)sm.Stage;
                    shaderStage.module = sm.shaderModule;
                    shaderStage.pName = Strings.main;// sm.FuncName;
                    shaderStageCreateInfo[count++] = shaderStage;
                }
            }

            return count;
        }

        public unsafe VkPipelineShaderStageCreateInfo GetComputeStageCreateInfo()
        {
            if (ShaderModels[5] != null)
            {
                var shaderStage = VkPipelineShaderStageCreateInfo.New();
                shaderStage.stage = VkShaderStageFlags.Compute;
                shaderStage.module = ShaderModels[5].shaderModule;
                shaderStage.pName = Strings.main;// sm.FuncName;
                return shaderStage;
            }

            return default;
        }

        internal unsafe VkPipeline GetGraphicsPipeline(RenderPass renderPass, Geometry geometry)
        {
            if (handle != 0)
            {
                return handle;
            }

            VkDescriptorSetLayout* pSetLayouts = stackalloc VkDescriptorSetLayout[ResourceLayout.Length];
            for (int i = 0; i < ResourceLayout.Length; i++)
            {
                pSetLayouts[i] = ResourceLayout[i].DescriptorSetLayout;
            }

            var pipelineLayoutInfo = PipelineLayoutCreateInfo(pSetLayouts, ResourceLayout.Length);
            if (!pushConstantRanges.IsNullOrEmpty())
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
                uint count = GetShaderStageCreateInfos(shaderStageCreateInfos);
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



        internal unsafe VkPipeline GetComputePipeline()
        {
            if (handle != 0)
            {
                return handle;
            }

            if (!IsComputeShader)
            {
                return 0;
            }

            VkDescriptorSetLayout* pSetLayouts = stackalloc VkDescriptorSetLayout[ResourceLayout.Length];
            for (int i = 0; i < ResourceLayout.Length; i++)
            {
                pSetLayouts[i] = ResourceLayout[i].DescriptorSetLayout;
            }

            var pipelineLayoutInfo = Builder.PipelineLayoutCreateInfo(pSetLayouts, ResourceLayout.Length);
            if (!pushConstantRanges.IsNullOrEmpty())
            {
                pipelineLayoutInfo.pushConstantRangeCount = (uint)pushConstantRanges.Length;
                pipelineLayoutInfo.pPushConstantRanges = (VkPushConstantRange*)Unsafe.AsPointer(ref pushConstantRanges[0]);
            }
            vkCreatePipelineLayout(Graphics.device, ref pipelineLayoutInfo, IntPtr.Zero, out pipelineLayout);

            var pipelineCreateInfo = VkComputePipelineCreateInfo.New();
            pipelineCreateInfo.stage = GetComputeStageCreateInfo();
            pipelineCreateInfo.layout = pipelineLayout;

            handle = Device.CreateComputePipeline(ref pipelineCreateInfo);
            return handle;

        }
    }
}
