using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;


namespace SharpGame
{
    public struct SubpassDescription
    {
        public VkSubpassDescriptionFlags flags;
        public VkPipelineBindPoint pipelineBindPoint;
        public VkAttachmentReference[] pInputAttachments;
        public VkAttachmentReference[] pColorAttachments;
        public VkAttachmentReference[] pResolveAttachments;
        public VkAttachmentReference[] pDepthStencilAttachment;
        public uint[] pPreserveAttachments;

        public unsafe void ToNative(VkSubpassDescription* native)
        {
            native->flags = flags;
            native->pipelineBindPoint = pipelineBindPoint;

            if (!pInputAttachments.IsNullOrEmpty())
            {
                native->inputAttachmentCount = (uint)pInputAttachments.Length;
                native->pInputAttachments = (VkAttachmentReference*)Unsafe.AsPointer(ref pInputAttachments[0]);
            }
            else
            {
                native->inputAttachmentCount = 0;
                native->pInputAttachments = null;
            }

            if (!pColorAttachments.IsNullOrEmpty())
            {
                native->colorAttachmentCount = (uint)pColorAttachments.Length;
                native->pColorAttachments = (VkAttachmentReference*)Unsafe.AsPointer(ref pColorAttachments[0]);
            }
            else
            {
                native->colorAttachmentCount = 0;
                native->pColorAttachments = null;
            }

            if (!pResolveAttachments.IsNullOrEmpty())
            {
                native->pResolveAttachments = (VkAttachmentReference*)Unsafe.AsPointer(ref pResolveAttachments[0]);
            }
            else
            {
                native->pResolveAttachments = null;
            }

            if (!pDepthStencilAttachment.IsNullOrEmpty())
            {
                native->pDepthStencilAttachment = (VkAttachmentReference*)Unsafe.AsPointer(ref pDepthStencilAttachment[0]);
            }
            else
            {
                native->pDepthStencilAttachment = null;
            }

            if (!pPreserveAttachments.IsNullOrEmpty())
            {
                native->preserveAttachmentCount = (uint)pPreserveAttachments.Length;
                native->pPreserveAttachments = (uint*)Unsafe.AsPointer(ref pPreserveAttachments[0]);
            }
            else
            {
                native->preserveAttachmentCount = 0;
                native->pPreserveAttachments = null;
            }

        }

    }

    public class RenderPass : HandleBase<VkRenderPass>
    {
        public VkAttachmentDescription[] attachments;
        public SubpassDescription[] subpasses;
        public VkSubpassDependency[] dependencies;

        public RenderPass(VkAttachmentDescription[] attachments,
            SubpassDescription[] subpasses, VkSubpassDependency[] dependencies, VkRenderPassCreateFlags flags = 0)
        {
            this.attachments = attachments;
            this.subpasses = subpasses;
            this.dependencies = dependencies;

            unsafe
            {
                using Vector<VkSubpassDescription> subPasses = new Vector<VkSubpassDescription>((uint)subpasses.Length, (uint)subpasses.Length);

                var renderPassCreateInfo = new VkRenderPassCreateInfo
                {
                    sType = VkStructureType.RenderPassCreateInfo
                };
                renderPassCreateInfo.flags = flags;
                renderPassCreateInfo.attachmentCount = (uint)attachments.Length;
                renderPassCreateInfo.pAttachments = (VkAttachmentDescription*)Unsafe.AsPointer(ref attachments[0]);
                renderPassCreateInfo.subpassCount = (uint)subpasses.Length;

                for (uint i = 0; i < subpasses.Length; i++)
                {
                    subpasses[i].ToNative((VkSubpassDescription*)subPasses.GetAddress(i));
                }

                renderPassCreateInfo.pSubpasses = (VkSubpassDescription*)subPasses.Data;
                renderPassCreateInfo.dependencyCount = (uint)dependencies.Length;
                renderPassCreateInfo.pDependencies = (VkSubpassDependency*)Unsafe.AsPointer(ref dependencies[0]);

                handle = Device.CreateRenderPass(ref renderPassCreateInfo);
            }
        }

        public uint GetColorAttachmentCount(uint subpass)
        {
            var pColorAttachments = subpasses[subpass].pColorAttachments;
            return (uint)(pColorAttachments != null ? pColorAttachments.Length : 1);
        }


    }


}
