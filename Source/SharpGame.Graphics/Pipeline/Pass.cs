﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;


namespace SharpGame
{
    using global::System.Runtime.CompilerServices;
    using global::System.Collections.Concurrent;

    public struct SpecializationMapEntry
    {
        public uint constantID;
        public uint offset;
        public UIntPtr size;

        public SpecializationMapEntry(uint id, uint offset, uint size)
        {
            constantID = id;
            this.offset = offset;
            this.size = (UIntPtr)size;
        }
    }

    public unsafe class SpecializationInfo
    {
        public SpecializationMapEntry[] mapEntries;
        public byte[] data;
        private VkSpecializationInfo* pSpecializationInfo;

        public SpecializationInfo()
        {
        }

        public SpecializationInfo(params SpecializationMapEntry[] mapEntries)
        {
            this.mapEntries = mapEntries;
            uint size = 0;
            foreach(var entry in mapEntries)
            {
                var sz = entry.offset + (uint)entry.size;
                if(sz > size)
                {
                    size = sz;
                }
            }

            data = new byte[size];
            pSpecializationInfo = (VkSpecializationInfo*)Utilities.Alloc<VkSpecializationInfo>();
        }

        ~SpecializationInfo()
        {
            Utilities.Free((IntPtr)pSpecializationInfo);
        }

        public int Offset(uint id)
        {
            for(int i = 0; i < mapEntries.Length; i++)
            {
                if(id == mapEntries[i].constantID)
                {
                    return (int)mapEntries[i].offset;
                }
            }

            return -1;
        }

        public unsafe SpecializationInfo Write<T>(uint id, T val)
        {
            int offset = Offset(id);
            if(offset >= 0)
            {
                Unsafe.AsRef<T>(Unsafe.AsPointer(ref data[offset])) = val;
            }
            else
            {
                Log.Error("Error constant id: " + id);
            }
            return this;
        }

        internal VkSpecializationInfo* ToNative
        {
            get
            {
                pSpecializationInfo->pMapEntries = (VkSpecializationMapEntry*)Unsafe.AsPointer(ref mapEntries[0]);
                pSpecializationInfo->mapEntryCount = (uint)mapEntries.Length;
                pSpecializationInfo->pData = Unsafe.AsPointer(ref data[0]);
                pSpecializationInfo->dataSize = (UIntPtr)data.Length;
                return pSpecializationInfo;
            }
        }
    }

    public partial class Pass : DisposeBase
    {
        public static readonly string Shadow = "shadow";
        public static readonly string Depth = "depth";
        public static readonly string EarlyZ = "early_z";
        public static readonly string Clear = "clear";
        public static readonly string Main = "main";

        private static List<string> passList = new List<string>();

        static Pass()
        {
            passList.Add(Main);
        }

        public static ulong GetID(string pass)
        {
            if (string.IsNullOrEmpty(pass))
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

        private string name_;
        public string Name
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

        [IgnoreDataMember]
        public int passIndex;

        readonly ShaderModule[] shaderModels = new ShaderModule[6];

        [DataMember]
        public ShaderModule[] ShaderModels { get => shaderModels; }

        [IgnoreDataMember]
        public ref ShaderModule VertexShader => ref shaderModels[0];

        [IgnoreDataMember]
        public ref ShaderModule PixelShader => ref shaderModels[4];

        [IgnoreDataMember]
        public ref ShaderModule GeometryShader => ref shaderModels[1];

        [IgnoreDataMember]
        public ref ShaderModule HullShader => ref shaderModels[2];

        [IgnoreDataMember]
        public ref ShaderModule DomainShader => ref shaderModels[3];

        [IgnoreDataMember]
        public ref ShaderModule ComputeShader => ref shaderModels[5];

        [IgnoreDataMember]
        public bool IsComputeShader => ComputeShader != null;

        private bool builded_ = false;

        [IgnoreDataMember]
        public ref RasterizationStateInfo RasterizationState => ref rasterizationState;
        private RasterizationStateInfo rasterizationState = RasterizationStateInfo.Default;

        [IgnoreDataMember]
        public ref MultisampleStateInfo MultisampleState => ref multisampleState;
        private MultisampleStateInfo multisampleState = MultisampleStateInfo.Default;

        [IgnoreDataMember]
        public ref DepthStencilStateInfo DepthStencilState => ref depthStencilState_;
        private DepthStencilStateInfo depthStencilState_ = DepthStencilStateInfo.Solid;

        [IgnoreDataMember]
        public ref ColorBlendStateInfo ColorBlendState => ref colorBlendState;
        private ColorBlendStateInfo colorBlendState = ColorBlendStateInfo.Replace;

        public PolygonMode FillMode { get => rasterizationState.polygonMode; set => rasterizationState.polygonMode = value; }
        public CullMode CullMode { get => rasterizationState.cullMode; set => rasterizationState.cullMode = value; }
        public FrontFace FrontFace { get => rasterizationState.frontFace; set => rasterizationState.frontFace = value; }
        public bool DepthTestEnable { get => depthStencilState_.depthTestEnable; set => depthStencilState_.depthTestEnable = value; }
        public bool DepthWriteEnable { get => depthStencilState_.depthWriteEnable; set => depthStencilState_.depthWriteEnable = value; }

        private BlendMode blendMode = BlendMode.Replace;
        public BlendMode BlendMode { get => blendMode; set { blendMode = value; SetBlendMode(value); } }
        public DynamicStateInfo DynamicStates { get; set; } = new DynamicStateInfo(DynamicState.Viewport, DynamicState.Scissor);
        public string[] Defines { get; set; }

        public PipelineLayout PipelineLayout { get; set; } = new PipelineLayout();
     
        [IgnoreDataMember]
        public PrimitiveTopology PrimitiveTopology { get; set; } = PrimitiveTopology.TriangleList;
        [IgnoreDataMember]
        public VertexLayout VertexLayout { get; set; }

        internal Pipeline computeHandle;
        ConcurrentDictionary<long, Pipeline> pipelines = new ConcurrentDictionary<long, Pipeline>();

        public Pass()
        {
        }

        public void Build()
        {
            if (builded_)
            {
                return;
            }

            builded_ = true;
            passID = GetID(Name);

            List<DescriptorSetLayout> reslayouts = new List<DescriptorSetLayout>();
            foreach(var sm in ShaderModels)
            {
                if(sm != null)
                {
                    sm.Build();

                    if(sm.ShaderReflection != null && sm.ShaderReflection.descriptorSets != null)
                    {
                        var descriptors = sm.ShaderReflection.descriptorSets;
                        DescriptorSetLayout currentLayout = null;
                        foreach (var des in descriptors)
                        {
                            currentLayout = reslayouts.Find((i) => i.Set == des.set);
                            if (currentLayout == null)
                            {
                                currentLayout = new DescriptorSetLayout(des.set);
                                reslayouts.Add(currentLayout);
                            }
                            DescriptorSetLayoutBinding resBinding = currentLayout.Bindings.Find((i) => i.binding == des.binding);
                            if (resBinding == null)
                            {
                                resBinding = new DescriptorSetLayoutBinding
                                {
                                    name = des.name,
                                    binding = des.binding,
                                    descriptorType = des.descriptorType,
                                    stageFlags = sm.Stage
                                };

                                currentLayout.Add(resBinding);
                            }
                            else
                            {

                                if(resBinding.name == des.name && resBinding.descriptorType == des.descriptorType)
                                {
                                    resBinding.stageFlags |= sm.Stage;
                                }
                                else
                                    Log.Warn("Duplicate binding : " + des.name);
                            }


                        }

                    }

                }
            }
            reslayouts.Sort((x, y) => { return x.Set - y.Set; });
            PipelineLayout.ResourceLayout = reslayouts.ToArray();

            PipelineLayout.Build();

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

        public DescriptorSetLayout GetResourceLayout(int index)
        {
            if(index >= PipelineLayout.ResourceLayout.Length)
            {
                return null;
            }

            return PipelineLayout.ResourceLayout[index];
        }

        public bool GetPushConstant(string name, out PushConstantRange pushConstantRange)
        {
            if(PipelineLayout.PushConstantNames != null)
            {
                for (int i = 0; i < PipelineLayout.PushConstantNames.Count; i++)
                {
                    if (PipelineLayout.PushConstantNames[i] == name)
                    {
                        pushConstantRange = PipelineLayout.PushConstant[i];
                        return true;
                    }
                }
            }
          
            pushConstantRange = default;
            return false;
        }

        public unsafe uint GetShaderStageCreateInfos(Span<VkPipelineShaderStageCreateInfo> shaderStageCreateInfo)
        {
            int count = 0;
            foreach (var sm in ShaderModels)
            {
                if (sm != null)
                {
                    var shaderStage = new VkPipelineShaderStageCreateInfo { sType = VkStructureType.PipelineShaderStageCreateInfo };
                    shaderStage.stage = (VkShaderStageFlags)sm.Stage;
                    shaderStage.module = sm.shaderModule;
                    shaderStage.pName = Strings.main;// sm.FuncName;

                    if(sm.SpecializationInfo != null)
                    {
                        shaderStage.pSpecializationInfo = sm.SpecializationInfo.ToNative;
                    }

                    shaderStageCreateInfo[count++] = shaderStage;
                }
            }

            return (uint)count;
        }

        private unsafe VkPipelineShaderStageCreateInfo GetComputeStageCreateInfo()
        {
            if (ShaderModels[5] != null)
            {
                var shaderStage = new VkPipelineShaderStageCreateInfo
                {
                    sType = VkStructureType.PipelineShaderStageCreateInfo
                };
                shaderStage.stage = VkShaderStageFlags.Compute;
                shaderStage.module = ShaderModels[5].shaderModule;
                shaderStage.pName = Strings.main;// sm.FuncName;
                return shaderStage;
            }

            return default;
        }

        public Pipeline GetGraphicsPipeline(RenderPass renderPass, uint subPass, Geometry geometry)
        {
            var vertexInput = VertexLayout?? (geometry != null ? geometry.VertexLayout : null);
            var primitiveTopology = geometry != null ? geometry.PrimitiveTopology : PrimitiveTopology;

            if (pipelines.TryGetValue(vertexInput?.GetHashCode() ?? 0, out var pipe))
            {
                return pipe;
            }

            return CreateGraphicsPipeline(renderPass, subPass, vertexInput, primitiveTopology);            
        }

        public unsafe Pipeline CreateGraphicsPipeline(RenderPass renderPass, uint subPass, VertexLayout vertexInput, PrimitiveTopology primitiveTopology)
        {
            VkGraphicsPipelineCreateInfo pipelineCreateInfo = new VkGraphicsPipelineCreateInfo
            {
                sType = VkStructureType.GraphicsPipelineCreateInfo
            };
            pipelineCreateInfo.layout = PipelineLayout.handle;
            pipelineCreateInfo.renderPass = renderPass.handle;
            pipelineCreateInfo.subpass = subPass;
            pipelineCreateInfo.flags = 0;
            pipelineCreateInfo.basePipelineIndex = -1;
            pipelineCreateInfo.basePipelineHandle = new VkPipeline();

            VkPipelineVertexInputStateCreateInfo vertexInputState;
            if (vertexInput != null)
            {
                vertexInput.ToNative(out vertexInputState);
            }
            else
            {
                vertexInputState = new VkPipelineVertexInputStateCreateInfo { sType = VkStructureType.PipelineVertexInputStateCreateInfo };
            }

            pipelineCreateInfo.pVertexInputState = &vertexInputState;

            Span<VkPipelineShaderStageCreateInfo> shaderStageCreateInfos = stackalloc VkPipelineShaderStageCreateInfo[6];
            uint count = GetShaderStageCreateInfos(shaderStageCreateInfos);
            pipelineCreateInfo.stageCount = count;
            pipelineCreateInfo.pStages = (VkPipelineShaderStageCreateInfo*)Unsafe.AsPointer(ref shaderStageCreateInfos[0]);

            var pipelineInputAssemblyStateCreateInfo = new VkPipelineInputAssemblyStateCreateInfo
            {
                sType = VkStructureType.PipelineInputAssemblyStateCreateInfo
            };
            pipelineInputAssemblyStateCreateInfo.topology = (VkPrimitiveTopology)primitiveTopology;
            pipelineInputAssemblyStateCreateInfo.flags = 0;
            pipelineInputAssemblyStateCreateInfo.primitiveRestartEnable = false;
            pipelineCreateInfo.pInputAssemblyState = &pipelineInputAssemblyStateCreateInfo;

            rasterizationState.ToNative(out VkPipelineRasterizationStateCreateInfo rasterizationStateCreateInfo);
            pipelineCreateInfo.pRasterizationState = &rasterizationStateCreateInfo;

            var pipelineViewportStateCreateInfo = new VkPipelineViewportStateCreateInfo
            {
                sType = VkStructureType.PipelineViewportStateCreateInfo
            };
            pipelineViewportStateCreateInfo.viewportCount = 1;
            pipelineViewportStateCreateInfo.scissorCount = 1;
            pipelineViewportStateCreateInfo.flags = 0;
            pipelineCreateInfo.pViewportState = &pipelineViewportStateCreateInfo;

            this.multisampleState.ToNative(out VkPipelineMultisampleStateCreateInfo multisampleState);
            pipelineCreateInfo.pMultisampleState = &multisampleState;

            depthStencilState_.ToNative(out VkPipelineDepthStencilStateCreateInfo depthStencilState);
            pipelineCreateInfo.pDepthStencilState = &depthStencilState;

            ColorBlendState.ToNative(out var colorBlendState, renderPass.GetColorAttachmentCount(subPass));
            pipelineCreateInfo.pColorBlendState = &colorBlendState;

            VkPipelineDynamicStateCreateInfo dynamicStateCreateInfo;
            if (DynamicStates.HasValue)
            {
                DynamicStates.ToNative(out dynamicStateCreateInfo);
                pipelineCreateInfo.pDynamicState = &dynamicStateCreateInfo;
            }

            var handle = new Pipeline(Device.CreateGraphicsPipeline(ref pipelineCreateInfo));
            pipelines.TryAdd(vertexInput?.GetHashCode()??0, handle);
            return handle;
        }

        public unsafe Pipeline GetComputePipeline()
        {
            if (!IsComputeShader)
            {
                return null;
            }

            if (computeHandle != null)
            {
                return computeHandle;
            }
            
            var pipelineCreateInfo = new VkComputePipelineCreateInfo
            {
                sType = VkStructureType.ComputePipelineCreateInfo
            };
            pipelineCreateInfo.stage = GetComputeStageCreateInfo();
            pipelineCreateInfo.layout = PipelineLayout.handle;

            computeHandle = new Pipeline(Device.CreateComputePipeline(ref pipelineCreateInfo));
            return computeHandle;

        }

        protected override void Destroy(bool disposing)
        {
            foreach (var stage in ShaderModels)
            {
                stage?.Dispose();
            }

            foreach (var kvp in pipelines)
            {
                kvp.Value.Dispose();
            }

            pipelines.Clear();

            if (computeHandle != null)
            {
                computeHandle.Dispose();
                computeHandle = null;
            }

            PipelineLayout.Dispose();

            base.Destroy(disposing);
        }

    }
}
