using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGame
{

    public unsafe partial struct VkFence : IDisposable
    {
        public VkFence(VkFenceCreateFlags flags)
        {
            VkFenceCreateInfo createInfo = new VkFenceCreateInfo
            {
                sType = VkStructureType.FenceCreateInfo,
                flags = flags
            };

            Vulkan.vkCreateFence(Vulkan.device, &createInfo, null, out this).CheckResult();
        }

        public VkResult GetStatus()
        {
            VkResult result = Vulkan.vkGetFenceStatus(Vulkan.device, this);
            return result;
        }

        public void Reset()
        {
            Vulkan.vkResetFences(Vulkan.device, this);
        }

        public void Wait(ulong timeout = ~0ul)
        {
            Vulkan.vkWaitForFences(Vulkan.device, this, false, timeout);
        }

        public void Dispose()
        {
            Vulkan.vkDestroyFence(Vulkan.device, this, null);
        }
    }

}
