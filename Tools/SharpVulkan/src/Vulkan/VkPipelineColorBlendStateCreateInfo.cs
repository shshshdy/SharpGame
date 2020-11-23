using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public partial struct VkPipelineColorBlendStateCreateInfo
    {
        public static VkPipelineColorBlendStateCreateInfo New()
        {
            return new VkPipelineColorBlendStateCreateInfo
            {
                sType = VkStructureType.PipelineCacheCreateInfo
            };
        }
    }
}
