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

        public ResourceLayout(params DescriptorSetLayoutBinding[] bindings)
        {
            descriptorSetLayout = Graphics.CreateDescriptorSetLayout(bindings);

            int uniformBufferCount = 0;
            int sampledImageCount = 0;
            int samplerCount = 0;
            int storageBufferCount = 0;
            int storageImageCount = 0;

            foreach(var binding in bindings)
            {                
                switch (binding.DescriptorType)
                {
                    case DescriptorType.Sampler:
                        samplerCount += 1;
                        break;
                    case DescriptorType.SampledImage:
                        sampledImageCount += 1;
                        break;
                    case DescriptorType.StorageImage:
                        storageImageCount += 1;
                        break;
                    case DescriptorType.UniformBuffer:
                        uniformBufferCount += 1;
                        break;
                    case DescriptorType.StorageBuffer:
                        storageBufferCount += 1;
                        break;
                }
            }

            descriptorResourceCounts = new DescriptorResourceCounts(
                uniformBufferCount,
                sampledImageCount,
                samplerCount,
                storageBufferCount,
                storageImageCount);
        }

        public void Dispose()
        {
            descriptorSetLayout?.Dispose();
        }
    }

    internal struct DescriptorResourceCounts
    {
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
        }
    }

}
