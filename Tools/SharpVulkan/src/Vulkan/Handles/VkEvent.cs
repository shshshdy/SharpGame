using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public unsafe partial struct VkEvent : IDisposable
    {
        public VkEvent(VkEventCreateFlags flags)
        {
            var createInfo = new VkEventCreateInfo
            {
                sType = VkStructureType.EventCreateInfo,
                flags = flags
            };

            Vulkan.vkCreateEvent(Vulkan.device, &createInfo, null, out this).CheckResult();
        }

        public VkResult Status
        {
            get
            {
                VkResult result = Vulkan.vkGetEventStatus(Vulkan.device, this);
                if (result != VkResult.EventSet && result != VkResult.EventReset)
                    VulkanUtil.CheckResult(result);
                return result;
            }
        }

        public void Set()
        {
            Vulkan.vkSetEvent(Vulkan.device, this);
        }

        public void Reset()
        {
            Vulkan.vkResetEvent(Vulkan.device, this);
        }

        public void Dispose()
        {
            Vulkan.vkDestroyEvent(Vulkan.device, this, null);
        }
    }

}
