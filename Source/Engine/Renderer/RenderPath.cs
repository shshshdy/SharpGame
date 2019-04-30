﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class RenderPath : Resource
    {
        public Dictionary<string, Framebuffer> FrameBuffers = new Dictionary<string, Framebuffer>();
        public List<RenderPass> RenderPasses { get; set; } = new List<RenderPass>();

        public RenderPath()
        {
        }

        public void AddRenderPass(RenderPass renderPass)
        {
            renderPass.RenderPath = this;
            RenderPasses.Add(renderPass);
        }

        public void Draw(View view)
        {
            var graphics = Get<Graphics>();

            foreach (var renderPass in RenderPasses)
            {
                renderPass.Draw();
            }
            
        }

        public void Summit(int imageIndex)
        {
            foreach (var renderPass in RenderPasses)
            {
                renderPass.Summit(imageIndex);
            }

        }

    }

}
