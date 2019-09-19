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
        public float LodDistance { get; set; }

        public VertexLayout VertexLayout { get; set; }

        uint numVertexBuffer;
        FixedArray4<VkBuffer> buffers_ = new FixedArray4<VkBuffer>();
        FixedArray4<ulong> offsets_ = new FixedArray4<ulong>();

        public Geometry()
        {
        }

        public bool SetDrawRange(PrimitiveTopology type, uint indexStart, uint indexCount)
        {
            if (IndexBuffer == null)
            {
                return false;
            }

            if (IndexBuffer != null && indexStart + indexCount > IndexBuffer.Count)
            {
                return false;
            }

            PrimitiveTopology = type;

            IndexStart = indexStart;
            IndexCount = indexCount;

            // Get min.vertex index and num of vertices from index buffer. If it fails, use full range as fallback
            if (indexCount > 0)
            {
                VertexStart = 0;
                VertexCount = VertexBuffers[0] != null ? (uint)VertexBuffers[0].Count : 0;                
            }
            else
            {
                VertexStart = 0;
                VertexCount = 0;
            }

            return true;
        }

        public bool SetDrawRange(PrimitiveTopology type, uint indexStart, uint indexCount, uint vertexStart, uint vertexCount)
        {
            if (IndexBuffer != null)
            {
                // We can allow setting an illegal draw range now if the caller guarantees to resize / fill the buffer later
                if (indexStart + indexCount > IndexBuffer.Count)
                {
                    return false;
                }
            }
            else
            {
                indexStart = 0;
                indexCount = 0;
            }

            PrimitiveTopology = type;
            IndexStart = indexStart;
            IndexCount = indexCount;
            VertexStart = vertexStart;
            VertexCount = vertexCount;

            return true;
        }
        
        [MethodImpl((MethodImplOptions)0x100)]
        public void Draw(CommandBuffer cmdBuffer)
        {
            cmdBuffer.BindVertexBuffers(0, numVertexBuffer, buffers_.Data, ref offsets_.item1);

            if(IndexBuffer != null && IndexCount > 0)
            {
                cmdBuffer.BindIndexBuffer(IndexBuffer, 0, IndexBuffer.Stride == 2 ?  IndexType.Uint16 : IndexType.Uint32);
                cmdBuffer.DrawIndexed(IndexCount, 1, IndexStart, (int)VertexStart, 0);
            }
            else
            {
                cmdBuffer.Draw(VertexCount, 1, VertexStart, 0);
            }
        }

        protected override void Destroy()
        {
            foreach (var vb in VertexBuffers)
            {
                vb.Dispose();
            }

            IndexBuffer?.Dispose();

            base.Destroy();
        }

    }
}
