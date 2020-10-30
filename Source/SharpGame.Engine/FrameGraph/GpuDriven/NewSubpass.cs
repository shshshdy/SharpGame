using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class NewSubpass : Subpass
    {
        public void DrawBatch(CommandBuffer cb, ulong passID, SourceBatch batch, Span<ConstBlock> pushConsts, PipelineResourceSet sets)
        {

        }
    }
}
