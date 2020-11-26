namespace SharpGame
{
    public partial struct VkMemoryBarrier
    {
        public unsafe VkMemoryBarrier(VkAccessFlags srcAccessMask, VkAccessFlags dstAccessMask)
        {
            this.sType = VkStructureType.MemoryBarrier;
            this.pNext = null;
            this.srcAccessMask = (VkAccessFlags)srcAccessMask;
            this.dstAccessMask = (VkAccessFlags)dstAccessMask;
        }
    }

}
