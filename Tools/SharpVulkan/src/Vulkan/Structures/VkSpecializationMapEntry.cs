namespace SharpGame
{
    public partial struct VkSpecializationMapEntry
    {
        public VkSpecializationMapEntry(uint constantID, uint offset, VkPointerSize size)
        {
            this.constantID = constantID;
            this.offset = offset;
            this.size = size;
        }
    }

}
