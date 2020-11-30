using System;


namespace SharpGame
{
    public unsafe partial struct VkRenderPass : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyRenderPass(Vulkan.device, this, null);
        }

    }
}
