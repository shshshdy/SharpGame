using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class ResourceLayout : IDisposable
    {
        public DescriptorSetLayout descriptorSetLayout;
        internal DescriptorResourceCounts descriptorResourceCounts;
        internal int numBindings;
        public ResourceLayout(params DescriptorSetLayoutBinding[] bindings)
        {
            descriptorSetLayout = Graphics.CreateDescriptorSetLayout(bindings);
            descriptorResourceCounts = new DescriptorResourceCounts();
            numBindings = bindings.Length;

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
