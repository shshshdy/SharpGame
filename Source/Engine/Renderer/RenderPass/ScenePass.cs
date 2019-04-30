using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using VulkanCore;


namespace SharpGame
{
    using vec2 = Vector2;
    using vec3 = Vector3;
    using vec4 = Vector4;
    using mat4 = Matrix;

    public class ScenePass : RenderPass
    {
        public AttachmentDescription[] attachments { get; set; }
        public SubpassDescription[] subpasses { get; set; }

        private Pipeline pipeline_;
        private DescriptorSetLayout _descriptorSetLayout;
        private DescriptorPool _descriptorPool;
        private DescriptorSet _descriptorSet;

        public ScenePass(string name = "main")
        {
            Name = name;
            Recreate();
        }

        protected override void Recreate()
        {
            var renderer = Get<Renderer>();
            attachments = new[]
            {
                // Color attachment.
                new AttachmentDescription
                {
                    Format = Graphics.Swapchain.Format,
                    Samples = SampleCounts.Count1,
                    LoadOp = AttachmentLoadOp.Clear,
                    StoreOp = AttachmentStoreOp.Store,
                    StencilLoadOp = AttachmentLoadOp.DontCare,
                    StencilStoreOp = AttachmentStoreOp.DontCare,
                    InitialLayout = ImageLayout.Undefined,
                    FinalLayout = ImageLayout.PresentSrcKhr
                },
                // Depth attachment.
                new AttachmentDescription
                {
                    Format = Graphics.DepthStencilBuffer.Format,
                    Samples = SampleCounts.Count1,
                    LoadOp = AttachmentLoadOp.Clear,
                    StoreOp = AttachmentStoreOp.DontCare,
                    StencilLoadOp = AttachmentLoadOp.DontCare,
                    StencilStoreOp = AttachmentStoreOp.DontCare,
                    InitialLayout = ImageLayout.Undefined,
                    FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
                }
            };

            subpasses = new[]
            {
                new SubpassDescription(
                    new[] { new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal) },
                    new AttachmentReference(1, ImageLayout.DepthStencilAttachmentOptimal))
            };

            var dependencies = new[]
            {
                new SubpassDependency
                {
                    SrcSubpass = Constant.SubpassExternal,
                    DstSubpass = 0,
                    SrcStageMask = PipelineStages.BottomOfPipe,
                    DstStageMask = PipelineStages.ColorAttachmentOutput,
                    SrcAccessMask = Accesses.MemoryRead,
                    DstAccessMask = Accesses.ColorAttachmentRead | Accesses.ColorAttachmentWrite,
                    DependencyFlags = Dependencies.ByRegion
                },
                new SubpassDependency
                {
                    SrcSubpass = 0,
                    DstSubpass = Constant.SubpassExternal,
                    SrcStageMask = PipelineStages.ColorAttachmentOutput,
                    DstStageMask = PipelineStages.BottomOfPipe,
                    SrcAccessMask = Accesses.ColorAttachmentRead | Accesses.ColorAttachmentWrite,
                    DstAccessMask = Accesses.MemoryRead,
                    DependencyFlags = Dependencies.ByRegion
                }
            };

            var createInfo = new RenderPassCreateInfo(subpasses, attachments, dependencies);
            renderPass_ = Graphics.ToDisposeFrame(Graphics.Device.CreateRenderPass(createInfo));
            framebuffer_ = Graphics.ToDisposeFrame(CreateFramebuffers());

        }

        protected Framebuffer[] CreateFramebuffers()
        {
            var framebuffers = new Framebuffer[Graphics.SwapchainImages.Length];
            for (int i = 0; i < Graphics.SwapchainImages.Length; i++)
            {
                framebuffers[i] = CreateFramebuffer(
                    new[] {
                        Graphics.SwapchainImageViews[i], Graphics.DepthStencilBuffer.View
                    },

                    Graphics.Width, Graphics.Height
                );
            }

            return framebuffers;
        }


    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FrameVS
    {
        public float cDeltaTime;
        public float cElapsedTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraVS
    {
        public vec3 cCameraPos;
        public float cNearClip;
        public float cFarClip;
        public vec4 cDepthMode;
        public vec3 cFrustumSize;
        public vec4 cGBufferOffsets;
        public mat4 cView;
        public mat4 cViewInv;
        public mat4 cViewProj;
        public vec4 cClipPlane;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ZoneVS
    {
        public vec3 cAmbientStartColor;
        public vec3 cAmbientEndColor;
        public mat4 cZone;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LightVS
    {
        public vec4 cLightPos;
        public vec3 cLightDir;
        public vec4 cNormalOffsetScale;
        // public mat4 cLightMatrices [4];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialVS
    {
        public vec4 cUOffset;
        public vec4 cVOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectVS
    {
        public mat4 cModel;
        //mat3 cBillboardRot;

        //vec4 cSkinMatrices [64*3];
    };

    // Pixel shader uniforms
    [StructLayout(LayoutKind.Sequential)]
    public struct FramePS
    {
        public float cDeltaTimePS;
        public float cElapsedTimePS;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraPS
    {
        public vec3 cCameraPosPS;
        public vec4 cDepthReconstruct;
        public vec2 cGBufferInvSize;
        public float cNearClipPS;
        public float cFarClipPS;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ZonePS
    {
        public vec4 cAmbientColor;
        public vec4 cFogParams;
        public vec3 cFogColor;
        public vec3 cZoneMin;
        public vec3 cZoneMax;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LightPS
    {
        public vec4 cLightColor;
        public vec4 cLightPosPS;
        public vec3 cLightDirPS;
        public vec4 cNormalOffsetScalePS;
        public vec4 cShadowCubeAdjust;
        public vec4 cShadowDepthFade;
        public vec2 cShadowIntensity;
        public vec2 cShadowMapInvSize;
        public vec4 cShadowSplits;
        /*
        mat4 cLightMatricesPS [4];
        */
        //    vec2 cVSMShadowParams;

        public float cLightRad;
        public float cLightLength;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialPS
    {
        public vec4 cMatDiffColor;
        public vec3 cMatEmissiveColor;
        public vec3 cMatEnvMapColor;
        public vec4 cMatSpecColor;
        public float cRoughness;
        public float cMetallic;
    }

}
