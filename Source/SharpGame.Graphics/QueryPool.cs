using System;
using System.Runtime.InteropServices;


namespace SharpGame
{
    /// <summary>
    /// Opaque handle to a query pool object.
    /// <para>
    /// Queries are managed using query pool objects. Each query pool is a collection of a specific
    /// number of queries of a particular type.
    /// </para>
    /// </summary>
    public unsafe class QueryPool : DisposeBase
    {
        internal VkQueryPool handle;
        public QueryPool(ref QueryPoolCreateInfo createInfo)
        {
            handle = Device.CreateQueryPool(ref createInfo.native);            
        }

        public void GetResults(uint firstQuery, uint queryCount, uint dataSize, IntPtr data, ulong stride, QueryResults flags = 0)
        {
            Device.GetQueryPoolResults(handle, firstQuery, queryCount, (UIntPtr)dataSize, (void*)data, stride, (VkQueryResultFlags)flags);
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

    /// <summary>
    /// Structure specifying parameters of a newly created query pool.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct QueryPoolCreateInfo
    {
        internal VkQueryPoolCreateInfo native;
        
        public QueryPoolCreateInfo(QueryType queryType, uint queryCount,
            QueryPipelineStatisticFlags pipelineStatistics = QueryPipelineStatisticFlags.None)
        {
            native = new VkQueryPoolCreateInfo
            {
                sType = VkStructureType.QueryPoolCreateInfo
            };
            native.queryType = (VkQueryType)queryType;
            native.queryCount = queryCount;
            native.pipelineStatistics = (VkQueryPipelineStatisticFlags)pipelineStatistics;
        }

    }

    // Is reserved for future use.
    [Flags]
    internal enum QueryPoolCreateFlags
    {
        None = 0
    }

    /// <summary>
    /// Specify the type of queries managed by a query pool.
    /// </summary>
    public enum QueryType
    {
        /// <summary>
        /// Specifies an occlusion query.
        /// </summary>
        Occlusion = 0,
        /// <summary>
        /// Specifies a pipeline statistics query.
        /// </summary>
        PipelineStatistics = 1,
        /// <summary>
        /// Specifies a timestamp query.
        /// </summary>
        Timestamp = 2
    }

    /// <summary>
    /// Bitmask specifying how and when query results are returned.
    /// </summary>
    [Flags]
    public enum QueryResults
    {
        /// <summary>
        /// No flags.
        /// </summary>
        None = 0,
        /// <summary>
        /// Specifies the results will be written as an array of 64-bit unsigned integer values. If
        /// this bit is not set, the results will be written as an array of 32-bit unsigned integer values.
        /// </summary>
        Query64 = 1 << 0,
        /// <summary>
        /// Specifies that Vulkan will wait for each query's status to become available before
        /// retrieving its results.
        /// </summary>
        QueryWait = 1 << 1,
        /// <summary>
        /// Specifies that the availability status accompanies the results.
        /// </summary>
        QueryWithAvailability = 1 << 2,
        /// <summary>
        /// Specifies that returning partial results is acceptable.
        /// </summary>
        QueryPartial = 1 << 3
    }

}
