using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using VulkanCore;

namespace SharpGame
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PosNormTex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;

        public PosNormTex(Vector3 p, Vector3 n, Vector2 uv)
        {
            Position = p;
            Normal = n;
            TexCoord = uv;
        }

        public PosNormTex(
            float px, float py, float pz,
            float nx, float ny, float nz,
            float u, float v)
        {
            Position = new Vector3(px, py, pz);
            Normal = new Vector3(nx, ny, nz);
            TexCoord = new Vector2(u, v);
        }

        public static PipelineVertexInputStateCreateInfo Layout = new PipelineVertexInputStateCreateInfo
        (
            new[]
            {
                new VertexInputBindingDescription(0, Interop.SizeOf<PosNormTex>(), VertexInputRate.Vertex)
            },
            new[]
            {
                new VertexInputAttributeDescription(0, 0, Format.R32G32B32SFloat, 0),
                new VertexInputAttributeDescription(1, 0, Format.R32G32B32SFloat, 12),
                new VertexInputAttributeDescription(2, 0, Format.R32G32SFloat, 24)
            }
        );
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PosColorVertex
    {
        public Vector3 Position;
        public uint Color;


        public static PipelineVertexInputStateCreateInfo Layout = new PipelineVertexInputStateCreateInfo
        (
            new[]
            {
                new VertexInputBindingDescription(0, Interop.SizeOf<PosColorVertex>(), VertexInputRate.Vertex)
            },
            new[]
            {
                new VertexInputAttributeDescription(0, 0, Format.R32G32B32SFloat, 0),
                new VertexInputAttributeDescription(2, 0, Format.R8G8B8A8UNorm, 12)
            }
        );
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Pos2dTexColorVertex
    {
        public Vector2 Position;
        public Vector2 TexCoord;
        public uint Color;


        public static PipelineVertexInputStateCreateInfo Layout = new PipelineVertexInputStateCreateInfo
        (
            new[]
            {
                new VertexInputBindingDescription(0, Interop.SizeOf<Pos2dTexColorVertex>(), VertexInputRate.Vertex)
            },
            new[]
            {
                new VertexInputAttributeDescription(0, 0, Format.R32G32SFloat, 0),
                new VertexInputAttributeDescription(1, 0, Format.R32G32SFloat, 8),
                new VertexInputAttributeDescription(2, 0, Format.R8G8B8A8UNorm, 16)
            }
        );
    }
}
