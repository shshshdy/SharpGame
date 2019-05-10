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

        protected CommandBuffer cmdBuffer_;
        //public CommandBuffer CommandBuffer => cmdBuffer_;

        internal VulkanCore.RenderPass renderPass_;

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

        public void BindVertexBuffer(GraphicsBuffer buffer, long offset = 0)
            => cmdBuffer_.CmdBindVertexBuffer(buffer, offset);

        public void BindVertexBuffers(int firstBinding, int bindingCount, GraphicsBuffer[] buffers, long[] offsets)
        {
            //VulkanCore.Buffer[] bufs = new VulkanCore.Buffer[buffers.Length];            
            //cmdBuffer_.CmdBindVertexBuffers(firstBinding, bindingCount, buffers, offsets);
        }
        public void BindIndexBuffer(GraphicsBuffer buffer, long offset = 0, IndexType indexType = IndexType.UInt32)
            => cmdBuffer_.CmdBindIndexBuffer(buffer, offset, indexType);

        public void BindGraphicsPipeline(Pipeline pipeline, Shader shader, ResourceSet resourceSet)
        {
            var pipe = pipeline.GetGraphicsPipeline(this, shader, null);
            cmdBuffer_.CmdBindPipeline(PipelineBindPoint.Graphics, pipe);
            cmdBuffer_.CmdBindDescriptorSet(PipelineBindPoint.Graphics, pipeline.pipelineLayout, resourceSet.descriptorSet);
        }

        public void BindDescriptorSet(PipelineBindPoint pipelineBindPoint, PipelineLayout layout, ResourceSet resourceSet, int? dynamicOffset = null)
            => cmdBuffer_.CmdBindDescriptorSet(pipelineBindPoint, layout, resourceSet.descriptorSet, dynamicOffset);

        public void SetScissor(Rect2D scissor)
            => cmdBuffer_.CmdSetScissor(scissor);

        public void BindDescriptorSets(PipelineBindPoint pipelineBindPoint, PipelineLayout layout, int firstSet, DescriptorSet[] descriptorSets, int[] dynamicOffsets = null)
            => cmdBuffer_.CmdBindDescriptorSets(pipelineBindPoint, layout, firstSet, descriptorSets, dynamicOffsets);

        public void DrawPrimitive(int vertexCount, int instanceCount = 1, int firstVertex = 0, int firstInstance = 0)
            => cmdBuffer_.CmdDraw(vertexCount, instanceCount, firstVertex, firstInstance);

        public void DrawIndexed(int indexCount, int instanceCount = 1, int firstIndex = 0, int vertexOffset = 0, int firstInstance = 0)
            => cmdBuffer_.CmdDrawIndexed(indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);

        public void DrawGeometry(Geometry geometry, Pipeline pipeline, Shader shader, ResourceSet resourceSet)
        {
            var pipe = pipeline.GetGraphicsPipeline(this, shader, geometry);
            cmdBuffer_.CmdBindPipeline(PipelineBindPoint.Graphics, pipe);
            cmdBuffer_.CmdBindDescriptorSet(PipelineBindPoint.Graphics, pipeline.pipelineLayout, resourceSet.descriptorSet);
            geometry.Draw(cmdBuffer_);
        }

        public void DrawBatch(ref SourceBatch batch, Pipeline pipeline, ResourceSet resourceSet)
        {
            var shader = batch.material_.Shader;
            var pipe = pipeline.GetGraphicsPipeline(this, shader, batch.geometry_);
            cmdBuffer_.CmdBindDescriptorSet(PipelineBindPoint.Graphics, pipeline.pipelineLayout, resourceSet.descriptorSet);
            cmdBuffer_.CmdBindPipeline(PipelineBindPoint.Graphics, pipe);
            batch.geometry_.Draw(cmdBuffer_);
        }

        protected virtual CommandBuffer BeginDraw()
        {
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

            cmdBuffer_.End();
            cmdBuffer_ = null;
        }

        protected virtual void OnDraw(RenderView view)
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

    public class GraphicsPass : RenderPass
    {

    }

    public class ComputePass : RenderPass
    {

    }
}
