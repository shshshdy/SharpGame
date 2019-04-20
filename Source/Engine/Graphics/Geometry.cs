using System;
using System.Collections.Generic;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    public class Geometry : Resource
    {
        public PrimitiveTopology PrimitiveTopology { get; set; }
        public List<GraphicsBuffer> VertexBuffer { get; set; }
        public GraphicsBuffer IndexBuffer { get; set; }        
    }
}
