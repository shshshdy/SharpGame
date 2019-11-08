using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public unsafe ref struct FramebufferCreateInfo
    {
        public RenderPass renderPass;
        public uint width;
        public uint height;
        public uint layers;
        public Span<VkImageView> attachments;
        public uint flags;

        public unsafe void ToNative(out VkFramebufferCreateInfo native)
        {
            native = VkFramebufferCreateInfo.New();
            native.flags = flags;
            native.renderPass = renderPass.handle;
            native.attachmentCount = (uint)attachments.Length;
            native.pAttachments = (VkImageView*)Unsafe.AsPointer(ref attachments[0]);
            native.width = width;
            native.height = height;
            native.layers = layers;
        }
    }

    public class Framebuffer : DisposeBase
    {
        public RenderPass renderPass { get; }
        public uint Width { get; }
        public uint Height { get; }

        internal VkFramebuffer handle;

        public Framebuffer(ref FramebufferCreateInfo framebufferCreateInfo)
        {
            renderPass = framebufferCreateInfo.renderPass;
            framebufferCreateInfo.ToNative(out VkFramebufferCreateInfo native);
            handle = Device.CreateFramebuffer(ref native);
            Width = native.width;
            Height = native.height;
        }

        protected override void Destroy(bool disposing)
        {
            Device.Destroy(handle);
        }

        public static Framebuffer Create(RenderPass renderPass, uint width, uint height, uint layers, ImageView[] attachments)
        {
            Span<VkImageView> views = stackalloc VkImageView[attachments.Length];
            for(int i = 0; i < views.Length; i++)
            {
                views[i] = attachments[i].handle;
            }

            var framebufferCreateInfo = new FramebufferCreateInfo
            {
                renderPass = renderPass,
                attachments = views,
                width = width,
                height = height,
                layers = layers
            };

            return new Framebuffer(ref framebufferCreateInfo);
        }
    }
}
