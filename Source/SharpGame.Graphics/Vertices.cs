using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;


namespace SharpGame
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPos
    {
        public vec3 Position;
        public VertexPos(float x, float y, float z)
        {
            Position.x = x;
            Position.y = y;
            Position.z = z;
        }

        public static int Size => Utilities.SizeOf<VertexPosColor>();

        public static VertexLayout Layout = new VertexLayout
        {
            new VertexAttribute(0, 0, VkFormat.R32G32B32SFloat, 0)
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPosTex
    {
        public vec3 position;
        public vec2 texcoord;

        public static int Size => Utilities.SizeOf<VertexPosTex>();

        public VertexPosTex(vec3 p, vec2 uv)
        {
            position = p;
            texcoord = uv;
        }

        public VertexPosTex(float px, float py, float pz, float u, float v)
        {
            position = new vec3(px, py, pz);
            texcoord = new vec2(u, v);
        }

        public static VertexLayout Layout = new VertexLayout
        {
            new VertexAttribute(0, 0, VkFormat.R32G32B32SFloat, 0),
            new VertexAttribute(0, 1, VkFormat.R32G32SFloat, 12),
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPosTexNorm
    {
        public vec3 position;
        public vec2 texcoord;
        public vec3 normal;

        public static int Size => Utilities.SizeOf<VertexPosTexNorm>();

        public VertexPosTexNorm(vec3 p, vec2 uv, vec3 n)
        {
            position = p;
            normal = n;
            texcoord = uv;
        }

        public VertexPosTexNorm(float px, float py, float pz, float nx, float ny, float nz, float u, float v)
        {
            position = new vec3(px, py, pz);
            texcoord = new vec2(u, v);
            normal = new vec3(nx, ny, nz);
        }

        public static VertexLayout Layout = new VertexLayout
        {
            new VertexAttribute(0, 0, VkFormat.R32G32B32SFloat, 0),
            new VertexAttribute(0, 1, VkFormat.R32G32SFloat, 12),
            new VertexAttribute(0, 2, VkFormat.R32G32B32SFloat, 20)
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPosTexNTB
    {
        public vec3 position;
        public vec2 texcoord;
        public vec3 normal;
        public vec3 tangent;
        public vec3 bitangent;

        public VertexPosTexNTB(vec3 p, vec2 uv, vec3 n, vec3 t, vec3 b)
        {
            position = p;
            normal = n;
            texcoord = uv;
            tangent = t;
            bitangent = b;
        }

        public static int Size => Utilities.SizeOf<VertexPosTexNTB>();

        public static VertexLayout Layout = new VertexLayout
        {
            new VertexAttribute(0, 0, VkFormat.R32G32B32SFloat, 0),
            new VertexAttribute(0, 1, VkFormat.R32G32SFloat, 12),
            new VertexAttribute(0, 2, VkFormat.R32G32B32SFloat, 20),
            new VertexAttribute(0, 3, VkFormat.R32G32B32SFloat, 32),
            new VertexAttribute(0, 4, VkFormat.R32G32B32SFloat, 44)
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPosTexNormTangent
    {
        public vec3 position;
        public vec2 texcoord;
        public vec3 normal;
        public vec4 tangent;

        public VertexPosTexNormTangent(vec3 p, vec2 uv, vec3 n, vec4 t)
        {
            position = p;
            normal = n;
            texcoord = uv;
            tangent = t;
        }

        public static int Size => Utilities.SizeOf<VertexPosTexNormTangent>();

        public static VertexLayout Layout = new VertexLayout
        {
            new VertexAttribute(0, 0, VkFormat.R32G32B32SFloat, 0),
            new VertexAttribute(0, 1, VkFormat.R32G32SFloat, 12),
            new VertexAttribute(0, 2, VkFormat.R32G32B32SFloat, 20),
            new VertexAttribute(0, 3, VkFormat.R32G32B32A32SFloat, 32)
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPosTexNormColor
    {
        public vec3 position;
        public vec2 texcoord;
        public vec3 normal;
        public Color color;

        public static int Size => Utilities.SizeOf<VertexPosTexNormColor>();

        public VertexPosTexNormColor(vec3 p, vec3 n, vec2 uv, Color color)
        {
            position = p;
            normal = n;
            texcoord = uv;
            this.color = color;
        }

        public static VertexLayout Layout = new VertexLayout
        {
            new VertexAttribute(0, 0, VkFormat.R32G32B32SFloat, 0),
            new VertexAttribute(0, 1, VkFormat.R32G32SFloat, 8),
            new VertexAttribute(0, 2, VkFormat.R32G32B32SFloat, 20),
            new VertexAttribute(0, 3, VkFormat.R8G8B8A8UNorm, 32)
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPosColor
    {
        public vec3 Position;
        public uint Color;

        public static int Size => Utilities.SizeOf<VertexPosColor>();

        public static VertexLayout Layout = new VertexLayout
        {
            new VertexAttribute(0, 0, VkFormat.R32G32B32SFloat, 0),
            new VertexAttribute(0, 1, VkFormat.R8G8B8A8UNorm, 12)
        };
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPos2dTexColor
    {
        public vec2 Position;
        public vec2 TexCoord;
        public uint Color;

        public static int Size => Utilities.SizeOf<VertexPos2dTexColor>();

        public static VertexLayout Layout = new VertexLayout
        {
            new VertexAttribute(0, 0, VkFormat.R32G32SFloat, 0),
            new VertexAttribute(0, 1, VkFormat.R32G32SFloat, 8),
            new VertexAttribute(0, 2, VkFormat.R8G8B8A8UNorm, 16)
        };
    }
}
