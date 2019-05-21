﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public unsafe struct FramebufferCreateInfo
    {
        public uint flags;
        public RenderPass renderPass;
        public int attachmentCount;
        public VkImageView* pAttachments;
        public int width;
        public int height;
        public int layers;

        public unsafe void ToNative(out VkFramebufferCreateInfo native)
        {
            native = VkFramebufferCreateInfo.New();
            native.flags = flags;
            native.renderPass = renderPass.handle;
            native.attachmentCount = (uint)attachmentCount;
            native.pAttachments = (VkImageView*)pAttachments;
            native.width = (uint)width;
            native.height = (uint)height;
            native.layers = (uint)layers;
        }
    }

    public class Framebuffer : DisposeBase
    {
        internal VkFramebuffer handle;
        public RenderPass renderPass { get; }

        public Framebuffer(ref FramebufferCreateInfo framebufferCreateInfo)
        {
            renderPass = framebufferCreateInfo.renderPass;
            framebufferCreateInfo.ToNative(out VkFramebufferCreateInfo vkFramebufferCreateInfo);
            handle = Device.CreateFramebuffer(ref vkFramebufferCreateInfo);       
        }

        protected override void Destroy()
        {
            Device.Destroy(handle);
        }
    }
}
