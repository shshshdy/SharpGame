using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public struct RenderPassBegin
    {
        public VulkanCore.CommandBuffer commandBuffer;
        public int imageIndex;
    }

    public struct RenderPassEnd
    {
        public VulkanCore.CommandBuffer commandBuffer;
        public int imageIndex;
    }
}
