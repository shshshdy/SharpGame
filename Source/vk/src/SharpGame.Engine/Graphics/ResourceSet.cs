using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;


namespace SharpGame
{
    using System.Runtime.CompilerServices;
    using static Initializers;

    public class ResourceSet : IDisposable
    {
        public VkDescriptorSet descriptorSet;

        internal VkDescriptorPool descriptorPool;
        internal ResourceLayout resourceLayout;
        internal ref DescriptorResourceCounts Counts => ref resourceLayout.descriptorResourceCounts;

        private VkWriteDescriptorSet[] writeDescriptorSets;
        public ResourceSet(ResourceLayout resLayout)
        {/*
            VkDescriptorPool pool = Graphics.DescriptorPoolManager.Allocate(resLayout);
            unsafe
            {
                var dsAI = descriptorSetAllocateInfo(pool, (VkDescriptorSetLayout*)Unsafe.AsPointer(ref resLayout.descriptorSetLayout), 1);
                descriptorSet = pool.AllocateSets(dsAI)[0];
                descriptorPool = pool;
                resourceLayout = resLayout;
            }
            writeDescriptorSets = new VkWriteDescriptorSet[resLayout.numBindings];*/
        }

        public ResourceSet(ResourceLayout resLayout, params IBindable[] bindables)
        {/*
            VkDescriptorPool pool = Graphics.DescriptorPoolManager.Allocate(resLayout);
            unsafe
            {
                var dsAI = descriptorSetAllocateInfo(pool, (VkDescriptorSetLayout*)Unsafe.AsPointer(ref resLayout.descriptorSetLayout), 1);
                descriptorSet = pool.AllocateSets(dsAI)[0];
                descriptorPool = pool;
                resourceLayout = resLayout;
            }

            System.Diagnostics.Debug.Assert(bindables.Length == resLayout.numBindings);

            writeDescriptorSets = new VkWriteDescriptorSet[resLayout.numBindings];
            for(int i = 0; i < resLayout.numBindings; i++)
            {
                Bind(i, bindables[i]);
            }

            UpdateSets();*/
        }

        public void Dispose()
        {
        //todo    descriptorPool.FreeSets(descriptorSet);
            //Graphics.DescriptorPoolManager.Free(descriptorPool, ref resourceLayout.descriptorResourceCounts);
        }
        /*
        public ResourceSet Bind(int dstBinding, int dstArrayElement, int descriptorCount, VkDescriptorType descriptorType, VkDescriptorImageInfo[] imageInfo = null, VkDescriptorBufferInfo[] bufferInfo = null, VkBufferView[] texelBufferView = null)
        {
            writeDescriptorSets[dstBinding] = writeDescriptorSet(descriptorSet, dstBinding, dstArrayElement, descriptorCount, descriptorType, imageInfo, bufferInfo, texelBufferView);
            return this;
        }*/

        public ResourceSet Bind(int dstBinding, IBindable bindable)
        {/*
            var descriptorType = resourceLayout.bindings[dstBinding].descriptorType;
            switch(descriptorType)
            {
                case VkDescriptorType.Sampler:
                    break;
                case VkDescriptorType.CombinedImageSampler:
                    var texture = bindable as Texture;
                    writeDescriptorSets[dstBinding] = writeDescriptorSet(descriptorSet, dstBinding, 0, 1, 
                        descriptorType, imageInfo : new[] { new DescriptorImageInfo(texture.Sampler, texture.View, ImageLayout.General) });
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
                    writeDescriptorSets[dstBinding] = writeDescriptorSet(descriptorSet, dstBinding, 0, 1,
                        descriptorType, bufferInfo: new[] { new DescriptorBufferInfo(buffer) });
                    
                    break;
                case VkDescriptorType.InputAttachment:
                    break;
            }*/
            return this;
        }

        public void UpdateSets()
        {
        //    descriptorPool.UpdateSets(writeDescriptorSets);
        }

    }
}
