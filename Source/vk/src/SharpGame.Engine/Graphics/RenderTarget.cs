using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class RenderTarget : DisposeBase
    {
        public VkImage image;
        public VkDeviceMemory mem;
        public VkImageView view;
    }
}
