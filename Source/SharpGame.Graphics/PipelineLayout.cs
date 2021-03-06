﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;


namespace SharpGame
{
    public struct PushConstantRange
    {
        public VkShaderStageFlags stageFlags;
        public int offset;
        public int size;
        public PushConstantRange(VkShaderStageFlags shaderStage, int offset, int size)
        {
            this.stageFlags = shaderStage;
            this.offset = offset;
            this.size = size;
        }
    }

    public class PipelineLayout : HandleBase<VkPipelineLayout>
    {
        public DescriptorSetLayout[] ResourceLayout { get; set; }

        public PushConstantRange[] PushConstant { get => pushConstant; set { SetPushConstants(value); } }
        private PushConstantRange[] pushConstant;

        Vector<PushConstantRange> combindePushConstant;

        public List<string> PushConstantNames { get; set; }

        public PipelineLayout()
        {
        }

        public PipelineLayout(params DescriptorSetLayout[] resourceLayouts)
        {
            ResourceLayout = resourceLayouts;

            Build();
        }

        public DescriptorSetLayoutBinding GetBinding(string name)
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
            if(combindePushConstant == null)
                combindePushConstant = new Vector<PushConstantRange>();
            foreach (var c in pushConstant)
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
            VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo = new VkPipelineLayoutCreateInfo
            {
                sType = VkStructureType.PipelineLayoutCreateInfo
            };

            if (!ResourceLayout.IsNullOrEmpty())
            {
                foreach(var resLayout in ResourceLayout)
                {
                    resLayout.Build();
                }

                VkDescriptorSetLayout* pSetLayouts = stackalloc VkDescriptorSetLayout[ResourceLayout.Length];

                for (int i = 0; i < ResourceLayout.Length; i++)
                {
                    pSetLayouts[i] = ResourceLayout[i].Handle;
                }

                pipelineLayoutCreateInfo.setLayoutCount = (uint)ResourceLayout.Length;
                pipelineLayoutCreateInfo.pSetLayouts = pSetLayouts;
            }

            if (!pushConstant.IsNullOrEmpty())
            {
                pipelineLayoutCreateInfo.pushConstantRangeCount = combindePushConstant.Count;
                pipelineLayoutCreateInfo.pPushConstantRanges = (VkPushConstantRange*)Unsafe.AsPointer(ref combindePushConstant[0]);
            }

            handle = Device.CreatePipelineLayout(ref pipelineLayoutCreateInfo);
        }

    }
}
