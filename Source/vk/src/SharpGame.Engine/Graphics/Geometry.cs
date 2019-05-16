using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class Geometry : Object
    {
        public VkPrimitiveTopology PrimitiveTopology { get; set; } = VkPrimitiveTopology.TriangleList;

        public VkPipelineVertexInputStateCreateInfo VertexLayout { get; set; }
    }
}
