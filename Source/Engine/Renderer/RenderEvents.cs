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

    public struct EndRender
    {
    }

    public struct BeginView
    {
    }

    public struct EndView
    {
    }

    public struct BeginRenderPass
    {
        public RenderPass renderPass;
        public VulkanCore.CommandBuffer commandBuffer;
        public int imageIndex;
    }

    public struct EndRenderPass
    {
        public RenderPass renderPass;
        public VulkanCore.CommandBuffer commandBuffer;
        public int imageIndex;
    }
}
