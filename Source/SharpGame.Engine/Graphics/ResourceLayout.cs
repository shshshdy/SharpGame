using System;
using System.Collections;
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

        public ResourceLayoutBinding(uint binding, DescriptorType type, ShaderStage stageFlags, uint descriptorCount = 1)
        {
            descriptorType = type;
            this.binding = binding;
            this.descriptorCount = descriptorCount;
            this.stageFlags = stageFlags;
            pImmutableSamplers = null;
        }
    }

    public class ResourceLayout : DisposeBase, IEnumerable<ResourceLayoutBinding>
    {
        public List<ResourceLayoutBinding> Bindings { get; set; }

        private VkDescriptorSetLayoutBinding[] bindings;

        internal VkDescriptorSetLayout descriptorSetLayout;
        internal DescriptorResourceCounts descriptorResourceCounts;
        internal int NumBindings => Bindings.Count;
        private bool needRebuild = true;

        public ResourceLayout()
        {
        }

        public ResourceLayout(params ResourceLayoutBinding[] bindings)
        {
            Bindings = new List<ResourceLayoutBinding>(bindings);
            Build();
        }
        
        protected override void Destroy()
        {
            if(descriptorSetLayout != 0)
            {
                VulkanNative.vkDestroyDescriptorSetLayout(Graphics.device, descriptorSetLayout, IntPtr.Zero);
            }
        }

        public unsafe ResourceLayout Build()
        {
            if(!needRebuild)
            {
                return this;
            }

            needRebuild = false;
            Destroy();
            bindings = new VkDescriptorSetLayoutBinding[Bindings.Count];
            for (int i = 0; i < Bindings.Count; i++)
            {
                bindings[i] = new VkDescriptorSetLayoutBinding
                {
                    descriptorType = (VkDescriptorType)Bindings[i].descriptorType,
                    stageFlags = (VkShaderStageFlags)Bindings[i].stageFlags,
                    binding = Bindings[i].binding,
                    descriptorCount = Bindings[i].descriptorCount
                };
            }

            var descriptorSetLayoutCreateInfo = VkDescriptorSetLayoutCreateInfo.New();
            descriptorSetLayoutCreateInfo.pBindings = (VkDescriptorSetLayoutBinding*)Utilities.AsPointer(ref bindings[0]);
            descriptorSetLayoutCreateInfo.bindingCount = (uint)bindings.Length;

            VulkanNative.vkCreateDescriptorSetLayout(Graphics.device, ref descriptorSetLayoutCreateInfo, IntPtr.Zero, out descriptorSetLayout);

            descriptorResourceCounts = new DescriptorResourceCounts();            
            foreach (var binding in bindings)
            {
                descriptorResourceCounts[(int)binding.descriptorType] += 1;
            }

            return this;
        }

        public IEnumerator<ResourceLayoutBinding> GetEnumerator()
        {
            return Bindings.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Bindings.GetEnumerator();
        }

        public void Add(ResourceLayoutBinding binding)
        {
            if(Bindings == null)
            {
                Bindings = new List<ResourceLayoutBinding>();
            }

            Bindings.Add(binding);
            needRebuild = true;
        }
    }

    internal unsafe struct DescriptorResourceCounts
    {
        fixed uint counts[11];

        public ref uint this[int idx] { get=> ref counts[idx]; }
        
    }

}
