using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;


namespace SharpGame
{
    public unsafe ref struct FramebufferCreateInfo
    {
        public RenderPass renderPass;
        public uint width;
        public uint height;
        public uint layers;
        public Span<VkImageView> attachments;
        public VkFramebufferCreateFlags flags;

        public unsafe void ToNative(out VkFramebufferCreateInfo native)
        {
            native = new VkFramebufferCreateInfo
            {
                sType = VkStructureType.FramebufferCreateInfo
            };
            native.flags = flags;
            native.renderPass = renderPass;
            native.attachmentCount = (uint)attachments.Length;
            native.pAttachments = (VkImageView*)Unsafe.AsPointer(ref attachments[0]);
            native.width = width;
            native.height = height;
            native.layers = layers;
        }
    }

    public class Framebuffer : HandleBase<VkFramebuffer>
    {
        public RenderPass renderPass { get; }
        public uint Width { get; }
        public uint Height { get; }

        public Framebuffer(RenderPass renderPass, uint width, uint height, uint layers, ImageView[] attachments)
        {
            Span<VkImageView> views = stackalloc VkImageView[attachments.Length];
            for (int i = 0; i < views.Length; i++)
            {
                views[i] = attachments[i];
            }

            this.renderPass = renderPass;
            Width = width;
            Height = height;

            Create(renderPass, width, height, layers, views, VkFramebufferCreateFlags.None);
        }

        public Framebuffer(RenderPass renderPass, uint width, uint height, uint layers, ReadOnlySpan<VkImageView> attachments, VkFramebufferCreateFlags flags = 0)
        {
            this.renderPass = renderPass;
            Width = width;
            Height = height;

            Create(renderPass, width, height, layers, attachments, flags);
        }

        public static implicit operator VkFramebuffer(Framebuffer cmd) => cmd.handle;

        unsafe void Create(RenderPass renderPass, uint width, uint height, uint layers, ReadOnlySpan<VkImageView> attachments, VkFramebufferCreateFlags flags = 0)
        {
            fixed (VkImageView* attachmentsPtr = attachments)
            {
                var framebufferCreateInfo = new VkFramebufferCreateInfo
                {
                    sType = VkStructureType.FramebufferCreateInfo,
                    flags = flags,
                    renderPass = renderPass,
                    attachmentCount = (uint)attachments.Length,
                    pAttachments = attachmentsPtr,
                    width = width,
                    height = height,
                    layers = layers
                };

                handle = Device.CreateFramebuffer(ref framebufferCreateInfo);
            };

        }

    }
}
