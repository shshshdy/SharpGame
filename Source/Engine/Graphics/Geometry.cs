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
        public int VertexCount { get; set; }

        VulkanCore.Buffer[] buffers_;
        long[] offsets_;

        public async override void Load()
        {

        }

        public void Draw(CommandBuffer cmdBuffer)
        {
            cmdBuffer.CmdBindVertexBuffers(0, buffers_.Length, buffers_, offsets_);

            if(IndexBuffer != null)
            {
                cmdBuffer.CmdBindIndexBuffer(IndexBuffer);
                cmdBuffer.CmdDrawIndexed(IndexBuffer.Count);
            }
            else
            {
                cmdBuffer.CmdDraw(VertexCount);
            }
        }
    }
}
