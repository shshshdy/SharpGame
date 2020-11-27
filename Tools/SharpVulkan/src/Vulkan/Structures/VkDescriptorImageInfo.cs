using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public partial struct VkDescriptorImageInfo
    {
        public VkDescriptorImageInfo(VkSampler sampler, VkImageView imageView, VkImageLayout imageLayout)
        {
            this.sampler = sampler;
            this.imageView = imageView;
            this.imageLayout = imageLayout;           
        }
    }

}
