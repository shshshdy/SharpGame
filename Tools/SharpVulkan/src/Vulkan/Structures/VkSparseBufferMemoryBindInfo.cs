using System;

namespace SharpGame
{
    public unsafe partial struct VkSparseBufferMemoryBindInfo
    {
        public VkSparseBufferMemoryBindInfo(VkBuffer buffer, Span<VkSparseMemoryBind> binds)
        {
            this.buffer = buffer;
            this.bindCount = (uint)binds.Length;
            fixed (VkSparseMemoryBind* pBinds = binds)
                this.pBinds = pBinds;
        }
    }

}
