using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;


namespace SharpGame
{
    public struct RasterizationStateInfo
    {
        public uint flags;
        public bool depthClampEnable;
        public bool rasterizerDiscardEnable;
        public VkPolygonMode polygonMode;
        public VkCullModeFlags cullMode;
        public VkFrontFace frontFace;
        public bool depthBiasEnable;
        public float depthBiasConstantFactor;
        public float depthBiasClamp;
        public float depthBiasSlopeFactor;
        public float lineWidth;

        public static RasterizationStateInfo Default = new RasterizationStateInfo
        {
            polygonMode = VkPolygonMode.Fill,
            cullMode = VkCullModeFlags.Back,
            frontFace = VkFrontFace.CounterClockwise,
            depthClampEnable = false,
            lineWidth = 1.0f
        };

        public void ToNative(out VkPipelineRasterizationStateCreateInfo rasterizationStateCreateInfo)
        {
            rasterizationStateCreateInfo = new VkPipelineRasterizationStateCreateInfo
            {
                sType = VkStructureType.PipelineRasterizationStateCreateInfo
            };
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
        public VkPipelineMultisampleStateCreateFlags flags;
        public VkSampleCountFlags rasterizationSamples;
        public bool sampleShadingEnable;
        public float minSampleShading;
        public uint[] pSampleMask;
        public bool alphaToCoverageEnable;
        public bool alphaToOneEnable;

        public static MultisampleStateInfo Default = new MultisampleStateInfo
        {
            rasterizationSamples = VkSampleCountFlags.Count1,
            minSampleShading = 1.0f
        };

        public unsafe void ToNative(out VkPipelineMultisampleStateCreateInfo native)
        {
            native = new VkPipelineMultisampleStateCreateInfo();
            native.sType = VkStructureType.PipelineMultisampleStateCreateInfo;
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
        public VkStencilOp failOp;
        public VkStencilOp passOp;
        public VkStencilOp depthFailOp;
        public VkCompareOp compareOp;
        public uint compareMask;
        public uint writeMask;
        public uint reference;
    }

    public struct DepthStencilStateInfo
    {
        public VkPipelineDepthStencilStateCreateFlags flags;
        public bool depthTestEnable;
        public bool depthWriteEnable;
        public VkCompareOp depthCompareOp;
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
            depthCompareOp = VkCompareOp.LessOrEqual,
            back = new StencilOpState
            {
                failOp = VkStencilOp.Keep,
                passOp = VkStencilOp.Keep,
                compareOp = VkCompareOp.Always
            },
            front = new StencilOpState
            {
                failOp = VkStencilOp.Keep,
                passOp = VkStencilOp.Keep,
                compareOp = VkCompareOp.Always
            }

        };

        public unsafe void ToNative(out VkPipelineDepthStencilStateCreateInfo native)
        {
            native = new VkPipelineDepthStencilStateCreateInfo
            {
                sType = VkStructureType.PipelineDepthStencilStateCreateInfo
            };
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
        public VkBlendFactor srcColorBlendFactor;
        public VkBlendFactor dstColorBlendFactor;
        public VkBlendOp colorBlendOp;
        public VkBlendFactor srcAlphaBlendFactor;
        public VkBlendFactor dstAlphaBlendFactor;
        public VkBlendOp alphaBlendOp;
        public VkColorComponentFlags colorWriteMask;
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
                    srcColorBlendFactor = VkBlendFactor.One,
                    dstColorBlendFactor = VkBlendFactor.Zero,
                    colorBlendOp = VkBlendOp.Add,
                    srcAlphaBlendFactor = VkBlendFactor.One,
                    dstAlphaBlendFactor = VkBlendFactor.Zero,
                    alphaBlendOp = VkBlendOp.Add,
                    colorWriteMask = VkColorComponentFlags.All
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
                    srcColorBlendFactor = VkBlendFactor.One,
                    dstColorBlendFactor = VkBlendFactor.One,
                    colorBlendOp = VkBlendOp.Add,
                    srcAlphaBlendFactor = VkBlendFactor.SrcAlpha,
                    dstAlphaBlendFactor = VkBlendFactor.DstAlpha,
                    alphaBlendOp = VkBlendOp.Add,
                    colorWriteMask = VkColorComponentFlags.All
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
                    srcColorBlendFactor = VkBlendFactor.SrcAlpha,
                    dstColorBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                    colorBlendOp = VkBlendOp.Add,
                    srcAlphaBlendFactor = VkBlendFactor.SrcAlpha,
                    dstAlphaBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                    alphaBlendOp = VkBlendOp.Add,
                    colorWriteMask = VkColorComponentFlags.All
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
                    srcColorBlendFactor = VkBlendFactor.One,
                    dstColorBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                    colorBlendOp = VkBlendOp.Add,
                    srcAlphaBlendFactor = VkBlendFactor.One,
                    dstAlphaBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                    alphaBlendOp = VkBlendOp.Add,
                    colorWriteMask = VkColorComponentFlags.All
                }
            }
        };

        public unsafe void ToNative(out VkPipelineColorBlendStateCreateInfo native, uint attachmentCount)
        {
            native = new VkPipelineColorBlendStateCreateInfo();
            native.sType = VkStructureType.PipelineColorBlendStateCreateInfo;
            native.logicOpEnable = logicOpEnable;
            native.logicOp = logicOp;

            if(attachmentCount > attachments.Length)
            {
                Array.Resize(ref attachments, (int)attachmentCount);
                for(int i = 1; i < attachmentCount; i++)
                {
                    attachments[i] = attachments[0];
                }
            }

            native.attachmentCount = (uint)attachments.Length;
            native.pAttachments = (VkPipelineColorBlendAttachmentState*)Utilities.AsPointer(ref attachments[0]);
            native.blendConstants[0] = blendConstants_0;
            native.blendConstants[1] = blendConstants_1;
            native.blendConstants[2] = blendConstants_2;
            native.blendConstants[3] = blendConstants_3;
        }
    }

    public struct DynamicStateInfo
    {
        public VkPipelineDynamicStateCreateFlags flags;
        public VkDynamicState[] dynamicStates;
        public bool HasValue => !dynamicStates.IsNullOrEmpty();
       
        public DynamicStateInfo(params VkDynamicState[] dynamicStates)
        {
            this.dynamicStates = dynamicStates;
            this.flags = 0;
        }

        public bool HasState(VkDynamicState dynamicState)
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
            native = new VkPipelineDynamicStateCreateInfo
            {
                sType = VkStructureType.PipelineDynamicStateCreateInfo
            };
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
