using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Vulkan;

namespace SharpGame
{
    using static Vulkan.VulkanNative;
    internal class DescriptorPoolManager
    {
        private readonly List<PoolInfo> _pools = new List<PoolInfo>();
        private readonly object _lock = new object();
        public DescriptorPoolManager()
        {
            _pools.Add(CreateNewPool());
        }

        public unsafe VkDescriptorPool Allocate(ResourceLayout resLayout)
        {
            VkDescriptorPool pool = GetPool(ref resLayout.descriptorResourceCounts);
            return pool;
        }

        public void Free(VkDescriptorPool pool, ref DescriptorResourceCounts counts)
        {
            lock (_lock)
            {
                foreach (PoolInfo poolInfo in _pools)
                {
                    if (poolInfo.Pool == pool)
                    {
                        poolInfo.Free(ref counts);
                    }
                }
            }
        }

        private VkDescriptorPool GetPool(ref DescriptorResourceCounts counts)
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
            uint totalSets = 1000;
            uint descriptorCount = 100;
            int poolSizeCount = 11;

            VkDescriptorPoolSize* sizes = stackalloc VkDescriptorPoolSize[(int)poolSizeCount];
            for(int i = 0; i < 11; i++)
            {
                sizes[i].type = (VkDescriptorType)i;
                sizes[i].descriptorCount = descriptorCount;
            }

            VkDescriptorPoolCreateInfo poolCI = VkDescriptorPoolCreateInfo.New();
            poolCI.flags = VkDescriptorPoolCreateFlags.FreeDescriptorSet;
            poolCI.maxSets = totalSets;
            poolCI.pPoolSizes = sizes;

            VkDescriptorPool descriptorPool;
            Util.CheckResult(vkCreateDescriptorPool(Graphics.device, ref poolCI, null, out descriptorPool));
            return new PoolInfo(descriptorPool, totalSets, descriptorCount);
        }

        internal unsafe void DestroyAll()
        {
            foreach (PoolInfo poolInfo in _pools)
            {
                vkDestroyDescriptorPool(Graphics.device, poolInfo.Pool, null);
            }

            _pools.Clear();
        }

        private class PoolInfo
        {
            public readonly VkDescriptorPool Pool;

            public uint RemainingSets;
            public uint[] RemainingCount = new uint[11];

            public PoolInfo(VkDescriptorPool pool, uint totalSets, uint descriptorCount)
            {
                Pool = pool;
                RemainingSets = totalSets;
                for (int i = 0; i < 11; i++)
                {
                    RemainingCount[i] = descriptorCount;
                }
            }

            internal bool Allocate(DescriptorResourceCounts counts)
            {
                if(RemainingSets <= 0)
                {
                    return false;
                }

                for(int i = 0; i < 11; i++)
                {
                    if(RemainingCount[i] < counts[i])
                    {
                        return false;
                    }
                }

                RemainingSets -= 1;
                for (int i = 0; i < 11; i++)
                {
                    RemainingCount[i] -= counts[i];
                }

                return true;               
            }

            internal void Free(ref DescriptorResourceCounts counts)
            {
                RemainingSets += 1;
                for (int i = 0; i < 11; i++)
                {
                    RemainingCount[i] += counts[i];
                }
            }
        }
    }

}
