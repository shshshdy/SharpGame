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
            var descriptorType = resourceLayout.bindings[dstBinding].descriptorType;
            switch(descriptorType)
            {
                case VkDescriptorType.Sampler:
                    break;
                case VkDescriptorType.CombinedImageSampler:
                    var texture = bindable as Texture;
                    writeDescriptorSets[dstBinding] = WriteDescriptorSet(descriptorSet, descriptorType,
                        dstBinding, ref texture.descriptor, 1);
                    break;
                case VkDescriptorType.SampledImage:
                    break;
                case VkDescriptorType.StorageImage:
                    break;

                case VkDescriptorType.UniformTexelBuffer:
                    break;
                case VkDescriptorType.StorageTexelBuffer:
                    break;


                case VkDescriptorType.UniformBuffer:
                case VkDescriptorType.StorageBuffer:
                case VkDescriptorType.UniformBufferDynamic:
                case VkDescriptorType.StorageBufferDynamic:
                    var buffer = bindable as GraphicsBuffer;                    
                    writeDescriptorSets[dstBinding] = WriteDescriptorSet(descriptorSet,
                        descriptorType, dstBinding, ref buffer.descriptor, 1);
                    
                    break;
                case VkDescriptorType.InputAttachment:
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
