using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class ResourceLayout : IDisposable
    {
        public VkDescriptorSetLayoutBinding[] bindings;

        internal VkDescriptorSetLayout descriptorSetLayout;
        internal DescriptorResourceCounts descriptorResourceCounts;
        internal int numBindings => bindings.Length;

        public ResourceLayout()
        {
        }

        public ResourceLayout(params VkDescriptorSetLayoutBinding[] bindings)
        {
            this.bindings = bindings;
            Build();
        }

        public void Dispose()
        {
         //todo   descriptorSetLayout?.Dispose();
        }

        public void Build()
        {/*
            descriptorSetLayout = Graphics.CreateDescriptorSetLayout(bindings);
            descriptorResourceCounts = new DescriptorResourceCounts();
            
            foreach (var binding in bindings)
            {
                descriptorResourceCounts[(int)binding.descriptorType] += 1;
            }*/
        }
    }

    internal unsafe struct DescriptorResourceCounts
    {
        fixed uint counts[11];

        public ref uint this[int idx] { get=> ref counts[idx]; }
        
    }

}
