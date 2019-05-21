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

    public class FrameGraph : Resource//, IEnum
    {
        public RenderTarget[] RenderTargets { get; set; }


        public List<PassHandler> RenderPassList { get; set; } = new List<PassHandler>();

        public FrameGraph()
        {
        }

        public void AddRenderPass(PassHandler renderPass)
        {
            renderPass.FrameGraph = this;
            RenderPassList.Add(renderPass);
        }

        public void Draw(RenderView view)
        {
            foreach (var renderPass in RenderPassList)
            {
                renderPass.Draw(view);
            }
            
        }

        public void Summit(int imageIndex)
        {
            foreach (var renderPass in RenderPassList)
            {
                renderPass.Summit(imageIndex);
            }

        }

    }

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

}
