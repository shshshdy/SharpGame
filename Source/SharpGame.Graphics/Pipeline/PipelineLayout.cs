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

        public PushConstantRange[] PushConstant { get => pushConstant; set { SetPushConstants(value); } }
        private PushConstantRange[] pushConstant;

        private NativeList<PushConstantRange> combindePushConstant = new NativeList<PushConstantRange>();
        public List<string> PushConstantNames { get; set; }

        public DefaultResourcSet DefaultResourcSet { get; set;}

        internal VkPipelineLayout handle;

        public PipelineLayout()
        {
        }

        public PipelineLayout(params ResourceLayout[] resourceLayouts)
        {
            ResourceLayout = resourceLayouts;

            Build();
        }


        public ResourceLayoutBinding GetBinding(string name)
        {
            foreach(var layout in ResourceLayout)
            {
                foreach(var binding in layout.Bindings)
                {
                    if(binding.name == name)
                    {
                        return binding;
                    }
                }
            }

            return null;
        }

        void SetPushConstants(PushConstantRange[] pushConstant)
        {
            this.pushConstant = pushConstant;
            foreach(var c in pushConstant)
            {
                bool found = false;
                for(int i = 0; i < combindePushConstant.Count; i++)
                {
                    ref PushConstantRange c1 = ref combindePushConstant[i]; 
                    if (c1.stageFlags == c.stageFlags)
                    {
                        c1.offset = glm.min(c1.offset, c.offset);
                        if(c.offset + c.size > c1.offset  + c1.size)
                        {
                            c1.size = c.offset + c.size - c1.offset;
                        }

                        found = true;
                        break;

                    }
                    
                }

                if(!found)
                combindePushConstant.Add(c);

            }
        }

        public unsafe void Build()
        {
            VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo = VkPipelineLayoutCreateInfo.New();
            if (!ResourceLayout.IsNullOrEmpty())
            {
                foreach(var resLayout in ResourceLayout)
                {
                    resLayout.Build();
                    DefaultResourcSet |= resLayout.DefaultResourcSet;
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
                pipelineLayoutCreateInfo.pushConstantRangeCount = (uint)combindePushConstant.Count;
                pipelineLayoutCreateInfo.pPushConstantRanges = (VkPushConstantRange*)Unsafe.AsPointer(ref combindePushConstant[0]);
            }

            handle = Device.CreatePipelineLayout(ref pipelineLayoutCreateInfo);
        }

        protected override void Destroy(bool disposing)
        {
            if(handle != 0)
            {
                Device.DestroyPipelineLayout(handle);
                handle = 0;
            }
        }


    }
}
