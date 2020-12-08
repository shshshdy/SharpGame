using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;


namespace SharpGame
{
    using static Vulkan;
    internal class DescriptorPoolManager
    {
        private readonly List<PoolInfo> _pools = new List<PoolInfo>();
        private readonly object _lock = new object();
        public DescriptorPoolManager()
        {
            _pools.Add(CreateNewPool());
        }

        public void Free(VkDescriptorPool pool, ref DescriptorResourceCounts counts, uint count = 1)
        {
            lock (_lock)
            {
                foreach (PoolInfo poolInfo in _pools)
                {
                    if (poolInfo.Pool == pool)
                    {
                        poolInfo.Free(ref counts, count);
                    }
                }
            }
        }

        public VkDescriptorPool GetPool(ref DescriptorResourceCounts counts, uint count = 1)
        {
            lock (_lock)
            {
                foreach (PoolInfo poolInfo in _pools)
                {
                    if (poolInfo.Allocate(counts, count))
                    {
                        return poolInfo.Pool;
                    }
                }

                PoolInfo newPool = CreateNewPool();
                _pools.Add(newPool);
                bool result = newPool.Allocate(counts, count);
                Debug.Assert(result);
                return newPool.Pool;
            }
        }

        static readonly VkDescriptorType[] descriptorTypes = {
            VkDescriptorType.Sampler,
            VkDescriptorType.CombinedImageSampler,
            VkDescriptorType.SampledImage,
            VkDescriptorType.StorageImage,
            VkDescriptorType.UniformTexelBuffer,
            VkDescriptorType.StorageTexelBuffer,
            VkDescriptorType.UniformBuffer,
            VkDescriptorType.StorageBuffer,
            VkDescriptorType.UniformBufferDynamic,
            VkDescriptorType.StorageBufferDynamic,
            VkDescriptorType.InputAttachment,
            VkDescriptorType.InlineUniformBlockEXT/* = 1000138000*/,
            VkDescriptorType.AccelerationStructureKHR/* = 1000165000*/,
            VkDescriptorType.AccelerationStructureNV/* = AccelerationStructureKHR*/,
            0,
            0
        };

        public const int MAX_DESCRIPTOR_COUNT = 13;

        private unsafe PoolInfo CreateNewPool()
        {
            uint totalSets = 1000;
            uint descriptorCount = 256;
            uint poolSizeCount = MAX_DESCRIPTOR_COUNT;

            VkDescriptorPoolSize* sizes = stackalloc VkDescriptorPoolSize[(int)poolSizeCount];
            for(int i = 0; i < MAX_DESCRIPTOR_COUNT; i++)
            {
                sizes[i].type = descriptorTypes[i];
                sizes[i].descriptorCount = descriptorCount;
            }

            VkDescriptorPoolInlineUniformBlockCreateInfoEXT descriptorPoolInlineUniformBlockCreateInfo = new VkDescriptorPoolInlineUniformBlockCreateInfoEXT
            {
                sType = VkStructureType.DescriptorPoolInlineUniformBlockCreateInfoEXT,
                maxInlineUniformBlockBindings = totalSets
            };

            var poolCI = new VkDescriptorPoolCreateInfo
            {
                sType = VkStructureType.DescriptorPoolCreateInfo,
                flags = VkDescriptorPoolCreateFlags.FreeDescriptorSet,
                poolSizeCount = poolSizeCount,
                maxSets = totalSets,
                pPoolSizes = sizes,
                pNext = &descriptorPoolInlineUniformBlockCreateInfo
            };

            var descriptorPool = Device.CreateDescriptorPool(ref poolCI);
            return new PoolInfo(descriptorPool, totalSets, descriptorCount);
        }

        internal unsafe void DestroyAll()
        {
            foreach (PoolInfo poolInfo in _pools)
            {
                Device.DestroyDescriptorPool(poolInfo.Pool);
            }

            _pools.Clear();
        }

        private class PoolInfo
        {
            public readonly VkDescriptorPool Pool;

            public uint RemainingSets;
            public uint[] RemainingCount = new uint[MAX_DESCRIPTOR_COUNT];

            public PoolInfo(VkDescriptorPool pool, uint totalSets, uint descriptorCount)
            {
                Pool = pool;
                RemainingSets = totalSets;
                for (int i = 0; i < MAX_DESCRIPTOR_COUNT; i++)
                {
                    RemainingCount[i] = descriptorCount;
                }
            }

            internal bool Allocate(DescriptorResourceCounts counts, uint count = 1)
            {
                if(RemainingSets <= 0)
                {
                    return false;
                }

                for(int i = 0; i < MAX_DESCRIPTOR_COUNT; i++)
                {
                    if(RemainingCount[i] < counts[i] * count)
                    {
                        return false;
                    }
                }

                RemainingSets -= 1;
                for (int i = 0; i < MAX_DESCRIPTOR_COUNT; i++)
                {
                    RemainingCount[i] -= counts[i] * count;
                }

                return true;               
            }

            internal void Free(ref DescriptorResourceCounts counts, uint count = 1)
            {
                RemainingSets += 1;
                for (int i = 0; i < MAX_DESCRIPTOR_COUNT; i++)
                {
                    RemainingCount[i] += (counts[i] * count);
                }
            }
        }
    }

}
