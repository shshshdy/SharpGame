using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public class RenderPass : DisposeBase
    {
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
        public uint inputAttachmentCount;
        public AttachmentReference* pInputAttachments;
        public uint colorAttachmentCount;
        public AttachmentReference* pColorAttachments;
        public AttachmentReference* pResolveAttachments;
        public AttachmentReference* pDepthStencilAttachment;
        public uint preserveAttachmentCount;
        public uint* pPreserveAttachments;
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

    public struct RenderPassCreateInfo
    {
        public uint flags;
        public AttachmentDescription[] pAttachments;
        public SubpassDescription[] pSubpasses;
        public SubpassDependency[] pDependencies;

        public unsafe void ToNative(out VkRenderPassCreateInfo native)
        {
            native = VkRenderPassCreateInfo.New();
            native.flags = flags;
            native.attachmentCount = (uint)pAttachments.Length;
            native.pAttachments = (VkAttachmentDescription*)Unsafe.AsPointer(ref pAttachments[0]);
            native.subpassCount = (uint)pSubpasses.Length;
            native.pSubpasses = (VkSubpassDescription*)Unsafe.AsPointer(ref pSubpasses[0]);
            native.dependencyCount = (uint)pDependencies.Length;
            native.pDependencies = (VkSubpassDependency*)Unsafe.AsPointer(ref pDependencies[0]);
        }
    }
}
