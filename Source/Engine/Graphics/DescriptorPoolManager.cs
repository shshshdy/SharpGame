using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    internal class DescriptorPoolManager
    {
        private readonly List<PoolInfo> _pools = new List<PoolInfo>();
        private readonly object _lock = new object();
        Graphics graphics_;
        public DescriptorPoolManager(Graphics graphics)
        {
            graphics_ = graphics;
            _pools.Add(CreateNewPool());
        }

        public unsafe DescriptorAllocationToken Allocate(DescriptorResourceCounts counts, DescriptorSetLayout setLayout)
        {
            DescriptorPool pool = GetPool(counts);
            var dsAI = new DescriptorSetAllocateInfo(1, setLayout);
            return new DescriptorAllocationToken(pool.AllocateSets(dsAI)[0], pool);
        }

        public void Free(DescriptorAllocationToken token, DescriptorResourceCounts counts)
        {
            lock (_lock)
            {
                foreach (PoolInfo poolInfo in _pools)
                {
                    if (poolInfo.Pool == token.Pool)
                    {
                        poolInfo.Free(graphics_, token, counts);
                    }
                }
            }
        }

        private DescriptorPool GetPool(DescriptorResourceCounts counts)
        {
            lock (_lock)
            {
                foreach (PoolInfo poolInfo in _pools)
                {
                    if (poolInfo.Allocate(counts))
                    {
                        return poolInfo.Pool;
                    }
                }

                PoolInfo newPool = CreateNewPool();
                _pools.Add(newPool);
                bool result = newPool.Allocate(counts);
                Debug.Assert(result);
                return newPool.Pool;
            }
        }

        private unsafe PoolInfo CreateNewPool()
        {
            int totalSets = 1000;
            int descriptorCount = 100;
            int poolSizeCount = 7;
            DescriptorPoolSize[] sizes = new DescriptorPoolSize[(int)poolSizeCount];
            sizes[0].Type = DescriptorType.UniformBuffer;
            sizes[0].DescriptorCount = descriptorCount;
            sizes[1].Type = DescriptorType.SampledImage;
            sizes[1].DescriptorCount = descriptorCount;
            sizes[2].Type = DescriptorType.Sampler;
            sizes[2].DescriptorCount = descriptorCount;
            sizes[3].Type = DescriptorType.StorageBuffer;
            sizes[3].DescriptorCount = descriptorCount;
            sizes[4].Type = DescriptorType.StorageImage;
            sizes[4].DescriptorCount = descriptorCount;
            sizes[5].Type = DescriptorType.UniformBufferDynamic;
            sizes[5].DescriptorCount = descriptorCount;
            sizes[6].Type = DescriptorType.StorageBufferDynamic;
            sizes[6].DescriptorCount = descriptorCount;

            DescriptorPoolCreateInfo poolCI = new DescriptorPoolCreateInfo
            {
                Flags = DescriptorPoolCreateFlags.FreeDescriptorSet,
                MaxSets = totalSets,
                PoolSizes = sizes
            };
            DescriptorPool descriptorPool = graphics_.Device.CreateDescriptorPool(poolCI);
            return new PoolInfo(descriptorPool, totalSets, descriptorCount);
        }

        internal unsafe void DestroyAll()
        {
            foreach (PoolInfo poolInfo in _pools)
            {
                poolInfo.Pool.Dispose();
            }

            _pools.Clear();
        }

        private class PoolInfo
        {
            public readonly DescriptorPool Pool;

            public int RemainingSets;

            public int UniformBufferCount;
            public int SampledImageCount;
            public int SamplerCount;
            public int StorageBufferCount;
            public int StorageImageCount;

            public PoolInfo(DescriptorPool pool, int totalSets, int descriptorCount)
            {
                Pool = pool;
                RemainingSets = totalSets;
                UniformBufferCount = descriptorCount;
                SampledImageCount = descriptorCount;
                SamplerCount = descriptorCount;
                StorageBufferCount = descriptorCount;
                StorageImageCount = descriptorCount;
            }

            internal bool Allocate(DescriptorResourceCounts counts)
            {
                if (RemainingSets > 0
                    && UniformBufferCount >= counts.UniformBufferCount
                    && SampledImageCount >= counts.SampledImageCount
                    && SamplerCount >= counts.SamplerCount
                    && StorageBufferCount >= counts.SamplerCount
                    && StorageImageCount >= counts.StorageImageCount)
                {
                    RemainingSets -= 1;
                    UniformBufferCount -= counts.UniformBufferCount;
                    SampledImageCount -= counts.SampledImageCount;
                    SamplerCount -= counts.SamplerCount;
                    StorageBufferCount -= counts.StorageBufferCount;
                    StorageImageCount -= counts.StorageImageCount;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            internal void Free(Graphics device, DescriptorAllocationToken token, DescriptorResourceCounts counts)
            {
                DescriptorSet set = token.Set;
                Pool.FreeSets(set);
                //vkFreeDescriptorSets(device, Pool, 1, ref set);

                RemainingSets += 1;

                UniformBufferCount += counts.UniformBufferCount;
                SampledImageCount += counts.SampledImageCount;
                SamplerCount += counts.SamplerCount;
                StorageBufferCount += counts.StorageBufferCount;
                StorageImageCount += counts.StorageImageCount;
            }
        }
    }

    internal struct DescriptorAllocationToken
    {
        public readonly DescriptorSet Set;
        public readonly DescriptorPool Pool;

        public DescriptorAllocationToken(DescriptorSet set, DescriptorPool pool)
        {
            Set = set;
            Pool = pool;
        }
    }

    internal struct DescriptorResourceCounts
    {
        public readonly int UniformBufferCount;
        public readonly int SampledImageCount;
        public readonly int SamplerCount;
        public readonly int StorageBufferCount;
        public readonly int StorageImageCount;

        public DescriptorResourceCounts(
            int uniformBufferCount,
            int sampledImageCount,
            int samplerCount,
            int storageBufferCount,
            int storageImageCount)
        {
            UniformBufferCount = uniformBufferCount;
            SampledImageCount = sampledImageCount;
            SamplerCount = samplerCount;
            StorageBufferCount = storageBufferCount;
            StorageImageCount = storageImageCount;
        }
    }
}
