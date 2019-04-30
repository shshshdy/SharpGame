using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct ShaderParameter
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


    public class Material : Resource
    {
        public string Name { get; set; }
        public string Shader { get; set; }

        public Dictionary<string, ShaderParameter> UniformData = new Dictionary<string, ShaderParameter>();
        public Dictionary<string, Texture> TextureData = new Dictionary<string, Texture>();

        public Material()
        {
        }




    }
}
