using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;


namespace SharpGame
{
    using System.Runtime.CompilerServices;
    using static Builder;

    public class ResourceSet : IDisposable
    {
        public VkDescriptorSet descriptorSet;

        internal VkDescriptorPool descriptorPool;
        internal ResourceLayout resourceLayout;
        internal ref DescriptorResourceCounts Counts => ref resourceLayout.descriptorResourceCounts;

        private VkWriteDescriptorSet[] writeDescriptorSets;
        public ResourceSet(ResourceLayout resLayout)
        {
            resLayout.Build();
            VkDescriptorPool pool = Graphics.DescriptorPoolManager.Allocate(resLayout);
            unsafe
            {
                var dsAI = DescriptorSetAllocateInfo(pool, (VkDescriptorSetLayout*)Unsafe.AsPointer(ref resLayout.descriptorSetLayout), 1);
                VulkanNative.vkAllocateDescriptorSets(Graphics.device, ref dsAI, out descriptorSet);
                descriptorPool = pool;
                resourceLayout = resLayout;
            }

            writeDescriptorSets = new VkWriteDescriptorSet[resLayout.numBindings];
        }

        public ResourceSet(ResourceLayout resLayout, params IBindable[] bindables)
        {
            resLayout.Build();
            VkDescriptorPool pool = Graphics.DescriptorPoolManager.Allocate(resLayout);
            unsafe
            {
                var dsAI = DescriptorSetAllocateInfo(pool, (VkDescriptorSetLayout*)Unsafe.AsPointer(ref resLayout.descriptorSetLayout), 1);
                VulkanNative.vkAllocateDescriptorSets(Graphics.device, ref dsAI, out descriptorSet);
                descriptorPool = pool;
                resourceLayout = resLayout;
            }

            System.Diagnostics.Debug.Assert(bindables.Length == resLayout.numBindings);

            writeDescriptorSets = new VkWriteDescriptorSet[resLayout.numBindings];

            for(uint i = 0; i < resLayout.numBindings; i++)
            {
                Bind(i, bindables[i]);
            }

            UpdateSets();
        }

        public void Dispose()
        {
            VulkanNative.vkFreeDescriptorSets(Graphics.device, descriptorPool, 1, ref descriptorSet);
            Graphics.DescriptorPoolManager.Free(descriptorPool, ref resourceLayout.descriptorResourceCounts);
        }

        public ResourceSet Bind(uint dstBinding, IBindable bindable)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            switch(descriptorType)
            {
                case DescriptorType.Sampler:
                    break;
                case DescriptorType.CombinedImageSampler:
                    var texture = bindable as Texture;
                    writeDescriptorSets[dstBinding] = WriteDescriptorSet(descriptorSet, (VkDescriptorType)descriptorType,
                        dstBinding, ref texture.descriptor, 1);
                    break;
                case DescriptorType.SampledImage:
                    break;
                case DescriptorType.StorageImage:
                    break;

                case DescriptorType.UniformTexelBuffer:
                    break;
                case DescriptorType.StorageTexelBuffer:
                    break;

                case DescriptorType.UniformBuffer:
                case DescriptorType.StorageBuffer:
                case DescriptorType.UniformBufferDynamic:
                case DescriptorType.StorageBufferDynamic:
                    var buffer = bindable as GraphicsBuffer;                    
                    writeDescriptorSets[dstBinding] = WriteDescriptorSet(descriptorSet,
                        (VkDescriptorType)descriptorType, dstBinding, ref buffer.descriptor, 1);
                    
                    break;
                case DescriptorType.InputAttachment:
                    break;
            }
            return this;
        }

        public void UpdateSets()
        {
            VulkanNative.vkUpdateDescriptorSets(Graphics.device, (uint)writeDescriptorSets.Length,
                ref writeDescriptorSets[0], 0, IntPtr.Zero);
        }

    }
}
