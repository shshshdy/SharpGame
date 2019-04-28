using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct MaterialParam
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
    }


    public class Material : Resource
    {
        public string Name { get; set; }

        public Dictionary<string, MaterialParam> uniformData;
        public Dictionary<string, Texture> textureData;

        public Material()
        {
            
        }
    }
}
