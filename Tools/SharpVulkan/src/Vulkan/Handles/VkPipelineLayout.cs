using System;


namespace SharpGame
{
    public unsafe partial struct VkPipelineLayout : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyPipelineLayout(Vulkan.device, this, null);
        }

    }
}
