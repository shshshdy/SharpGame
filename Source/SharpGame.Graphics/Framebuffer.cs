using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;


namespace SharpGame
{
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
