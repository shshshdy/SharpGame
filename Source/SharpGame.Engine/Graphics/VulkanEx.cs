using Vulkan;

namespace SharpGame
{
    using static VulkanNative;

    public unsafe static class VulkanEx
    {
        public static void WaitIdle(this VkDevice device)
        {
            VulkanUtil.CheckResult(vkDeviceWaitIdle(device));
        }

        public static VkMemoryType GetMemoryType(this VkPhysicalDeviceMemoryProperties memoryProperties, uint index)
        {
            return (&memoryProperties.memoryTypes_0)[index];
        }
    }
}
