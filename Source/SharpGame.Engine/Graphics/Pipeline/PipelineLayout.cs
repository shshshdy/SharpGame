using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
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

    public class PipelineLayout : DisposeBase
    {
        public ResourceLayout[] ResourceLayout { get; set; }

        public PushConstantRange[] PushConstant { get => pushConstant; set => pushConstant = value; }

        private PushConstantRange[] pushConstant;

        internal VkPipelineLayout handle;

        public unsafe void Build()
        {            
            VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo = VkPipelineLayoutCreateInfo.New();
            if (!ResourceLayout.IsNullOrEmpty())
            {
                foreach(var resLayout in ResourceLayout)
                {
                    resLayout.Build();
                }

                VkDescriptorSetLayout* pSetLayouts = stackalloc VkDescriptorSetLayout[ResourceLayout.Length];

                for (int i = 0; i < ResourceLayout.Length; i++)
                {
                    pSetLayouts[i] = ResourceLayout[i].DescriptorSetLayout;
                }

                pipelineLayoutCreateInfo.setLayoutCount = (uint)ResourceLayout.Length;
                pipelineLayoutCreateInfo.pSetLayouts = pSetLayouts;
            }

            if (!pushConstant.IsNullOrEmpty())
            {
                pipelineLayoutCreateInfo.pushConstantRangeCount = (uint)pushConstant.Length;
                pipelineLayoutCreateInfo.pPushConstantRanges = (VkPushConstantRange*)Unsafe.AsPointer(ref pushConstant[0]);
            }

            vkCreatePipelineLayout(Graphics.device, ref pipelineLayoutCreateInfo, IntPtr.Zero, out handle);
        }

        protected override void Destroy()
        {
            if(handle != 0)
            {
                Device.DestroyPipelineLayout(handle);
                handle = 0;
            }
        }


    }
}
