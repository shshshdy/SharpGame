﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public class FullScreenSubpass : Subpass
    {
        protected Pass pass;
        public PipelineResourceSet PipelineResourceSet { get; }

        public Action<PipelineResourceSet> onBindResource;

        public FullScreenSubpass(string fs)
        {
            pass = ShaderUtil.CreatePass("shaders/post/fullscreen.vert", fs);
            pass.CullMode = VkCullModeFlags.None;
            pass.DepthTestEnable = false;
            pass.DepthWriteEnable = false;

            PipelineResourceSet = new PipelineResourceSet(pass.PipelineLayout);
        }

        public override void Init()
        {
            CreateResources();
            BindResources();
        }

        public override void DeviceReset()
        {
            BindResources();
        }

        protected virtual void CreateResources()
        {
        }

        protected virtual void BindResources()
        {
            //var rt = FrameGraphPass.RenderTarget[0];
            PipelineResourceSet.SetResourceSet(0, Texture.Blue);

            onBindResource?.Invoke(PipelineResourceSet);
        }

        public override void Draw(RenderContext rc, CommandBuffer cmd)
        {
            DrawFullScreenQuad(cmd, FrameGraphPass.RenderPass, subpassIndex, pass, PipelineResourceSet.ResourceSet);
        }

        public void DrawFullScreenQuad(CommandBuffer cmd, RenderPass renderPass, uint subpass, Pass pass, Span<DescriptorSet> resourceSet)
        {
            var pipe = pass.GetGraphicsPipeline(renderPass, subpass, null);

            cmd.BindPipeline(VkPipelineBindPoint.Graphics, pipe);

            foreach (var rs in resourceSet)
            {
                cmd.BindGraphicsResourceSet(pass.PipelineLayout, rs.Set, rs);
            }

            cmd.Draw(3, 1, 0, 0);
        }

    }
}
