using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    using vec2 = Vector2;
    using vec3 = Vector3;
    using vec4 = Vector4;
    using mat4 = Matrix;

    [StructLayout(LayoutKind.Sequential)]
    public struct FrameUniform
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

    public class RenderPass : GPUObject
    {
        public StringID Name { get; set; }

        [IgnoreDataMember]
        public RenderPath RenderPath { get; set; }

        [IgnoreDataMember]
        public Framebuffer[] framebuffer_;

        public Renderer Renderer => Get<Renderer>();
        public ResourceCache ResourceCache => Get<ResourceCache>();

        protected CommandBuffer[] cmdBuffers_ = new CommandBuffer[2];

        internal VulkanCore.RenderPass renderPass_;

        protected Pipeline pipeline_;


        public RenderPass()
        {
        }

        protected override void Recreate()
        {
        }

        public Framebuffer CreateFramebuffer(ImageView[] attachments, int width, int height, int layers = 1)
        {
            return renderPass_.CreateFramebuffer(
                    new FramebufferCreateInfo(attachments, width, height)
                );
        }


        public void DrawBatch(CommandBuffer cmdBuffer, ref SourceBatch batch, ResourceSet descriptorSet)
        {
            var shader = batch.material_.Shader;
            var pipeline = pipeline_.GetGraphicsPipeline(this, shader, batch.geometry_);
            cmdBuffer.CmdBindDescriptorSet(PipelineBindPoint.Graphics, pipeline_.pipelineLayout, descriptorSet.descriptorSet);
            cmdBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, pipeline);
            batch.geometry_.Draw(cmdBuffer);
        }

        protected virtual CommandBuffer BeginDraw()
        {
            int workContext = Graphics.WorkContext;

            CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
            {
                Framebuffer = framebuffer_[workContext],
                RenderPass = renderPass_
            };

            CommandBuffer cmdBuffer = Graphics.SecondaryCmdBuffers[workContext].Get();
            cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.OneTimeSubmit | CommandBufferUsages.RenderPassContinue
                | CommandBufferUsages.SimultaneousUse, inherit));

            this.SendGlobalEvent(new BeginRenderPass { renderPass = this, commandBuffer = cmdBuffer });

            //System.Diagnostics.Debug.Assert(cmdBuffers_[imageIndex] == null);
            cmdBuffers_[workContext] = cmdBuffer;

            return cmdBuffer;
        }

        public void Draw(RenderView view)
        {
            CommandBuffer cmdBuffer = BeginDraw();

            OnDraw(view, cmdBuffer);

            EndDraw(cmdBuffer);
        }

        protected virtual void EndDraw(CommandBuffer cmdBuffer)
        {
            this.SendGlobalEvent(new EndRenderPass { renderPass = this, commandBuffer = cmdBuffer });

            cmdBuffer.End();
        }

        protected virtual void OnDraw(RenderView view, CommandBuffer cmdBuffer)
        {
        }

        public void Summit(int imageIndex)
        {
            CommandBuffer cmdBuffer = Graphics.PrimaryCmdBuffers[imageIndex];

            var renderPassBeginInfo = new RenderPassBeginInfo
            (
                framebuffer_[imageIndex], new Rect2D(Offset2D.Zero, new Extent2D(Graphics.Width, Graphics.Height)),
                new ClearColorValue(new ColorF4(0.0f, 0.0f, 0.0f, 1.0f)),
                new ClearDepthStencilValue(1.0f, 0)
            );

            cmdBuffer.CmdBeginRenderPass(renderPassBeginInfo, SubpassContents.SecondaryCommandBuffers);
            if (cmdBuffers_[imageIndex] != null)
            {
                cmdBuffer.CmdExecuteCommand(cmdBuffers_[imageIndex]);
                cmdBuffers_[imageIndex] = null;
            }

            cmdBuffer.CmdEndRenderPass();
        }
    }
}
