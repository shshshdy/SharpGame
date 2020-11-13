using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class Geometry : Object
    {
        public string Name { get; set; }

        private Buffer[] vertexBuffers_;
        public Buffer[] VertexBuffers
        {
            get => vertexBuffers_;
            set
            {
                vertexBuffers_ = value;
                numVertexBuffer = (uint)vertexBuffers_.Length;
                if (numVertexBuffer > 4)
                {
                    numVertexBuffer = 4;
                    System.Diagnostics.Debug.Assert(false);
                }

                for (int i = 0; i < numVertexBuffer; i++)
                {
                    buffers_[i] = vertexBuffers_[i].buffer;
                    offsets_[i] = 0;
                }
            }
        }

        public Buffer IndexBuffer { get; set; }
        public PrimitiveTopology PrimitiveTopology { get; set; } = PrimitiveTopology.TriangleList;
        public uint VertexStart { get; set; }
        public uint VertexCount { get; set; }
        public uint IndexStart { get; set; }
        public uint IndexCount { get; set; }
        public int VertexOffset { get; set; }
        public float LodDistance { get; set; }

        public VertexLayout VertexLayout { get; set; }

        uint numVertexBuffer;
        FixedArray4<VkBuffer> buffers_ = new FixedArray4<VkBuffer>();
        FixedArray4<ulong> offsets_ = new FixedArray4<ulong>();

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
            cmdBuffer.BindVertexBuffers(0, numVertexBuffer, buffers_.Data, ref offsets_.item1);

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

        protected override void Destroy(bool disposing)
        {
            foreach (var vb in VertexBuffers)
            {
                vb.Dispose();
            }

            IndexBuffer?.Dispose();

            base.Destroy(disposing);
        }

    }
}
