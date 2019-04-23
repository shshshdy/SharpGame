using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public struct Resizing
    {
    }

    public struct BeginRender
    {
    }

    public struct RenderEnd
    {
    }

    public struct BeginRenderPass
    {
        public RenderPass renderPass;
        public VulkanCore.CommandBuffer commandBuffer;
        public int imageIndex;
    }

    public struct RenderPassEnd
    {
        public RenderPass renderPass;
        public VulkanCore.CommandBuffer commandBuffer;
        public int imageIndex;
    }
}
