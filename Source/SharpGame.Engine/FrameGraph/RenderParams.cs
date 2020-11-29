using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FrameUniform
    {
        public float DeltaTime;
        public float ElapsedTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraVS
    {
        public mat4 View;
        public mat4 ViewInv;
        public mat4 Proj;
        public mat4 ProjInv;
        public mat4 ViewProj;
        public mat4 ViewProjInv;
        public vec3 CameraPos;
        float pading1;
        public vec3 CameraDir;
        float pading2;
        public vec2 GBufferInvSize;
        public float NearClip;
        public float FarClip;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct LightParameter
    {
        public Color4 AmbientColor;
        public Color4 SunlightColor;
        public vec3 SunlightDir;
        public float LightPS_pading1;

        public vec4 cascadeSplits;
        public FixedArray4<mat4> lightMatrices;

        public FixedArray8<Color4> lightColor;
        public FixedArray8<vec4> lightVec;

        public void SetLightMatrices(int index, ref mat4 mat)
        {
            lightMatrices[index] = mat;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectVS
    {
        public mat4 Model;

        public vec4 UOffset1;
        public vec4 VOffset1;
        public vec4 UOffset2;
        public vec4 VOffset2;
    };

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SkinVS
    {
        fixed float SkinMatrices[16 * 64];
    }
}
