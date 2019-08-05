using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{

    public enum UniformType
    {
        Bool,
        Int,
        Float,
        Vec2,
        Vec3,
        Vec4,
        Color,
        Mat3,
        Mat4,
        Buffer,
        Sampler,
        Texture

    }

    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct UniformData
    {
        [FieldOffset(0)]
        public bool boolVal;
        [FieldOffset(0)]
        public int intVal;
        [FieldOffset(0)]
        public float floatVal;
        [FieldOffset(0)]
        public Vector2 vec2Val;
        [FieldOffset(0)]
        public Vector3 vec3Val;
        [FieldOffset(0)]
        public Vector4 vec4Val;
        [FieldOffset(0)]
        public Color colorVal;
        //[FieldOffset(0)]
        //public GraphicsBuffer buffer;
        //[FieldOffset(0)]
        //public Vulkan.Sampler sampler;
        //[FieldOffset(0)]
        //public Texture texture;
    }

    public struct TexureParameter
    {
        public StringID name;
        public ResourceRef texture;
        public Vector4 uvOffset;

        public bool IsNull => name.IsNullOrEmpty;

        public static TexureParameter Null = new TexureParameter();
    }

    public struct BufferParameter
    {
        public StringID name;
        public DeviceBuffer buffer;

        public bool IsNull => name.IsNullOrEmpty;

    }

    public struct ShaderParameter
    {
        public StringID name;
        public UniformType uniformType;
        public UniformData data;

        internal int set;
        internal int binding;
        internal ResourceSet resourceSet;

        public bool IsNull => name.IsNullOrEmpty;

        public static ShaderParameter Null = new ShaderParameter();

        public void SetValue<T>(T val)
        {
            switch (val)
            {
                case bool boolVal:
                    uniformType = UniformType.Bool;
                    data.boolVal = boolVal;
                    break;
                case int intVal:
                    uniformType = UniformType.Int;
                    data.intVal = intVal;
                    break;
                case float floatVal:
                    uniformType = UniformType.Float;
                    data.floatVal = floatVal;
                    break;
                case Vector2 vec2Val:
                    uniformType = UniformType.Vec2;
                    data.vec2Val = vec2Val;
                    break;
                case Vector3 vec3Val:
                    uniformType = UniformType.Vec3;
                    data.vec3Val = vec3Val;
                    break;
                case Vector4 vec4Val:
                    uniformType = UniformType.Vec4;
                    data.vec4Val = vec4Val;
                    break;
                case Color colorVal:
                    uniformType = UniformType.Color;
                    data.colorVal = colorVal;
                    break;
                case Texture texture:
                    uniformType = UniformType.Texture;
                    //data.texture = texture;
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }

        }
    }


}
