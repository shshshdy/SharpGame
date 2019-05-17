using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public enum DescriptorType
    {
        Sampler = 0,
        CombinedImageSampler = 1,
        SampledImage = 2,
        StorageImage = 3,
        UniformTexelBuffer = 4,
        StorageTexelBuffer = 5,
        UniformBuffer = 6,
        StorageBuffer = 7,
        UniformBufferDynamic = 8,
        StorageBufferDynamic = 9,
        InputAttachment = 10
    }

    public struct ResourceLayoutBinding
    {
        public uint binding;
        public DescriptorType descriptorType;
        public uint descriptorCount;
        public ShaderStage stageFlags;
        public VkSampler[] pImmutableSamplers;

        public ResourceLayoutBinding(DescriptorType type, ShaderStage stageFlags, uint binding, uint descriptorCount = 1)
        {
            descriptorType = type;
            this.binding = binding;
            this.descriptorCount = descriptorCount;
            this.stageFlags = stageFlags;
            pImmutableSamplers = null;
        }
    }

    public class ResourceLayout : IDisposable
    {
        internal VkDescriptorSetLayoutBinding[] bindings;

        public VkDescriptorSetLayout descriptorSetLayout;
        internal DescriptorResourceCounts descriptorResourceCounts;
        internal int numBindings => bindings.Length;

        public ResourceLayout()
        {
        }

        public ResourceLayout(params ResourceLayoutBinding[] bindings)
        {
            this.bindings = new VkDescriptorSetLayoutBinding[bindings.Length];
            for(int i = 0; i < bindings.Length; i++)
            {
                this.bindings[i] = new VkDescriptorSetLayoutBinding
                {
                    descriptorType = (VkDescriptorType)bindings[i].descriptorType,
                    stageFlags = (VkShaderStageFlags)bindings[i].stageFlags,
                    binding = bindings[i].binding,
                    descriptorCount = bindings[i].descriptorCount
                };
            }
            Build();
        }

        public ResourceLayout(params VkDescriptorSetLayoutBinding[] bindings)
        {
            this.bindings = bindings;
            Build();
        }

        public void Dispose()
        {
            VulkanNative.vkDestroyDescriptorSetLayout(Graphics.device, descriptorSetLayout, IntPtr.Zero);
        }

        public void Build()
        {
            var descriptorSetLayoutCreateInfo = Builder.DescriptorSetLayoutCreateInfo(bindings);
            VulkanNative.vkCreateDescriptorSetLayout(Graphics.device, ref descriptorSetLayoutCreateInfo, IntPtr.Zero, out descriptorSetLayout);

            descriptorResourceCounts = new DescriptorResourceCounts();            
            foreach (var binding in bindings)
            {
                descriptorResourceCounts[(int)binding.descriptorType] += 1;
            }
        }
    }

    internal unsafe struct DescriptorResourceCounts
    {
        fixed uint counts[11];

        public ref uint this[int idx] { get=> ref counts[idx]; }
        
    }

}
