﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    public class GraphicsSubpass : Subpass
    {
        public RenderPass RenderPass => FrameGraphPass.RenderPass;
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
            DescriptorSet resourceSet, Span<DescriptorSet> resourceSet1)
        {
            var shader = batch.material.Shader;
            if ((passID & shader.passFlags) == 0)
            {
                return;
            }

            var pass = shader.GetPass(passID);
            var pipe = pass.GetGraphicsPipeline(FrameGraphPass.RenderPass, SubpassIndex, batch.geometry);

            cb.BindPipeline(VkPipelineBindPoint.Graphics, pipe);

            batch.Draw(cb, pushConsts, resourceSet, resourceSet1, pass);

        }

        public void DrawBatches(CommandBuffer commandBuffer, Span<SourceBatch> sourceBatches, DescriptorSet set0, Span<DescriptorSet> set1)
        {
            foreach (var batch in sourceBatches)
            {
                DrawBatch(commandBuffer, passID, batch, default, set0, set1);
            }
        }

    }



}
