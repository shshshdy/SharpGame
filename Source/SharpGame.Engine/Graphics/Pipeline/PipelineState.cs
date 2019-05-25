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

    public enum SampleCountFlags
    {
        None = 0,
        Count1 = 1,
        Count2 = 2,
        Count4 = 4,
        Count8 = 8,
        Count16 = 16,
        Count32 = 32,
        Count64 = 64
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

    public enum CompareOp
    {
        Never = 0,
        Less = 1,
        Equal = 2,
        LessOrEqual = 3,
        Greater = 4,
        NotEqual = 5,
        GreaterOrEqual = 6,
        Always = 7
    }

    public enum StencilOp
    {
        Keep = 0,
        Zero = 1,
        Replace = 2,
        IncrementAndClamp = 3,
        DecrementAndClamp = 4,
        Invert = 5,
        IncrementAndWrap = 6,
        DecrementAndWrap = 7
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

    public enum BlendFactor
    {
        Zero = 0,
        One = 1,
        SrcColor = 2,
        OneMinusSrcColor = 3,
        DstColor = 4,
        OneMinusDstColor = 5,
        SrcAlpha = 6,
        OneMinusSrcAlpha = 7,
        DstAlpha = 8,
        OneMinusDstAlpha = 9,
        ConstantColor = 10,
        OneMinusConstantColor = 11,
        ConstantAlpha = 12,
        OneMinusConstantAlpha = 13,
        SrcAlphaSaturate = 14,
        Src1Color = 15,
        OneMinusSrc1Color = 16,
        Src1Alpha = 17,
        OneMinusSrc1Alpha = 18
    }

    public enum BlendOp
    {
        Add = 0,
        Subtract = 1,
        ReverseSubtract = 2,
        Min = 3,
        Max = 4,
        ZeroEXT = 1000148000,
        SrcEXT = 1000148001,
        DstEXT = 1000148002,
        SrcOverEXT = 1000148003,
        DstOverEXT = 1000148004,
        SrcInEXT = 1000148005,
        DstInEXT = 1000148006,
        SrcOutEXT = 1000148007,
        DstOutEXT = 1000148008,
        SrcAtopEXT = 1000148009,
        DstAtopEXT = 1000148010,
        XorEXT = 1000148011,
        MultiplyEXT = 1000148012,
        ScreenEXT = 1000148013,
        OverlayEXT = 1000148014,
        DarkenEXT = 1000148015,
        LightenEXT = 1000148016,
        ColordodgeEXT = 1000148017,
        ColorburnEXT = 1000148018,
        HardlightEXT = 1000148019,
        SoftlightEXT = 1000148020,
        DifferenceEXT = 1000148021,
        ExclusionEXT = 1000148022,
        InvertEXT = 1000148023,
        InvertRgbEXT = 1000148024,
        LineardodgeEXT = 1000148025,
        LinearburnEXT = 1000148026,
        VividlightEXT = 1000148027,
        LinearlightEXT = 1000148028,
        PinlightEXT = 1000148029,
        HardmixEXT = 1000148030,
        HslHueEXT = 1000148031,
        HslSaturationEXT = 1000148032,
        HslColorEXT = 1000148033,
        HslLuminosityEXT = 1000148034,
        PlusEXT = 1000148035,
        PlusClampedEXT = 1000148036,
        PlusClampedAlphaEXT = 1000148037,
        PlusDarkerEXT = 1000148038,
        MinusEXT = 1000148039,
        MinusClampedEXT = 1000148040,
        ContrastEXT = 1000148041,
        InvertOvgEXT = 1000148042,
        RedEXT = 1000148043,
        GreenEXT = 1000148044,
        BlueEXT = 1000148045
    }

    public enum ColorComponentFlags
    {
        None = 0,
        R = 1,
        G = 2,
        B = 4,
        A = 8,
        All = 0xf
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
