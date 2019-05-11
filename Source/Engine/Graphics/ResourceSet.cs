using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class ResourceSet : IDisposable
    {
        public DescriptorSet descriptorSet;

        internal DescriptorPool descriptorPool;
        internal ResourceLayout resourceLayout;
        internal ref DescriptorResourceCounts Counts => ref resourceLayout.descriptorResourceCounts;

        private WriteDescriptorSet[] writeDescriptorSets;
        public ResourceSet(ResourceLayout resLayout)
        {
            DescriptorPool pool = Graphics.DescriptorPoolManager.Allocate(resLayout);
            var dsAI = new DescriptorSetAllocateInfo(1, resLayout.descriptorSetLayout);
            descriptorSet = pool.AllocateSets(dsAI)[0];
            descriptorPool = pool;
            resourceLayout = resLayout;
            writeDescriptorSets = new WriteDescriptorSet[resLayout.numBindings];
        }

        public ResourceSet(ResourceLayout resLayout, params IBindable[] bindables)
        {
            DescriptorPool pool = Graphics.DescriptorPoolManager.Allocate(resLayout);
            var dsAI = new DescriptorSetAllocateInfo(1, resLayout.descriptorSetLayout);
            descriptorSet = pool.AllocateSets(dsAI)[0];
            descriptorPool = pool;
            resourceLayout = resLayout;

            System.Diagnostics.Debug.Assert(bindables.Length == resLayout.numBindings);

            writeDescriptorSets = new WriteDescriptorSet[resLayout.numBindings];
            for(int i = 0; i < resLayout.numBindings; i++)
            {
                Bind(i, bindables[i]);
            }
            UpdateSets();
        }

        public void Dispose()
        {
            descriptorPool.FreeSets(descriptorSet);
            Graphics.DescriptorPoolManager.Free(descriptorPool, ref resourceLayout.descriptorResourceCounts);
        }

        public ResourceSet Bind(int dstBinding, int dstArrayElement, int descriptorCount, DescriptorType descriptorType, DescriptorImageInfo[] imageInfo = null, DescriptorBufferInfo[] bufferInfo = null, BufferView[] texelBufferView = null)
        {
            writeDescriptorSets[dstBinding] = new WriteDescriptorSet(descriptorSet, dstBinding, dstArrayElement, descriptorCount, descriptorType, imageInfo, bufferInfo, texelBufferView);
            return this;
        }

        public ResourceSet Bind(int dstBinding, IBindable bindable)
        {
            var descriptorType = resourceLayout.bindings[dstBinding].DescriptorType;
            switch(descriptorType)
            {
                case DescriptorType.Sampler:
                    break;
                case DescriptorType.CombinedImageSampler:
                    var texture = bindable as Texture;
                    writeDescriptorSets[dstBinding] = new WriteDescriptorSet(descriptorSet, dstBinding, 0, 1, 
                        descriptorType, imageInfo : new[] { new DescriptorImageInfo(texture.Sampler, texture.View, ImageLayout.General) });
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
                    writeDescriptorSets[dstBinding] = new WriteDescriptorSet(descriptorSet, dstBinding, 0, 1,
                        descriptorType, bufferInfo: new[] { new DescriptorBufferInfo(buffer) });
                    
                    break;
                case DescriptorType.InputAttachment:
                    break;
            }
            return this;
        }

        public void UpdateSets()
        {
            descriptorPool.UpdateSets(writeDescriptorSets);
        }

    }
}
