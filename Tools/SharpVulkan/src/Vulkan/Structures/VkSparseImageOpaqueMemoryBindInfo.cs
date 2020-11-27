using System;
using System.Runtime.CompilerServices;

namespace SharpGame
{
    public unsafe partial struct VkSparseImageOpaqueMemoryBindInfo
    {
        public VkSparseImageOpaqueMemoryBindInfo(VkImage image, ref VkSparseMemoryBind binds)
        {
            this.image = image;
            this.bindCount = 1;
            this.pBinds = (VkSparseMemoryBind*)Unsafe.AsPointer(ref binds);
        }

        public VkSparseImageOpaqueMemoryBindInfo(VkImage image, ReadOnlySpan<VkSparseMemoryBind> binds)
        {
            this.image = image;
            this.bindCount = (uint)binds.Length;
            fixed (VkSparseMemoryBind* pBinds = binds)
                this.pBinds = pBinds;
        }
    }

}
