using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public struct FramebufferInfo
    {

    }

    public class Framebuffer : DisposeBase
    {
        VkFramebuffer framebuffer;

        public RenderPass renderPass;
    }
}
