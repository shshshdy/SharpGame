using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class ResourceSet : IDisposable
    {
        internal DescriptorSet descriptorSet;
        internal DescriptorPool descriptorPool;
        internal ResourceLayout resourceLayout;
        internal ref DescriptorResourceCounts Counts => ref resourceLayout.descriptorResourceCounts;
        public ResourceSet(ResourceLayout resLayout)
        {
            DescriptorPool pool = Graphics.DescriptorPoolManager.Allocate(resLayout);
            var dsAI = new DescriptorSetAllocateInfo(1, resLayout.descriptorSetLayout);
            descriptorSet = pool.AllocateSets(dsAI)[0];
            descriptorPool = pool;
            resourceLayout = resLayout;
        }

        public void Dispose()
        {
            descriptorPool.FreeSets(descriptorSet);
            Graphics.DescriptorPoolManager.Free(descriptorPool, ref resourceLayout.descriptorResourceCounts);
        }
    }
}
