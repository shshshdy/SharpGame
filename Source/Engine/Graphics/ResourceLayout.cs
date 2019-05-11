using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class ResourceLayout : IDisposable
    {
        public DescriptorSetLayoutBinding[] bindings;

        internal DescriptorSetLayout descriptorSetLayout;
        internal DescriptorResourceCounts descriptorResourceCounts;
        internal int numBindings => bindings.Length;

        public ResourceLayout()
        {
        }

        public ResourceLayout(params DescriptorSetLayoutBinding[] bindings)
        {
            this.bindings = bindings;
            Build();
        }

        public void Dispose()
        {
            descriptorSetLayout?.Dispose();
        }

        public void Build()
        {
            descriptorSetLayout = Graphics.CreateDescriptorSetLayout(bindings);
            descriptorResourceCounts = new DescriptorResourceCounts();
            
            foreach (var binding in bindings)
            {
                descriptorResourceCounts[(int)binding.DescriptorType] += 1;
            }
        }
    }

    internal unsafe struct DescriptorResourceCounts
    {
        fixed int counts[11];

        public ref int this[int idx] { get=> ref counts[idx]; }
        
    }

}
