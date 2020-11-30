using System;


namespace SharpGame
{
    public unsafe partial struct VkFramebuffer : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyFramebuffer(Vulkan.device, this, null);
        }

    }
}
