using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
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
    }

    public struct TextureParameter
    {
        public string name;
        public ResourceRef texture;
        public Vector4 uvOffset;

        [IgnoreDataMember]
        public bool IsNull => string.IsNullOrEmpty(name);

        public static TextureParameter Null = new TextureParameter();
    }

    public struct BufferParameter
    {
        public string name;
        public DeviceBuffer buffer;

        [IgnoreDataMember]
        public bool IsNull => string.IsNullOrEmpty(name);
    }

    public struct ShaderParameter
    {
        public string name;
        public UniformType uniformType;
        public UniformData data;
        internal IntPtr addr;

        [IgnoreDataMember]
        public bool IsNull => string.IsNullOrEmpty(name);

        [IgnoreDataMember]
        public bool IsBinded => addr != IntPtr.Zero;

        public static ShaderParameter Null = new ShaderParameter();
        public void SetValue<T>(ref T val)
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
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }

            if(IsBinded)
            {
                WriteData();
            }
        }

        public void Bind(IntPtr intPtr)
        {
            addr = intPtr;
            WriteData();
        }

        void WriteData()
        {
            switch (uniformType)
            {
                case UniformType.Bool:
                    Utilities.Write(addr, ref data.boolVal);
                    break;
                case UniformType.Int:
                    Utilities.Write(addr, ref data.intVal);
                    break;
                case UniformType.Float:
                    Utilities.Write(addr, ref data.floatVal);
                    break;
                case UniformType.Vec2:
                    Utilities.Write(addr, ref data.vec2Val);
                    break;
                case UniformType.Vec3:
                    Utilities.Write(addr, ref data.vec3Val);
                    break;
                case UniformType.Vec4:
                    Utilities.Write(addr, ref data.vec4Val);
                    break;
                case UniformType.Color:
                    Utilities.Write(addr, ref data.colorVal);
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }
        }
    }


}
