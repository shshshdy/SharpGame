using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class Geometry : Object
    {
        private GraphicsBuffer[] vertexBuffers_;
        public GraphicsBuffer[] VertexBuffers
        {
            get => vertexBuffers_;
            set
            {
                vertexBuffers_ = value;
                buffers_ = new VkBuffer[vertexBuffers_.Length];
                offsets_ = new long[vertexBuffers_.Length];
                for (int i = 0; i < vertexBuffers_.Length; i++)
                {
                    buffers_[i] = vertexBuffers_[i].buffer;
                    offsets_[i] = 0;
                }
            }
        }

        public GraphicsBuffer IndexBuffer { get; set; }
        public VkPrimitiveTopology PrimitiveTopology { get; set; } = VkPrimitiveTopology.TriangleList;
        public int VertexStart { get; set; }
        public int VertexCount { get; set; }
        public int IndexStart { get; set; }
        public int IndexCount { get; set; }
        public float LodDistance { get; set; }

        public VkPipelineVertexInputStateCreateInfo VertexLayout { get; set; }

        VkBuffer[] buffers_;
        long[] offsets_;

        public Geometry()
        {
        }

        protected override void Destroy()
        {
            foreach(var vb in VertexBuffers)
            {
                vb.Dispose();
            }

            IndexBuffer?.Dispose();

            base.Destroy();
        }

        public void SetNumVertexBuffers(int num)
        {
            Array.Resize(ref vertexBuffers_, num);
        }

        public bool SetDrawRange(VkPrimitiveTopology type, int indexStart, int indexCount)
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

        public bool SetDrawRange(VkPrimitiveTopology type, int indexStart, int indexCount, int vertexStart, int vertexCount)
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
            /*
            cmdBuffer.CmdBindVertexBuffers(0, buffers_.Length, buffers_, offsets_);

            if(IndexBuffer != null && IndexCount > 0)
            {
                cmdBuffer.CmdBindIndexBuffer(IndexBuffer, 0, IndexBuffer.Stride == 2 ?  IndexType.UInt16 : IndexType.UInt32);
                cmdBuffer.CmdDrawIndexed(IndexCount, 1, IndexStart, VertexStart);
            }
            else
            {
                cmdBuffer.CmdDraw(VertexCount, 1, VertexStart);
            }*/
        }
    }
}
