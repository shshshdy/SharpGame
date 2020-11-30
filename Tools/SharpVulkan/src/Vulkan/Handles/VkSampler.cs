using System;


namespace SharpGame
{
    public unsafe partial struct VkSampler : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroySampler(Vulkan.device, this, null);
        }

    }
}
