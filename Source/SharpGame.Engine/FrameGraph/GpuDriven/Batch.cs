using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public struct Batch
    {
        public SourceBatch pass;

    }

    public class BatchGroup
    {
        public Pass pass;
        public PipelineResourceSet pipelineResourceSet;

        public FastList<Batch> geometries = new FastList<Batch>();

    }
}
