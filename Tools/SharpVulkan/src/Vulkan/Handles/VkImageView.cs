using System;


namespace SharpGame
{
    public unsafe partial struct VkImageView : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyImageView(Vulkan.device, this, null);
        }

    }
}
