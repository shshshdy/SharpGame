using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;


namespace SharpGame
{
    using global::System.Runtime.Serialization;
    using System.Runtime.CompilerServices;

    public class ResourceSet : IDisposable
    {
        public int Set => resourceLayout.Set;
        public bool Updated { get; private set; } = false;

        internal ResourceLayout resourceLayout;

        [IgnoreDataMember]
        internal ref DescriptorResourceCounts Counts => ref resourceLayout.descriptorResourceCounts;

        internal VkDescriptorSet descriptorSet;
        internal VkDescriptorPool descriptorPool;
        internal WriteDescriptorSet[] writeDescriptorSets;
        public ResourceSet(ResourceLayout resLayout)
        {
            resLayout.Build();

            descriptorPool = Graphics.DescriptorPoolManager.Allocate(resLayout);
            resourceLayout = resLayout;

            unsafe
            {
                VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = VkDescriptorSetAllocateInfo.New();
                descriptorSetAllocateInfo.descriptorPool = descriptorPool;
                descriptorSetAllocateInfo.pSetLayouts = (VkDescriptorSetLayout*)Unsafe.AsPointer(ref resLayout.DescriptorSetLayout);
                descriptorSetAllocateInfo.descriptorSetCount = 1;

                VulkanNative.vkAllocateDescriptorSets(Graphics.device, ref descriptorSetAllocateInfo, out descriptorSet);
            }

            writeDescriptorSets = new WriteDescriptorSet[resLayout.NumBindings];
        }

        public ResourceSet(ResourceLayout resLayout, params IBindableResource[] bindables)
        {
            resLayout.Build();

            descriptorPool = Graphics.DescriptorPoolManager.Allocate(resLayout);
            resourceLayout = resLayout;

            unsafe
            {
                VkDescriptorSetAllocateInfo descriptorSetAllocateInfo = VkDescriptorSetAllocateInfo.New();
                descriptorSetAllocateInfo.descriptorPool = descriptorPool;
                descriptorSetAllocateInfo.pSetLayouts = (VkDescriptorSetLayout*)Unsafe.AsPointer(ref resLayout.DescriptorSetLayout);
                descriptorSetAllocateInfo.descriptorSetCount = 1;

                VulkanNative.vkAllocateDescriptorSets(Graphics.device, ref descriptorSetAllocateInfo, out descriptorSet);
            }

            System.Diagnostics.Debug.Assert(bindables.Length == resLayout.NumBindings);

            writeDescriptorSets = new WriteDescriptorSet[resLayout.NumBindings];

            for(uint i = 0; i < resLayout.NumBindings; i++)
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

        public ResourceSet Bind(uint dstBinding, Span<VkDescriptorImageInfo> imageInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            writeDescriptorSets[dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet,
                               descriptorType, ref imageInfo[0], (uint)imageInfo.Length);
            return this;
        }

        public ResourceSet Bind(uint dstBinding, Span<VkDescriptorBufferInfo> bufferInfo)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            writeDescriptorSets[dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet,
                               descriptorType, ref bufferInfo[0], (uint)bufferInfo.Length);
            return this;
        }

        public ResourceSet Bind(uint dstBinding, IBindableResource bindable)
        {
            var descriptorType = resourceLayout.Bindings[(int)dstBinding].descriptorType;
            switch(descriptorType)
            {
                case DescriptorType.Sampler:
                    break;
                case DescriptorType.CombinedImageSampler:
                    {
                        var texture = bindable as Texture;
                        writeDescriptorSets[dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet,
                            descriptorType, ref texture.descriptor, 1);
                    }
                    break;
                case DescriptorType.SampledImage:
                    break;
                case DescriptorType.StorageImage:
                    {
                        var texture = bindable as Texture;
                        writeDescriptorSets[dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet,
                            descriptorType, ref texture.descriptor, 1);
                    }
                    break;

                case DescriptorType.UniformTexelBuffer:
                    break;
                case DescriptorType.StorageTexelBuffer:
                    break;

                case DescriptorType.UniformBuffer:
                case DescriptorType.StorageBuffer:
                case DescriptorType.UniformBufferDynamic:
                case DescriptorType.StorageBufferDynamic:
                    var buffer = bindable as DeviceBuffer;                    
                    writeDescriptorSets[dstBinding] = new WriteDescriptorSet(dstBinding, descriptorSet,
                        descriptorType, ref buffer.descriptor, 1);
                    
                    break;
                case DescriptorType.InputAttachment:
                    break;
            }
            return this;
        }

        public void UpdateSets()
        {
            VulkanNative.vkUpdateDescriptorSets(Graphics.device, (uint)writeDescriptorSets.Length,
                ref Unsafe.As<WriteDescriptorSet, VkWriteDescriptorSet>(ref writeDescriptorSets[0]), 0, IntPtr.Zero);
            Updated = true;
        }

    }

    public struct WriteDescriptorSet
    {
        internal VkWriteDescriptorSet native;
        public unsafe WriteDescriptorSet(uint binding,
            VkDescriptorSet dstSet,
            DescriptorType type,
            ref VkDescriptorBufferInfo bufferInfo,
            uint descriptorCount = 1)
        {
            native = VkWriteDescriptorSet.New();
            native.dstSet = dstSet;
            native.descriptorType = (VkDescriptorType)type;
            native.dstBinding = binding;
            native.pBufferInfo = (VkDescriptorBufferInfo*)Unsafe.AsPointer(ref bufferInfo);
            native.descriptorCount = descriptorCount;
        }

        public unsafe WriteDescriptorSet(
            uint binding,
            VkDescriptorSet dstSet,
            DescriptorType type,
            ref VkDescriptorImageInfo imageInfo,
            uint descriptorCount = 1)
        {
            native = VkWriteDescriptorSet.New();
            native.dstSet = dstSet;
            native.descriptorType = (VkDescriptorType)type;
            native.dstBinding = binding;
            native.pImageInfo = (VkDescriptorImageInfo*)Unsafe.AsPointer(ref imageInfo);
            native.descriptorCount = descriptorCount;
        }
    }

}
