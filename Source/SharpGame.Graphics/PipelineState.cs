using System;
using System.Collections;
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
                sType = VkStructureType.PipelineRasterizationStateCreateInfo,
                depthClampEnable = depthClampEnable,
                rasterizerDiscardEnable = rasterizerDiscardEnable,
                polygonMode = (VkPolygonMode)polygonMode,
                cullMode = (VkCullModeFlags)cullMode,
                frontFace = (VkFrontFace)frontFace,
                depthBiasEnable = depthBiasEnable,
                depthBiasConstantFactor = depthBiasConstantFactor,
                depthBiasClamp = depthBiasClamp,
                depthBiasSlopeFactor = depthBiasSlopeFactor,
                lineWidth = lineWidth
            };
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
            native = new VkPipelineMultisampleStateCreateInfo
            {
                sType = VkStructureType.PipelineMultisampleStateCreateInfo,
                flags = flags,
                rasterizationSamples = (VkSampleCountFlags)rasterizationSamples,
                sampleShadingEnable = sampleShadingEnable,
                minSampleShading = minSampleShading,
                alphaToCoverageEnable = alphaToCoverageEnable,
                alphaToOneEnable = alphaToOneEnable,
            };

            if (pSampleMask != null && pSampleMask.Length > 0)
            {
                native.pSampleMask = (uint*)Utilities.AsPointer(ref pSampleMask[0]);
            }

        }
    }

    public struct DepthStencilStateInfo
    {
        public VkPipelineDepthStencilStateCreateFlags flags;
        public bool depthTestEnable;
        public bool depthWriteEnable;
        public VkCompareOp depthCompareOp;
        public bool depthBoundsTestEnable;
        public bool stencilTestEnable;
        public VkStencilOpState front;
        public VkStencilOpState back;
        public float minDepthBounds;
        public float maxDepthBounds;

        public static DepthStencilStateInfo Solid = new DepthStencilStateInfo
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

        public unsafe void ToNative(out VkPipelineDepthStencilStateCreateInfo native)
        {
            native = new VkPipelineDepthStencilStateCreateInfo
            {
                sType = VkStructureType.PipelineDepthStencilStateCreateInfo,
                flags = flags,
                depthTestEnable = depthTestEnable,
                depthWriteEnable = depthWriteEnable,
                depthCompareOp = (VkCompareOp)depthCompareOp,
                depthBoundsTestEnable = depthBoundsTestEnable,
                stencilTestEnable = stencilTestEnable,
                front = *(VkStencilOpState*)Unsafe.AsPointer(ref front),
                back = *(VkStencilOpState*)Unsafe.AsPointer(ref back),
                minDepthBounds = minDepthBounds,
                maxDepthBounds = maxDepthBounds
            };
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

    public struct ColorBlendStateInfo : IEnumerable
    {
        public uint flags;
        public VkBool32 logicOpEnable;
        public VkLogicOp logicOp;
        public Vector<ColorBlendAttachment> attachments;
        public float blendConstants_0;
        public float blendConstants_1;
        public float blendConstants_2;
        public float blendConstants_3;

        public void Add(in ColorBlendAttachment colorBlendAttachment)
        {
            if(attachments == null)
                attachments = new Vector<ColorBlendAttachment>();
            
            attachments.Add(colorBlendAttachment);
        }

        public static ColorBlendStateInfo Replace = new ColorBlendStateInfo
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
            
        };

        public static ColorBlendStateInfo Addtive = new ColorBlendStateInfo
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

        };

        public static ColorBlendStateInfo AlphaBlend = new ColorBlendStateInfo
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

        };

        public static ColorBlendStateInfo PremulAlpha = new ColorBlendStateInfo
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
           
        };

        public unsafe void ToNative(out VkPipelineColorBlendStateCreateInfo native, uint attachmentCount)
        {
            native = new VkPipelineColorBlendStateCreateInfo
            {
                sType = VkStructureType.PipelineColorBlendStateCreateInfo,
                logicOpEnable = logicOpEnable,
                logicOp = logicOp
            };

            while (attachmentCount > attachments.Count)
            {
                attachments.Add(attachments.Back());
            }

            native.attachmentCount = (uint)attachments.Count;
            native.pAttachments = (VkPipelineColorBlendAttachmentState*)attachments.DataPtr;
            native.blendConstants[0] = blendConstants_0;
            native.blendConstants[1] = blendConstants_1;
            native.blendConstants[2] = blendConstants_2;
            native.blendConstants[3] = blendConstants_3;
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)attachments).GetEnumerator();
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
