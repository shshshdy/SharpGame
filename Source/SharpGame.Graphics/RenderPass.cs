using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class RenderPass : DisposeBase
    {
        public AttachmentDescription[] attachments;
        public SubpassDescription[] subpasses;
        public SubpassDependency[] dependencies;

        public uint GetColorAttachmentCount(uint subpass)
        {
            var pColorAttachments = subpasses[subpass].pColorAttachments;
            return (uint)(pColorAttachments != null ? pColorAttachments.Length : 1);
        }

        internal VkRenderPass handle;

        public RenderPass(AttachmentDescription[] attachments,
            SubpassDescription[] subpasses, SubpassDependency[] dependencies, uint flags = 0)
        {
            this.attachments = attachments;
            this.subpasses = subpasses;
            this.dependencies = dependencies;

            unsafe
            {
                var renderPassCreateInfo = VkRenderPassCreateInfo.New();

                using NativeList<VkSubpassDescription> subPasses = new NativeList<VkSubpassDescription>((uint)subpasses.Length, (uint)subpasses.Length);

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

        protected override void Destroy(bool disposing)
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

        public AttachmentDescription(
            Format format,
            SampleCountFlags samples = SampleCountFlags.Count1,
            AttachmentLoadOp loadOp = AttachmentLoadOp.Clear,
            AttachmentStoreOp storeOp = AttachmentStoreOp.Store,
            AttachmentLoadOp stencilLoadOp = AttachmentLoadOp.DontCare,
            AttachmentStoreOp stencilStoreOp = AttachmentStoreOp.DontCare,
            ImageLayout initialLayout = ImageLayout.Undefined,
            ImageLayout finalLayout = ImageLayout.PresentSrcKHR,
            AttachmentDescriptionFlags flags =  AttachmentDescriptionFlags.None)
        {
            this.format = format;
            this.samples = samples;
            this.loadOp = loadOp;
            this.storeOp = storeOp;
            this.stencilLoadOp = stencilLoadOp;
            this.stencilStoreOp = stencilStoreOp;
            this.initialLayout = initialLayout;
            this.finalLayout = finalLayout;
            this.flags = flags;
        }
    }

    public struct AttachmentReference
    {
        public uint attachment;
        public ImageLayout layout;

        public AttachmentReference(uint attachment, ImageLayout layout)
        {
            this.attachment = attachment;
            this.layout = layout;
        }
    }

    [Flags]
    public enum SubpassDescriptionFlags
    {
        None = 0,
        PerViewAttributesNVX = 1,
        PerViewPositionXOnlyNVX = 2
    }

    public unsafe struct SubpassDescription
    {
        public SubpassDescriptionFlags flags;
        public PipelineBindPoint pipelineBindPoint;
        public AttachmentReference[] pInputAttachments;
        public AttachmentReference[] pColorAttachments;
        public AttachmentReference[] pResolveAttachments;
        public AttachmentReference[] pDepthStencilAttachment;
        public uint[]pPreserveAttachments;

        public unsafe void ToNative(VkSubpassDescription* native)
        {
            native->flags = (VkSubpassDescriptionFlags)flags;
            native->pipelineBindPoint = (VkPipelineBindPoint)pipelineBindPoint;

            if(!pInputAttachments.IsNullOrEmpty())
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

    public enum PipelineStageFlags
    {
        None = 0,
        TopOfPipe = 1,
        DrawIndirect = 2,
        VertexInput = 4,
        VertexShader = 8,
        TessellationControlShader = 16,
        TessellationEvaluationShader = 32,
        GeometryShader = 64,
        FragmentShader = 128,
        EarlyFragmentTests = 256,
        LateFragmentTests = 512,
        ColorAttachmentOutput = 1024,
        ComputeShader = 2048,
        Transfer = 4096,
        BottomOfPipe = 8192,
        Host = 16384,
        AllGraphics = 32768,
        AllCommands = 65536,
        CommandProcessNVX = 131072
    }

    public enum DependencyFlags
    {
        None = 0,
        ByRegion = 1,
        ViewLocalKHX = 2,
        DeviceGroupKHX = 4
    }

    public struct SubpassDependency
    {
        public uint srcSubpass;
        public uint dstSubpass;
        public PipelineStageFlags srcStageMask;
        public PipelineStageFlags dstStageMask;
        public AccessFlags srcAccessMask;
        public AccessFlags dstAccessMask;
        public DependencyFlags dependencyFlags;
    }

    [Flags]
    public enum AccessFlags
    {
        None = 0,
        IndirectCommandRead = 1,
        IndexRead = 2,
        VertexAttributeRead = 4,
        UniformRead = 8,
        InputAttachmentRead = 16,
        ShaderRead = 32,
        ShaderWrite = 64,
        ColorAttachmentRead = 128,
        ColorAttachmentWrite = 256,
        DepthStencilAttachmentRead = 512,
        DepthStencilAttachmentWrite = 1024,
        TransferRead = 2048,
        TransferWrite = 4096,
        HostRead = 8192,
        HostWrite = 16384,
        MemoryRead = 32768,
        MemoryWrite = 65536,
        CommandProcessReadNVX = 131072,
        CommandProcessWriteNVX = 262144,
        ColorAttachmentReadNoncoherentEXT = 524288
    }

}
