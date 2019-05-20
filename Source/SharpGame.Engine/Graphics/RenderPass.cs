using System;
using System.Collections.Generic;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class RenderPass : DisposeBase
    {
        public AttachmentDescription[] Attachments { get; set; }
        public SubpassDescription[] Subpasses { get; set; }

        public VkRenderPass handle;

        public RenderPass()
        {
        }

        protected override void Destroy()
        {
            Device.Destroy(handle);
        }
    }

    [Flags]
    public enum AttachmentDescriptionFlags
    {
        None = 0,
        MayAlias = 1
    }

    public enum SampleCountFlags
    {
        None = 0,
        Count1 = 1,
        Count2 = 2,
        Count4 = 4,
        Count8 = 8,
        Count16 = 16,
        Count32 = 32,
        Count64 = 64
    }

    public enum AttachmentLoadOp
    {
        Load = 0,
        Clear = 1,
        DontCare = 2
    }

    public enum AttachmentStoreOp
    {
        Store = 0,
        DontCare = 1
    }

    public struct AttachmentDescription
    {
        public AttachmentDescriptionFlags flags;
        public Format format;
        public SampleCountFlags samples;
        public AttachmentLoadOp loadOp;
        public AttachmentStoreOp storeOp;
        public AttachmentLoadOp stencilLoadOp;
        public AttachmentStoreOp stencilStoreOp;
        public ImageLayout initialLayout;
        public ImageLayout finalLayout;
    }

    public struct AttachmentReference
    {
        public uint attachment;
        public ImageLayout layout;
    }

    public struct SubpassDescription
    {
        public VkSubpassDescriptionFlags flags;
        public VkPipelineBindPoint pipelineBindPoint;
        public AttachmentReference[] pInputAttachments;
        public AttachmentReference[] pColorAttachments;
        public AttachmentReference[] pResolveAttachments;
        public AttachmentReference[] pDepthStencilAttachment;
        public uint[] pPreserveAttachments;
    }

}
