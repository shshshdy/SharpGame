using System.Runtime.CompilerServices;

namespace SharpGame
{
    public partial struct VkBufferCreateInfo
    {
        public unsafe VkBufferCreateInfo(VkBufferUsageFlags usage, ulong size, uint[] queueFamilyIndices = default)
        {
            this.sType = VkStructureType.BufferCreateInfo;
            this.pNext = null;
            this.flags = VkBufferCreateFlags.None;
            this.sharingMode = VkSharingMode.Exclusive;

            if (queueFamilyIndices != null)
            {
                this.queueFamilyIndexCount = (uint)queueFamilyIndices.Length;
                this.pQueueFamilyIndices = (uint*)Unsafe.AsPointer(ref queueFamilyIndices[0]);
            }
            else
            {
                this.queueFamilyIndexCount = 0;
                this.pQueueFamilyIndices = null;
            }

            this.usage = usage;
            this.size = size;
        }
    }

}
