using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class NewSubpass : GraphicsSubpass
    {
        public DescriptorSet Set0 { get; set; }
        public DescriptorSet Set1 { get; set; }
        public DescriptorSet Set2 { get; set; }

        Dictionary<Pass, BatchGroup> batches = new Dictionary<Pass, BatchGroup>();

        public NewSubpass(string name = "") : base(name)
        {
        }

        public override void Draw(RenderContext rc, CommandBuffer cmd)
        {
            var set0 = Set0 ?? View.Set0;
            Span<DescriptorSet> set1 = new[] { Set1 ?? View.Set1, Set2 };
// 
//             cmd.SetViewport(View.Viewport);
//             cmd.SetScissor(View.ViewRect);

            DrawBatches(cmd, View.opaqueBatches.AsSpan(), set0, set1);
   
            DrawBatches(cmd, View.alphaTestBatches.AsSpan(), set0, set1);

            DrawBatches(cmd, View.translucentBatches.AsSpan(), set0, set1);

        }


    }
}
