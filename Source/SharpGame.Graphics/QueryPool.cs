using System;
using System.Runtime.InteropServices;


namespace SharpGame
{
    public unsafe class QueryPool : DisposeBase
    {
        internal VkQueryPool handle;
        public QueryPool(VkQueryType queryType, uint queryCount, VkQueryPipelineStatisticFlags pipelineStatistics = VkQueryPipelineStatisticFlags.None)
        {
            var createInfo = new VkQueryPoolCreateInfo
            {
                sType = VkStructureType.QueryPoolCreateInfo,
                queryType = queryType,
                queryCount = queryCount,
                pipelineStatistics = pipelineStatistics,
            };

            handle = Device.CreateQueryPool(ref createInfo);            
        }

        public void GetResults(uint firstQuery, uint queryCount, uint dataSize, IntPtr data, ulong stride, VkQueryResultFlags flags = VkQueryResultFlags.None)
        {
            Device.GetQueryPoolResults(handle, firstQuery, queryCount, (UIntPtr)dataSize, (void*)data, stride, flags);
        }

        protected override void Destroy(bool disposing)
        {
            base.Destroy(disposing);

            if(handle != 0)
            {
                Device.DestroyQueryPool(ref handle);
                handle = 0;
            }

        }

    }



}
