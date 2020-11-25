using System;
using System.Collections.Generic;
using System.Text;


namespace SharpGame
{
    public unsafe partial struct VkPipeline : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyPipeline(Vulkan.device, this, null);            
        }

    }
}
