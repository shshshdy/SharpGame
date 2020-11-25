using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;


namespace SharpGame
{
    using static Vulkan;

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

    public class PipelineLayout : DisposeBase
    {
        public DescriptorSetLayout[] ResourceLayout { get; set; }

        public PushConstantRange[] PushConstant { get => pushConstant; set { SetPushConstants(value); } }
        private PushConstantRange[] pushConstant;

        private Vector<PushConstantRange> combindePushConstant = new Vector<PushConstantRange>();
        public List<string> PushConstantNames { get; set; }

        public DefaultResourcSet DefaultResourcSet { get; set;}

        internal VkPipelineLayout handle;

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
            VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo = new VkPipelineLayoutCreateInfo
            {
                sType = VkStructureType.PipelineLayoutCreateInfo
            };
            if (!ResourceLayout.IsNullOrEmpty())
            {
                foreach(var resLayout in ResourceLayout)
                {
                    resLayout.Build();
                    //DefaultResourcSet |= resLayout.DefaultResourcSet;
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
