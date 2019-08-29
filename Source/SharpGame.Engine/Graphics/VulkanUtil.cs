using System;
using Vulkan;

namespace SharpGame
{
    public unsafe static class VulkanUtil
    {
        public static void CheckResult(VkResult result)
        {
            if (result != VkResult.Success)
            {
                Log.Error(result.ToString());
                throw new InvalidOperationException("Call failed.");
            }
        }

    }
}
