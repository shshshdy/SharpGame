using System;


namespace SharpGame
{
    public unsafe partial struct VkBufferView : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyBufferView(Vulkan.device, this, null);
        }

    }
}
