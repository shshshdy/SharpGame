using System;


namespace SharpGame
{
    public unsafe partial struct VkImage : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyImage(Vulkan.device, this, null);
        }

    }
}
