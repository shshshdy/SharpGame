using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    public enum UniformType
    {

    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct UnifromData
    {
        [FieldOffset(0)]
        public float floatValue;
        [FieldOffset(0)]
        public Vector2 vec2Value;
        [FieldOffset(0)]
        public Vector3 vec3Value;
        [FieldOffset(0)]
        public Vector4 vec4Value;
        [FieldOffset(0)]
        public Matrix matValue;
        [FieldOffset(0)]
        public Texture texture;
        [FieldOffset(0)]
        public GraphicsBuffer buffer;
    }

    public struct ShaderParameter
    {
        public StringID name;
        public UnifromData data;
    }

    public struct TextureParameter
    {
        public StringID name;
        public Texture texture;
    }
}
