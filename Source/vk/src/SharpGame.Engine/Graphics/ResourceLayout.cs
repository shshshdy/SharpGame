using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class ResourceLayout : IDisposable
    {
        public VkDescriptorSetLayoutBinding[] bindings;

        public VkDescriptorSetLayout descriptorSetLayout;
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
