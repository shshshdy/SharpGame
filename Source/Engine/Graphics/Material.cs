using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGame
{
    

    public class Material : Resource
    {
        public string Name { get; set; }
        public string Shader { get; set; }

        public FastList<ShaderParameter> ShaderParameters { get; set; } = new FastList<ShaderParameter>();
        public FastList<TextureParameter> TextureParameters { get; set; } = new FastList<TextureParameter>();
        
        public Material()
        {
        }
        /*
        public ref ShaderParameter GetShaderParameter(StringID name)
        {
            for(int i = 0; i < UniformData.Count; i++)
            {
                ref ShaderParameter uniform = ref UniformData.At(i);
                if(uniform.name == name)
                {
                    return ref uniform;
                }
            }

            //return default;
        }*/

        public void SetShaderParameter(StringID name, Vector2 vec2)
        {
            for (int i = 0; i < ShaderParameters.Count; i++)
            {
                ref ShaderParameter uniform = ref ShaderParameters.At(i);
                if (uniform.name == name)
                {
                    uniform.data.vec2Value = vec2;
                }
            }

            ShaderParameters.Add(new ShaderParameter { name = name, data = new UnifromData { vec2Value = vec2 } });

        }


    }
}
