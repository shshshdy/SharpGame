using System;
using System.Runtime.InteropServices;


namespace SharpGame
{
    public unsafe partial struct VkQueryPool : IDisposable
    {
        public VkQueryPool(VkQueryType queryType, uint queryCount, VkQueryPipelineStatisticFlags pipelineStatistics = VkQueryPipelineStatisticFlags.None)
        {
            var createInfo = new VkQueryPoolCreateInfo
            {
                sType = VkStructureType.QueryPoolCreateInfo,
                queryType = queryType,
                queryCount = queryCount,
                pipelineStatistics = pipelineStatistics,
            };

            Vulkan.vkCreateQueryPool(Vulkan.device, &createInfo, null, out this).CheckResult();          
      
        }

        public void Dispose()
        {
            Vulkan.vkDestroyQueryPool(Vulkan.device, this, null);
        }

        public void GetResults(uint firstQuery, uint queryCount, uint dataSize, IntPtr data, ulong stride, VkQueryResultFlags flags = VkQueryResultFlags.None)
        {
            Vulkan.vkGetQueryPoolResults(Vulkan.device, this, firstQuery, queryCount, (UIntPtr)dataSize, (void*)data, stride, flags);
        }

    }



}
