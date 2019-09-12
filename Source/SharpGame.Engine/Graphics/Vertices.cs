using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPosNormTex
    {
        public vec3 position;
        public vec2 texcoord;
        public vec3 normal;

        public static int Size => Utilities.SizeOf<VertexPosNormTex>();

        public VertexPosNormTex(vec3 p, vec2 uv, vec3 n)
        {
            position = p;
            normal = n;
            texcoord = uv;
        }

        public VertexPosNormTex(float px, float py, float pz, float nx, float ny, float nz, float u, float v)
        {
            position = new vec3(px, py, pz);
            texcoord = new vec2(u, v);
            normal = new vec3(nx, ny, nz);
        }

        public static VertexLayout Layout = new VertexLayout
        (
            new VertexInputAttribute(0, 0, Format.R32g32b32Sfloat, 0),
            new VertexInputAttribute(0, 1, Format.R32g32Sfloat, 12),
            new VertexInputAttribute(0, 2, Format.R32g32b32Sfloat, 20)
        );
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPosTBNTex
    {
        public vec3 position;
        public vec2 texcoord;
        public vec3 normal;
        public vec3 tangent;
        public vec3 bitangent;

        public static int Size => Utilities.SizeOf<VertexPosTBNTex>();

        public static VertexLayout Layout = new VertexLayout
        (
            new VertexInputAttribute(0, 0, Format.R32g32b32Sfloat, 0),
            new VertexInputAttribute(0, 1, Format.R32g32Sfloat, 12),
            new VertexInputAttribute(0, 2, Format.R32g32b32Sfloat, 20),
            new VertexInputAttribute(0, 3, Format.R32g32b32Sfloat, 32),
            new VertexInputAttribute(0, 4, Format.R32g32b32Sfloat, 44)            
        );
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPosNormTexColor
    {
        public vec3 position;
        public vec2 texcoord;
        public vec3 normal;
        public Color color;

        public static int Size => Utilities.SizeOf<VertexPosNormTexColor>();

        public VertexPosNormTexColor(vec3 p, vec3 n, vec2 uv, Color color)
        {
            position = p;
            normal = n;
            texcoord = uv;
            this.color = color;
        }

        public static VertexLayout Layout = new VertexLayout
        (
            new VertexInputAttribute(0, 0, Format.R32g32b32Sfloat, 0),
            new VertexInputAttribute(0, 1, Format.R32g32Sfloat, 8),
            new VertexInputAttribute(0, 2, Format.R32g32b32Sfloat, 20),
            new VertexInputAttribute(0, 3, Format.R8g8b8a8Unorm, 32)
        );
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPosColor
    {
        public vec3 Position;
        public uint Color;

        public static int Size => Utilities.SizeOf<VertexPosColor>();

        public static VertexLayout Layout = new VertexLayout
        (
            new VertexInputAttribute(0, 0, Format.R32g32b32Sfloat, 0),
            new VertexInputAttribute(0, 1, Format.R8g8b8a8Unorm, 12)
         );
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPos2dTexColor
    {
        public vec2 Position;
        public vec2 TexCoord;
        public uint Color;

        public static int Size => Utilities.SizeOf<VertexPos2dTexColor>();

        public static VertexLayout Layout = new VertexLayout
        (
            new VertexInputAttribute(0, 0, Format.R32g32Sfloat, 0),
            new VertexInputAttribute(0, 1, Format.R32g32Sfloat, 8),
            new VertexInputAttribute(0, 2, Format.R8g8b8a8Unorm, 16)
        );
    }
}
