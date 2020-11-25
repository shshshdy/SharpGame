using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    using System.Runtime.CompilerServices;
    using global::System.Runtime.InteropServices;

    public unsafe partial struct VkSemaphore : IDisposable
    {
        public VkSemaphore(VkSemaphoreCreateFlags flags)
        {
            var semaphoreCreateInfo = new VkSemaphoreCreateInfo
            {
                sType = VkStructureType.SemaphoreCreateInfo,
                flags = flags
            };

            Vulkan.vkCreateSemaphore(Vulkan.device, &semaphoreCreateInfo, null, out VkSemaphore handle);
            Handle = handle.Handle;
        }

        public void Dispose()
        {
            Vulkan.vkDestroySemaphore(Vulkan.device, this, null);
        }


    }


}
