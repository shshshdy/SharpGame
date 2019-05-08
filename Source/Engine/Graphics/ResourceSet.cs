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

        public ResourceSet(ResourceLayout resLayout, IBindable[] bindables)
        {
            DescriptorPool pool = Graphics.DescriptorPoolManager.Allocate(resLayout);
            var dsAI = new DescriptorSetAllocateInfo(1, resLayout.descriptorSetLayout);
            descriptorSet = pool.AllocateSets(dsAI)[0];
            descriptorPool = pool;
            resourceLayout = resLayout;
            writeDescriptorSets = new WriteDescriptorSet[resLayout.numBindings];
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
            //   return Bind(dstBinding, 0, 1, bindable.DescriptorType, bufferInfo: new[] { new DescriptorBufferInfo(uniformBuffer) });
            return this;
        }

        public ResourceSet UniformBuffer(int dstBinding, GraphicsBuffer uniformBuffer)
        {
            return Bind(dstBinding, 0, 1, DescriptorType.UniformBuffer, bufferInfo : new[] { new DescriptorBufferInfo(uniformBuffer) });
        }

        public ResourceSet CombinedImageSampler(int dstBinding, Texture texture)
        {
            return Bind(dstBinding, 0, 1, DescriptorType.CombinedImageSampler, imageInfo : new[] { new DescriptorImageInfo(texture.Sampler, texture.View, ImageLayout.General) });
        }

        public void UpdateSets()
        {
            descriptorPool.UpdateSets(writeDescriptorSets);
        }

    }
}
