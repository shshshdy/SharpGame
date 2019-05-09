using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class ResourceLayout : IDisposable
    {
        internal DescriptorSetLayout descriptorSetLayout;
        internal DescriptorResourceCounts descriptorResourceCounts;
        internal DescriptorSetLayoutBinding[] bindings;
        internal int numBindings => bindings.Length;

        public ResourceLayout(params DescriptorSetLayoutBinding[] bindings)
        {
            descriptorSetLayout = Graphics.CreateDescriptorSetLayout(bindings);
            descriptorResourceCounts = new DescriptorResourceCounts();
            this.bindings = bindings;
            foreach (var binding in bindings)
            {
                descriptorResourceCounts[(int)binding.DescriptorType] += 1;                
            }

        }

        public void Dispose()
        {
            descriptorSetLayout?.Dispose();
        }
    }

    internal unsafe struct DescriptorResourceCounts
    {
        fixed int counts[11];

        public ref int this[int idx] { get=> ref counts[idx]; }
        
    }

}
