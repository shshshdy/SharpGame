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
        Mat4

    }

    public struct ShaderParameter
    {
        public StringID name;
        public object data;
        
        public bool IsNull => name.IsNullOrEmpty;

        public static ShaderParameter Null = new ShaderParameter();
    }

    public struct TexureParameter
    {
        public StringID name;
        public Texture texture;
        public Vector4 uvOffset;

        public bool IsNull => name.IsNullOrEmpty;

        public static TexureParameter Null = new TexureParameter();
    }

}
