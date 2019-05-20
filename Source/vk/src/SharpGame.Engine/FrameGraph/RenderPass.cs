using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;


namespace SharpGame
{
    using vec2 = Vector2;
    using vec3 = Vector3;
    using vec4 = Vector4;
    using mat4 = Matrix;

    [StructLayout(LayoutKind.Sequential)]
    public struct FrameUniform
    {
        public float DeltaTime;
        public float ElapsedTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraVS
    {
        public vec3 CameraPos;
        public float NearClip;
        public float FarClip;
        public vec4 DepthMode;
        public vec3 FrustumSize;
        public vec4 GBufferOffsets;
        public mat4 View;
        public mat4 ViewInv;
        public mat4 ViewProj;
        public vec4 ClipPlane;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialVS
    {
        public vec4 UOffset;
        public vec4 VOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectVS
    {
        public mat4 Model;
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

    public class RenderPass : Object
    {
        private StringID name_;
        public StringID Name
        {
            get => name_;
            set
            {
                name_ = value;
                passID = Pass.GetID(name_);
            }
        }

        public int passID;

        [IgnoreDataMember]
        public FrameGraph RenderPath { get; set; }

        [IgnoreDataMember]
        public Framebuffer[] framebuffer_;

        protected CommandBuffer[] cmdBuffers_ = new CommandBuffer[2];

        protected CommandBuffer cmdBuffer_;
        //public CommandBuffer CommandBuffer => cmdBuffer_;

        internal Vulkan.VkRenderPass renderPass_;

        public RenderPass()
        {
        }

        public void DrawBatch(SourceBatch batch, Pipeline pipeline, ResourceSet resourceSet)
        {/*
            var shader = batch.material_.Shader;
            var pipe = pipeline.GetGraphicsPipeline(this, shader, batch.geometry_);
            cmdBuffer_.CmdBindDescriptorSet(PipelineBindPoint.Graphics, pipeline.pipelineLayout, resourceSet.descriptorSet);
            cmdBuffer_.CmdBindPipeline(PipelineBindPoint.Graphics, pipe);
            batch.geometry_.Draw(cmdBuffer_);*/
        }

        protected virtual CommandBuffer BeginDraw()
        {
            /*
            int workContext = Graphics.WorkContext;

            CommandBufferInheritanceInfo inherit = new CommandBufferInheritanceInfo
            {
                Framebuffer = framebuffer_[workContext],
                RenderPass = renderPass_
            };

            cmdBuffer_ = Graphics.SecondaryCmdBuffers[workContext].Get();
            cmdBuffer_.Begin(new CommandBufferBeginInfo(CommandBufferUsages.OneTimeSubmit | CommandBufferUsages.RenderPassContinue
                | CommandBufferUsages.SimultaneousUse, inherit));

            this.SendGlobalEvent(new BeginRenderPass { renderPass = this});

            //System.Diagnostics.Debug.Assert(cmdBuffers_[imageIndex] == null);
            cmdBuffers_[workContext] = cmdBuffer_;
            */
            return cmdBuffer_;
        }

        public void Draw(RenderView view)
        {
            BeginDraw();

            OnDraw(view);

            EndDraw();
        }

        protected virtual void EndDraw()
        {
            this.SendGlobalEvent(new EndRenderPass { renderPass = this });

            cmdBuffer_?.End();
            cmdBuffer_ = null;
        }

        protected virtual void OnDraw(RenderView view)
        {
        }

        public void Summit(int imageIndex)
        {
            /*
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
               // cmdBuffers_[imageIndex].Reset();
                cmdBuffers_[imageIndex] = null;
            }

            cmdBuffer.CmdEndRenderPass();*/
        }
    }


}
