using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class NewSubpass : GraphicsSubpass, DrawHelper
    {
        public Pass Pass { get; }
        public PipelineResourceSet PipelineResourceSet { get; }
        public Dictionary<(uint, uint), StringID> inputResources { get; } = new Dictionary<(uint, uint), StringID>();
        public Dictionary<uint, StringID> inputResourceSets { get; }

        Dictionary<Pass, BatchGroup> batches = new Dictionary<Pass, BatchGroup>();

        public NewSubpass(string name = "") : base(name)
        {
        }

        public override void Draw(RenderContext rc, CommandBuffer cmd)
        {
            
            /*
            var set0 = Set0 ?? View.Set0;
            Span<DescriptorSet> set1 = new[] { Set1 ?? View.Set1, Set2 };
// 
//             cmd.SetViewport(View.Viewport);
//             cmd.SetScissor(View.ViewRect);

            DrawBatches(cmd, View.opaqueBatches.AsSpan(), set0, set1);
   
            DrawBatches(cmd, View.alphaTestBatches.AsSpan(), set0, set1);

            DrawBatches(cmd, View.translucentBatches.AsSpan(), set0, set1);
            */
        }


    }
}
