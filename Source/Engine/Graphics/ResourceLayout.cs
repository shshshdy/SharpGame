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

        public ResourceLayout(params DescriptorSetLayoutBinding[] bindings)
        {
            descriptorSetLayout = Graphics.CreateDescriptorSetLayout(bindings);
            descriptorResourceCounts = new DescriptorResourceCounts();

            foreach(var binding in bindings)
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
        public fixed int Count[11];

        public ref int this[int idx] { get=> ref Count[idx]; }

        /*
        public readonly int UniformBufferCount;
        public readonly int SampledImageCount;
        public readonly int SamplerCount;
        public readonly int StorageBufferCount;
        public readonly int StorageImageCount;

        public DescriptorResourceCounts(
            int uniformBufferCount,
            int sampledImageCount,
            int samplerCount,
            int storageBufferCount,
            int storageImageCount)
        {
            UniformBufferCount = uniformBufferCount;
            SampledImageCount = sampledImageCount;
            SamplerCount = samplerCount;
            StorageBufferCount = storageBufferCount;
            StorageImageCount = storageImageCount;
        }*/
    }

}
