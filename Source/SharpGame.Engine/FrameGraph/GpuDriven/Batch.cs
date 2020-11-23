using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public struct Batch
    {
        public SourceBatch sourceBatch;

    }

    public class BatchGroup
    {
        public Pass pass;
        public Buffer materials;

        public Vector<VkDescriptorBufferInfo> buffers;
        public FastList<Batch> geometries = new FastList<Batch>();

    }
}
