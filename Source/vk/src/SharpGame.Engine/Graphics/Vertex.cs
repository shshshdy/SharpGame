using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Vulkan;

namespace SharpGame
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PosNormTex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 texcoord;

        public PosNormTex(Vector3 p, Vector3 n, Vector2 uv)
        {
            position = p;
            normal = n;
            texcoord = uv;
        }

        public PosNormTex(
            float px, float py, float pz,
            float nx, float ny, float nz,
            float u, float v)
        {
            position = new Vector3(px, py, pz);
            normal = new Vector3(nx, ny, nz);
            texcoord = new Vector2(u, v);
        }

        public static VertexLayout Layout = new VertexLayout
        (
            new[]
            {
                new VertexInputBinding(0, (uint)Utilities.SizeOf<PosNormTex>(), VertexInputRate.Vertex)
            },
            new[]
            {
                new VertexInputAttribute(0, 0, Format.R32g32b32Sfloat, 0),
                new VertexInputAttribute(0, 1, Format.R32g32b32Sfloat, 12),
                new VertexInputAttribute(0, 2, Format.R32g32Sfloat, 24)
            }
        );
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PosColorVertex
    {
        public Vector3 Position;
        public uint Color;


        public static VertexLayout Layout = new VertexLayout
        (
            new[]
            {
                new VertexInputBinding(0, (uint)Utilities.SizeOf<PosColorVertex>(), VertexInputRate.Vertex)
            },
            new[]
            {
                new VertexInputAttribute(0, 0, Format.R32g32b32Sfloat, 0),
                new VertexInputAttribute(0, 1, Format.R8g8b8a8Unorm, 12)
            }
        );
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Pos2dTexColorVertex
    {
        public Vector2 Position;
        public Vector2 TexCoord;
        public uint Color;


        public static VertexLayout Layout = new VertexLayout
        (
            new[]
            {
                new VertexInputBinding(0, (uint)Utilities.SizeOf<Pos2dTexColorVertex>(), VertexInputRate.Vertex)
            },
            new[]
            {
                new VertexInputAttribute(0, 0, Format.R32g32Sfloat, 0),
                new VertexInputAttribute(0, 1, Format.R32g32Sfloat, 8),
                new VertexInputAttribute(0, 2, Format.R8g8b8a8Unorm, 16)
            }
        );
    }
}
