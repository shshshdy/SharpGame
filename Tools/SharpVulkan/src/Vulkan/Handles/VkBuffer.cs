using System;


namespace SharpGame
{
    public unsafe partial struct VkBuffer : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyBuffer(Vulkan.device, this, null);
        }

    }
}
