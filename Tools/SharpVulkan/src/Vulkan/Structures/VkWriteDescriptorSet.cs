using System.Runtime.CompilerServices;

namespace SharpGame
{
    public partial struct VkWriteDescriptorSet
    {
        public unsafe VkWriteDescriptorSet(uint binding, VkDescriptorSet dstSet, VkDescriptorType type,
            ref VkDescriptorBufferInfo bufferInfo, uint descriptorCount = 1)
        {
            this.sType = VkStructureType.WriteDescriptorSet;
            this.pNext = null;
            this.dstSet = dstSet;
            this.descriptorType = type;
            this.dstBinding = binding;
            this.pBufferInfo = (VkDescriptorBufferInfo*)Unsafe.AsPointer(ref bufferInfo);
            this.descriptorCount = descriptorCount;
            this.pImageInfo = null;
            this.pTexelBufferView = null;
            this.dstArrayElement = 0;
        }

        public unsafe VkWriteDescriptorSet(
            uint binding,
            VkDescriptorSet dstSet,
            VkDescriptorType type,
            ref VkDescriptorImageInfo imageInfo,
            uint descriptorCount = 1)
        {
            this.sType = VkStructureType.WriteDescriptorSet; 
            this.pNext = null;
            this.dstSet = dstSet;
            this.descriptorType = type;
            this.dstBinding = binding;
            this.pImageInfo = (VkDescriptorImageInfo*)Unsafe.AsPointer(ref imageInfo);
            this.descriptorCount = descriptorCount;
            this.pBufferInfo = null;
            this.pTexelBufferView = null;
            this.dstArrayElement = 0;
        }

        public unsafe VkWriteDescriptorSet(uint binding,
            VkDescriptorSet dstSet,
            VkDescriptorType type,
            ref VkDescriptorBufferInfo bufferInfo,
            ref VkBufferView bufferView)
        {
            this.sType = VkStructureType.WriteDescriptorSet;
            this.pNext = null;
            this.dstSet = dstSet;
            this.descriptorType = type;
            this.dstBinding = binding;
            this.descriptorCount = 1;
            this.pBufferInfo = (VkDescriptorBufferInfo*)Unsafe.AsPointer(ref bufferInfo);
            this.pTexelBufferView = (VkBufferView*)Unsafe.AsPointer(ref bufferView);
            this.pImageInfo = null;
            this.dstArrayElement = 0;
        }

        public unsafe VkWriteDescriptorSet(uint binding,
            VkDescriptorSet dstSet,
            VkDescriptorType type, System.IntPtr data, uint size)
        {
            VkWriteDescriptorSetInlineUniformBlockEXT inlineUniformBlockEXT = new VkWriteDescriptorSetInlineUniformBlockEXT
            {
                sType = VkStructureType.WriteDescriptorSetInlineUniformBlockEXT,
                pData = (void*)data,
                dataSize = size
            };
            this.sType = VkStructureType.WriteDescriptorSet;
            this.pNext = &inlineUniformBlockEXT;
            this.dstSet = dstSet;
            this.descriptorType = type;
            this.dstBinding = binding;
            this.descriptorCount = 1;
            this.pBufferInfo = null;
            this.pTexelBufferView = null;
            this.pImageInfo = null;
            this.dstArrayElement = 0;
        }
    }

}
