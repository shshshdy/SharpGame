using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class Geometry : Object
    {
        private DeviceBuffer[] vertexBuffers_;
        public DeviceBuffer[] VertexBuffers
        {
            get => vertexBuffers_;
            set
            {
                vertexBuffers_ = value;

                buffers_.Count = (uint)vertexBuffers_.Length;
                offsets_.Count = (uint)vertexBuffers_.Length;
                for (int i = 0; i < vertexBuffers_.Length; i++)
                {
                    buffers_[i] = vertexBuffers_[i].buffer;
                    offsets_[i] = 0;
                }
            }
        }

        public DeviceBuffer IndexBuffer { get; set; }
        public PrimitiveTopology PrimitiveTopology { get; set; } = PrimitiveTopology.TriangleList;
        public int VertexStart { get; set; }
        public int VertexCount { get; set; }
        public int IndexStart { get; set; }
        public int IndexCount { get; set; }
        public float LodDistance { get; set; }

        public VertexLayout VertexLayout { get; set; }

        NativeList<VkBuffer> buffers_ = new NativeList<VkBuffer>();
        NativeList<ulong> offsets_ = new NativeList<ulong>();

        public Geometry()
        {
        }

        public bool SetDrawRange(PrimitiveTopology type, int indexStart, int indexCount)
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
                VertexCount = VertexBuffers[0] != null ? VertexBuffers[0].Count : 0;                
            }
            else
            {
                VertexStart = 0;
                VertexCount = 0;
            }

            return true;
        }

        public bool SetDrawRange(PrimitiveTopology type, int indexStart, int indexCount, int vertexStart, int vertexCount)
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

        public void Draw(CommandBuffer cmdBuffer)
        {
            cmdBuffer.BindVertexBuffers(0, buffers_.Count, buffers_.Data, ref offsets_[0]);

            if(IndexBuffer != null && IndexCount > 0)
            {
                cmdBuffer.BindIndexBuffer(IndexBuffer, 0, IndexBuffer.Stride == 2 ?  IndexType.Uint16 : IndexType.Uint32);
                cmdBuffer.DrawIndexed((uint)IndexCount, 1, (uint)IndexStart, VertexStart, 0);
            }
            else
            {
                cmdBuffer.Draw((uint)VertexCount, 1, (uint)VertexStart, 0);
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
