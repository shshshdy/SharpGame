using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;


namespace SharpGame
{
    public struct Primitive
    {
        public Buffer vertexBuffer;
        public Buffer IndexBuffer;
        public uint start;
        public uint count;
        public int vertexOffset;
    }

    public class Geometry
    {
        public string Name { get; set; }
        public VertexLayout VertexLayout { get; set; }
        public PrimitiveTopology PrimitiveTopology { get; set; } = PrimitiveTopology.TriangleList;
        public Buffer VertexBuffer { get; set; }
        public Buffer IndexBuffer { get; set; }
        public uint VertexStart { get; set; }
        public uint VertexCount { get; set; }
        public uint IndexStart { get; set; }
        public uint IndexCount { get; set; }
        public int VertexOffset { get; set; }
        public float LodDistance { get; set; }

        public Geometry()
        {
        }

        public void SetDrawRange(PrimitiveTopology type, uint vertexStart, uint vertexCount)
        {
            PrimitiveTopology = type;
            IndexStart = 0;
            IndexCount = 0;
            VertexStart = vertexStart;
            VertexCount = vertexCount;
        }

        public void SetDrawRange(PrimitiveTopology type, uint indexStart, uint indexCount, int vertexOffset)
        {
            PrimitiveTopology = type;
            IndexStart = indexStart;
            IndexCount = indexCount;
            VertexOffset = vertexOffset;
        }
        
        [MethodImpl((MethodImplOptions)0x100)]
        public void Draw(CommandBuffer cmdBuffer)
        {
            cmdBuffer.BindVertexBuffer(0, VertexBuffer);

            if(IndexBuffer != null && IndexCount > 0)
            {
                cmdBuffer.BindIndexBuffer(IndexBuffer, 0, IndexBuffer.Stride == 2 ?  IndexType.Uint16 : IndexType.Uint32);
                cmdBuffer.DrawIndexed(IndexCount, 1, IndexStart, VertexOffset, 0);
            }
            else
            {
                cmdBuffer.Draw(VertexCount, 1, VertexStart, 0);
            }
        }


    }
}
