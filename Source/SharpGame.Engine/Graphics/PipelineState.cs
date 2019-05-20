using System;
using System.Collections.Generic;
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
        public VkSampleCountFlags rasterizationSamples;
        public VkBool32 sampleShadingEnable;
        public float minSampleShading;
        public uint[] pSampleMask;
        public VkBool32 alphaToCoverageEnable;
        public VkBool32 alphaToOneEnable;

        public unsafe void ToNative(out VkPipelineMultisampleStateCreateInfo native)
        {
            native = VkPipelineMultisampleStateCreateInfo.New();
            native.flags = flags;
            native.rasterizationSamples = rasterizationSamples;
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

    public struct DepthStencilStateInfo
    {
        public uint flags;
        public VkBool32 depthTestEnable;
        public VkBool32 depthWriteEnable;
        public VkCompareOp depthCompareOp;
        public VkBool32 depthBoundsTestEnable;
        public VkBool32 stencilTestEnable;
        public VkStencilOpState front;
        public VkStencilOpState back;
        public float minDepthBounds;
        public float maxDepthBounds;

        public void ToNative(out VkPipelineDepthStencilStateCreateInfo native)
        {
            native = VkPipelineDepthStencilStateCreateInfo.New();
            native.flags = flags;
            native.depthTestEnable = depthTestEnable;
            native.depthWriteEnable = depthWriteEnable;
            native.depthCompareOp = depthCompareOp;
            native.depthBoundsTestEnable = depthBoundsTestEnable;
            native.stencilTestEnable = stencilTestEnable;
            native.front = front;
            native.back = back;
            native.minDepthBounds = minDepthBounds;
            native.maxDepthBounds = maxDepthBounds;
        }
    }

    public struct ColorBlendAttachment
    {
        public VkBool32 blendEnable;
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


    public enum DynamicState
    {
        Viewport = 0,
        Scissor = 1,
        LineWidth = 2,
        DepthBias = 3,
        BlendConstants = 4,
        DepthBounds = 5,
        StencilCompareMask = 6,
        StencilWriteMask = 7,
        StencilReference = 8,
        ViewportWScalingNV = 1000087000,
        DiscardRectangleEXT = 1000099000,
        SampleLocationsEXT = 1000143000
    }

    public struct DynamicStateInfo
    {
        public uint flags;
        public DynamicState[] dynamicStates;

        public DynamicStateInfo(params DynamicState[] dynamicStates)
        {
            this.dynamicStates = dynamicStates;
            this.flags = 0;
        }

        public unsafe void ToNative(out VkPipelineDynamicStateCreateInfo dynamicStateCreateInfo)
        {
            dynamicStateCreateInfo = VkPipelineDynamicStateCreateInfo.New();
            dynamicStateCreateInfo.flags = this.flags;
            dynamicStateCreateInfo.dynamicStateCount = (uint)dynamicStates.Length;
            dynamicStateCreateInfo.pDynamicStates = (VkDynamicState*)Utilities.AsPointer(ref dynamicStates[0]);
        }

    }
}
