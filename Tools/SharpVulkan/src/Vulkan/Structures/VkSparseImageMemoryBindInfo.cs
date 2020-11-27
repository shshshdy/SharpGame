using System;
using System.Runtime.CompilerServices;

namespace SharpGame
{
    public unsafe partial struct VkSparseImageMemoryBindInfo
    {
        public VkSparseImageMemoryBindInfo(VkImage image, ref VkSparseImageMemoryBind binds)
        {
            this.image = image;
            this.bindCount = 1;
            this.pBinds = (VkSparseImageMemoryBind*)Unsafe.AsPointer(ref binds);
        }

        public VkSparseImageMemoryBindInfo(VkImage image, ReadOnlySpan<VkSparseImageMemoryBind> binds)
        {
            this.image = image;
            this.bindCount = (uint)binds.Length;

            fixed(VkSparseImageMemoryBind* pBinds = binds)
                this.pBinds = pBinds;
        }
    }

}
