using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;


namespace SharpGame
{
    public class DescriptorSetLayoutBinding
    {
        public string name;
        public uint binding;
        public VkDescriptorType descriptorType;
        public uint descriptorCount = 1;
        public VkShaderStageFlags stageFlags;
        public VkSampler[] pImmutableSamplers;

        public bool IsTexture => descriptorType == VkDescriptorType.CombinedImageSampler;

        public DescriptorSetLayoutBinding()
        {
        }

        public DescriptorSetLayoutBinding(uint binding, VkDescriptorType type, VkShaderStageFlags stageFlags, uint descriptorCount = 1)
        {
            descriptorType = type;
            this.binding = binding;
            this.descriptorCount = descriptorCount;
            this.stageFlags = stageFlags;
            pImmutableSamplers = null;
        }
    }

    public class DescriptorSetLayout : DisposeBase, IEnumerable<DescriptorSetLayoutBinding>
    {
        public int Set { get; set; }
        public List<DescriptorSetLayoutBinding> Bindings { get; set; } = new List<DescriptorSetLayoutBinding>();

        private VkDescriptorSetLayoutBinding[] bindings;

        private VkDescriptorSetLayout descriptorSetLayout;
        internal ref VkDescriptorSetLayout Handle
        {
            get
            {
                Build();
                return ref descriptorSetLayout;
            }
        }

        internal DescriptorResourceCounts descriptorResourceCounts;
        internal int NumBindings => Bindings.Count;
        private bool needRebuild = true;

        public DescriptorSetLayout(int set = 0)
        {
            Set = set;
        }

        public DescriptorSetLayout(params DescriptorSetLayoutBinding[] bindings)
        {
            Bindings = new List<DescriptorSetLayoutBinding>(bindings);
            Build();
        }
        
        public unsafe DescriptorSetLayout Build()
        {
            if(!needRebuild)
            {
                return this;
            }

            needRebuild = false;

            Destroy(true);

            bindings = new VkDescriptorSetLayoutBinding[Bindings.Count];
            for (int i = 0; i < Bindings.Count; i++)
            {
                bindings[i] = new VkDescriptorSetLayoutBinding
                {
                    descriptorType = Bindings[i].descriptorType,
                    stageFlags = Bindings[i].stageFlags,
                    binding = Bindings[i].binding,
                    descriptorCount = Bindings[i].descriptorCount
                };
            }

            var descriptorSetLayoutCreateInfo = new VkDescriptorSetLayoutCreateInfo
            {
                sType = VkStructureType.DescriptorSetLayoutCreateInfo
            };
            descriptorSetLayoutCreateInfo.pBindings = (VkDescriptorSetLayoutBinding*)Utilities.AsPointer(ref bindings[0]);
            descriptorSetLayoutCreateInfo.bindingCount = (uint)bindings.Length;

            descriptorSetLayout = Device.CreateDescriptorSetLayout(ref descriptorSetLayoutCreateInfo);

            descriptorResourceCounts = new DescriptorResourceCounts();            
            foreach (var binding in bindings)
            {
                descriptorResourceCounts[(int)binding.descriptorType] += 1;
            }
            
            return this;
        }

        public IEnumerator<DescriptorSetLayoutBinding> GetEnumerator()
        {
            return Bindings.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Bindings.GetEnumerator();
        }

        public bool Contains(uint bd)
        {
            foreach (var binding in Bindings)
            {
                if (binding.binding == bd)
                {
                    return true;
                }
            }

            return false;
        }

        public void Add(DescriptorSetLayoutBinding binding)
        {
            if(Bindings == null)
            {
                Bindings = new List<DescriptorSetLayoutBinding>();
            }

            Bindings.Add(binding);
            needRebuild = true;
        }

        public DescriptorSetLayoutBinding GetBinding(string name)
        {
            foreach(var binding in Bindings)
            {
                if(binding.name == name)
                {
                    return binding;
                }
            }

            return null;
        }

        protected override void Destroy(bool disposing)
        {
            if (descriptorSetLayout != 0)
            {
                Device.DestroyDescriptorSetLayout(descriptorSetLayout);
            }
        }

    }

    internal unsafe struct DescriptorResourceCounts
    {
        fixed uint counts[11];

        public ref uint this[int idx] { get=> ref counts[idx]; }
        
    }

}
