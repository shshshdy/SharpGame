using System;


namespace SharpGame
{
    public unsafe partial struct VkShaderModule : IDisposable
    {
        public void Dispose()
        {
            Vulkan.vkDestroyShaderModule(Vulkan.device, this, null);
        }

    }
}
