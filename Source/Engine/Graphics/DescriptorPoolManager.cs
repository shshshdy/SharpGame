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
        public DescriptorPoolManager()
        {
            _pools.Add(CreateNewPool());
        }

        public unsafe DescriptorPool Allocate(ResourceLayout resLayout)
        {
            DescriptorPool pool = GetPool(ref resLayout.descriptorResourceCounts);
            return pool;
        }

        public void Free(DescriptorPool pool, ref DescriptorResourceCounts counts)
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

        private DescriptorPool GetPool(ref DescriptorResourceCounts counts)
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
            int poolSizeCount = 11;
            DescriptorPoolSize[] sizes = new DescriptorPoolSize[(int)poolSizeCount];
            for(int i = 0; i < 11; i++)
            {
                sizes[i].Type = (DescriptorType)i;
                sizes[i].DescriptorCount = descriptorCount;
            }

            DescriptorPoolCreateInfo poolCI = new DescriptorPoolCreateInfo
            {
                Flags = DescriptorPoolCreateFlags.FreeDescriptorSet,
                MaxSets = totalSets,
                PoolSizes = sizes
            };

            DescriptorPool descriptorPool = Graphics.Device.CreateDescriptorPool(poolCI);
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
            public int[] RemainingCount = new int[11];

            public PoolInfo(DescriptorPool pool, int totalSets, int descriptorCount)
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
