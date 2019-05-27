using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using global::System.Runtime.CompilerServices;
    using static VulkanNative;
    public class ComputePipeline : Resource
    {
        public ResourceLayout[] ResourceLayout { get; set; }

        private PushConstantRange[] pushConstantRanges;
        public PushConstantRange[] PushConstantRanges { get => pushConstantRanges; set => pushConstantRanges = value; }

        internal VkPipelineLayout pipelineLayout;
        internal VkPipeline handle;

        internal unsafe VkPipeline GetComputePipeline(Pass pass)
        {
            if(handle != 0)
            {
                return handle;
            }

            if (!pass.IsComputeShader)
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
            pipelineCreateInfo.stage = pass.GetComputeStageCreateInfo();
            pipelineCreateInfo.layout = pipelineLayout;

            handle = Device.CreateComputePipeline(ref pipelineCreateInfo);
            return handle;

        }

        protected override void Destroy()
        {
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
    }
}
