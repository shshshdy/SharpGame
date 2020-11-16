using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Vulkan;


namespace SharpGame
{
    public class GraphicsSubpass : Subpass
    {
        public Action<GraphicsSubpass, RenderContext, CommandBuffer> OnDraw { get; set; }

        public GraphicsSubpass(string name = "")
        {
            Name = name;
        }

        public override void Draw(RenderContext rc, CommandBuffer cb)
        {
            OnDraw?.Invoke(this, rc, cb);
        }

        public void DrawBatch(CommandBuffer cb, ulong passID, SourceBatch batch, Span<ConstBlock> pushConsts,
            ResourceSet resourceSet, Span<ResourceSet> resourceSet1)
        {
            var shader = batch.material.Shader;
            if ((passID & shader.passFlags) == 0)
            {
                return;
            }

            var pass = shader.GetPass(passID);
            var pipe = pass.GetGraphicsPipeline(FrameGraphPass.RenderPass, subpassIndex, batch.geometry);

            cb.BindPipeline(PipelineBindPoint.Graphics, pipe);
            cb.BindGraphicsResourceSet(pass.PipelineLayout, 0, resourceSet, batch.offset);

            int firstSet = 1;
            foreach (var rs in resourceSet1)
            {
                if (firstSet < pass.PipelineLayout.ResourceLayout.Length && rs != null)
                {
                    cb.BindGraphicsResourceSet(pass.PipelineLayout, firstSet, rs, -1);
                }
                firstSet++;
            }

            foreach (ConstBlock constBlock in pushConsts)
            {
                cb.PushConstants(pass.PipelineLayout, constBlock.range.stageFlags, constBlock.range.offset, constBlock.range.size, constBlock.data);
            }
            
            batch.Draw(cb, pass.passIndex);

            //batch.material.Bind(pass.passIndex, cb);
            //batch.geometry.Draw(cb);
        }


    }



}
