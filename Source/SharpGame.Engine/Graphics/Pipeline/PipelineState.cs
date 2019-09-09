using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public struct RasterizationStateInfo
    {
        public uint flags;
        public bool depthClampEnable;
        public bool rasterizerDiscardEnable;
        public PolygonMode polygonMode;
        public CullMode cullMode;
        public FrontFace frontFace;
        public bool depthBiasEnable;
        public float depthBiasConstantFactor;
        public float depthBiasClamp;
        public float depthBiasSlopeFactor;
        public float lineWidth;

        public static RasterizationStateInfo Default = new RasterizationStateInfo
        {
            polygonMode = PolygonMode.Fill,
            cullMode = CullMode.Back,
            frontFace = FrontFace.CounterClockwise,
            depthClampEnable = true,
            lineWidth = 1.0f
        };

        public void ToNative(out VkPipelineRasterizationStateCreateInfo rasterizationStateCreateInfo)
        {
            rasterizationStateCreateInfo = VkPipelineRasterizationStateCreateInfo.New();
            rasterizationStateCreateInfo.depthClampEnable = depthClampEnable;
            rasterizationStateCreateInfo.rasterizerDiscardEnable = rasterizerDiscardEnable;
            rasterizationStateCreateInfo.polygonMode = (VkPolygonMode)polygonMode;
            rasterizationStateCreateInfo.cullMode = (VkCullModeFlags)cullMode;
            rasterizationStateCreateInfo.frontFace = (VkFrontFace)frontFace;
            rasterizationStateCreateInfo.depthBiasEnable = depthBiasEnable;
            rasterizationStateCreateInfo.depthBiasConstantFactor = depthBiasConstantFactor;
            rasterizationStateCreateInfo.depthBiasClamp = depthBiasClamp;
            rasterizationStateCreateInfo.depthBiasSlopeFactor = depthBiasSlopeFactor;
            rasterizationStateCreateInfo.lineWidth = lineWidth;
        }
    }

    public struct MultisampleStateInfo
    {
        public uint flags;
        public SampleCountFlags rasterizationSamples;
        public bool sampleShadingEnable;
        public float minSampleShading;
        public uint[] pSampleMask;
        public bool alphaToCoverageEnable;
        public bool alphaToOneEnable;

        public static MultisampleStateInfo Default = new MultisampleStateInfo
        {
            rasterizationSamples = SampleCountFlags.Count1,
            minSampleShading = 1.0f
        };

        public unsafe void ToNative(out VkPipelineMultisampleStateCreateInfo native)
        {
            native = VkPipelineMultisampleStateCreateInfo.New();
            native.flags = flags;
            native.rasterizationSamples = (VkSampleCountFlags)rasterizationSamples;
            native.sampleShadingEnable = sampleShadingEnable;
            native.minSampleShading = minSampleShading;

            if (pSampleMask != null && pSampleMask.Length > 0)
            {
                native.pSampleMask = (uint*)Utilities.AsPointer(ref pSampleMask[0]);
            }

            native.alphaToCoverageEnable = alphaToCoverageEnable;
            native.alphaToOneEnable = alphaToOneEnable;
        }
    }

    public struct StencilOpState
    {
        public StencilOp failOp;
        public StencilOp passOp;
        public StencilOp depthFailOp;
        public CompareOp compareOp;
        public uint compareMask;
        public uint writeMask;
        public uint reference;
    }

    public struct DepthStencilStateInfo
    {
        public uint flags;
        public bool depthTestEnable;
        public bool depthWriteEnable;
        public CompareOp depthCompareOp;
        public bool depthBoundsTestEnable;
        public bool stencilTestEnable;
        public StencilOpState front;
        public StencilOpState back;
        public float minDepthBounds;
        public float maxDepthBounds;

        public static DepthStencilStateInfo Solid = new DepthStencilStateInfo
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

        public unsafe void ToNative(out VkPipelineDepthStencilStateCreateInfo native)
        {
            native = VkPipelineDepthStencilStateCreateInfo.New();
            native.flags = flags;
            native.depthTestEnable = depthTestEnable;
            native.depthWriteEnable = depthWriteEnable;
            native.depthCompareOp = (VkCompareOp)depthCompareOp;
            native.depthBoundsTestEnable = depthBoundsTestEnable;
            native.stencilTestEnable = stencilTestEnable;
            native.front = *(VkStencilOpState*)Unsafe.AsPointer(ref front);
            native.back = *(VkStencilOpState*)Unsafe.AsPointer(ref back);
            native.minDepthBounds = minDepthBounds;
            native.maxDepthBounds = maxDepthBounds;
        }
    }

    public struct ColorBlendAttachment
    {
        public bool blendEnable;
        public BlendFactor srcColorBlendFactor;
        public BlendFactor dstColorBlendFactor;
        public BlendOp colorBlendOp;
        public BlendFactor srcAlphaBlendFactor;
        public BlendFactor dstAlphaBlendFactor;
        public BlendOp alphaBlendOp;
        public ColorComponentFlags colorWriteMask;
    }

    public struct ColorBlendStateInfo
    {
        public uint flags;
        public VkBool32 logicOpEnable;
        public VkLogicOp logicOp;
        public uint attachmentCount;
        public ColorBlendAttachment[] attachments;
        public float blendConstants_0;
        public float blendConstants_1;
        public float blendConstants_2;
        public float blendConstants_3;

        public static ColorBlendStateInfo Replace = new ColorBlendStateInfo
        {
            attachments = new[]
            {
                new ColorBlendAttachment
                {
                    blendEnable = false,
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

        public static ColorBlendStateInfo Add = new ColorBlendStateInfo
        {
            attachments = new[]
            {
                new ColorBlendAttachment
                {
                    blendEnable = true,
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

        public static ColorBlendStateInfo AlphaBlend = new ColorBlendStateInfo
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

        public static ColorBlendStateInfo PremulAlpha = new ColorBlendStateInfo
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

        public unsafe void ToNative(out VkPipelineColorBlendStateCreateInfo native)
        {
            native = VkPipelineColorBlendStateCreateInfo.New();
            native.logicOpEnable = logicOpEnable;
            native.logicOp = logicOp;
            native.attachmentCount = (uint)attachments.Length;
            native.pAttachments = (VkPipelineColorBlendAttachmentState*)Utilities.AsPointer(ref attachments[0]);
            native.blendConstants_0 = blendConstants_0;
            native.blendConstants_1 = blendConstants_1;
            native.blendConstants_2 = blendConstants_2;
            native.blendConstants_3 = blendConstants_3;
        }
    }

    public struct DynamicStateInfo
    {
        public uint flags;
        public DynamicState[] dynamicStates;
        public bool HasValue => !dynamicStates.IsNullOrEmpty();
       
        public DynamicStateInfo(params DynamicState[] dynamicStates)
        {
            this.dynamicStates = dynamicStates;
            this.flags = 0;
        }

        public bool HasState(DynamicState dynamicState)
        {
            foreach(var ds in dynamicStates)
            {
                if(ds == dynamicState)
                {
                    return true;
                }
            }
            return false;
        }

        public unsafe void ToNative(out VkPipelineDynamicStateCreateInfo native)
        {
            native = VkPipelineDynamicStateCreateInfo.New();
            native.flags = this.flags;
            if(HasValue)
            {
                native.dynamicStateCount = (uint)dynamicStates.Length;
                native.pDynamicStates = (VkDynamicState*)Utilities.AsPointer(ref dynamicStates[0]);
            }
            else
            {
                native.dynamicStateCount = 0;
                native.pDynamicStates = null;
            }
        }

    }
}
