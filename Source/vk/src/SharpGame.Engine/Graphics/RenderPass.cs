using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class RenderPass : Object
    {
        public int passID;
        internal VkRenderPass renderPass;
    }
}
