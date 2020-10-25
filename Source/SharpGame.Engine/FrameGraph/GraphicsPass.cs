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
    public class GraphicsPass : Subpass
    {
        public Action<GraphicsPass, RenderView> OnDraw { get; set; }

        public GraphicsPass(string name = "", int workCount = 0)
        {
            Name = name;
        }
        
        public override void Draw(RenderView view, uint subpass)
        {
            //BeginRenderPass(view);
            DrawImpl(view);
            //EndRenderPass(view);
        }
        
        protected virtual void DrawImpl(RenderView view)
        {
            OnDraw?.Invoke(this, view);
        }

        public void DrawBatch(ulong passID, CommandBuffer cb, SourceBatch batch, Span<ConstBlock> pushConsts,
            ResourceSet resourceSet, ResourceSet resourceSet1, ResourceSet resourceSet2 = null)
        {
            var shader = batch.material.Shader;
            if ((passID & shader.passFlags) == 0)
            {
                return;
            }

            var pass = shader.GetPass(passID);
            var pipe = pass.GetGraphicsPipeline(FrameGraphPass.RenderPass, FrameGraphPass.Subpass, batch.geometry);

            cb.BindPipeline(PipelineBindPoint.Graphics, pipe);
            cb.BindGraphicsResourceSet(pass.PipelineLayout, 0, resourceSet, batch.offset);

            if (resourceSet1 != null)
            {
                cb.BindGraphicsResourceSet(pass.PipelineLayout, 1, resourceSet1, -1);
            }

            if (resourceSet2 != null)
            {
                cb.BindGraphicsResourceSet(pass.PipelineLayout, 2, resourceSet2, -1);
            }

            foreach (ConstBlock constBlock in pushConsts)
            {
                cb.PushConstants(pass.PipelineLayout, constBlock.range.stageFlags, constBlock.range.offset, constBlock.range.size, constBlock.data);
            }

            batch.material.Bind(pass.passIndex, cb);
            batch.geometry.Draw(cb);
        }

        public void DrawFullScreenQuad(Pass pass, CommandBuffer cb, ResourceSet resourceSet, ResourceSet resourceSet1, ResourceSet resourceSet2 = null)
        {
            var pipe = pass.GetGraphicsPipeline(FrameGraphPass.RenderPass, FrameGraphPass.Subpass, null);

            cb.BindPipeline(PipelineBindPoint.Graphics, pipe);
            cb.BindGraphicsResourceSet(pass.PipelineLayout, 0, resourceSet);

            if (resourceSet1 != null)
            {
                cb.BindGraphicsResourceSet(pass.PipelineLayout, 1, resourceSet1);
            }

            if (resourceSet2 != null)
            {
                cb.BindGraphicsResourceSet(pass.PipelineLayout, 2, resourceSet2, -1);
            }

            cb.Draw(3, 1, 0, 0);
        }


    }



}
