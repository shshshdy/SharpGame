using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GlobalUBO
    {
        public mat4 View;
        public mat4 InvView;
        public mat4 Proj;
        public mat4 InvProj;
        public mat4 ViewProj;
        public mat4 InvViewProj;
        public vec3 CameraPos;
        public float NearClip;
        public vec3 CameraDir;
        public float FarClip;
        public vec2 ViewportSize;
        public float Time;

    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct LightUBO
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
