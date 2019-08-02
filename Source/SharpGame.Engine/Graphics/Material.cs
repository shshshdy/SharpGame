using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Vulkan;

namespace SharpGame
{

    public class Material : Resource<Material>
    {
        public ResourceRef PipelineName { get; set; }

        public FastList<ShaderParameter> ShaderParameters { get; set; } = new FastList<ShaderParameter>();
        public FastList<TexureParameter> TextureParameters { get; set; } = new FastList<TexureParameter>();

        private ResourceSet resourceSet;
        public ResourceSet ResourceSet { get => resourceSet; set => resourceSet = value; }

        public GraphicsPipeline Pipeline { get; set; }

        public Material()
        {
        }

        public Material(string pipeline)
        {
            PipelineName = new ResourceRef("GraphicsPipeline", pipeline);
            OnBuild();
        }

        public Material(GraphicsPipeline pipeline)
        {
            Pipeline = pipeline;
            OnBuild();
        }

        protected override bool OnBuild()
        {
            if(PipelineName != null)
            {
                Pipeline = Resources.Instance.Load<GraphicsPipeline>(PipelineName);
            }
            resourceSet = new ResourceSet(Pipeline.ResourceLayout[1]);
            return Pipeline != null;
        }

        public ref ShaderParameter GetShaderParameter(StringID name)
        {
            for(int i = 0; i < ShaderParameters.Count; i++)
            {
                ref ShaderParameter param = ref ShaderParameters.At(i);
                if(param.name == name)
                {
                    return ref param;
                }
            }

            return ref ShaderParameter.Null;
        }

        public void SetShaderParameter<T>(StringID name, T val)
        {
            ref ShaderParameter param = ref GetShaderParameter(name);
            if (!param.IsNull)
            {
                param.SetValue(val);
            }
            else
            {
                var shaderParam = new ShaderParameter
                {
                    name = name
                };
                shaderParam.SetValue(val);
                ShaderParameters.Add(shaderParam);
            }
        }

        public ref TexureParameter GetTextureParameter(StringID name)
        {
            for (int i = 0; i < ShaderParameters.Count; i++)
            {
                ref TexureParameter param = ref TextureParameters.At(i);
                if (param.name == name)
                {
                    return ref param;
                }
            }

            return ref TexureParameter.Null;
        }

        public void SetTexture(StringID name, ResourceRef texRef)
        {
            for (int i = 0; i < TextureParameters.Count; i++)
            {
                ref TexureParameter param = ref TextureParameters.At(i);
                if (param.name == name)
                {
                    param.texture = texRef;
                    break;
                }
            }

            TextureParameters.Add(new TexureParameter { name = name, texture = texRef });

        }

        public void SetTexture(StringID name, Texture tex)
        {
            for (int i = 0; i < TextureParameters.Count; i++)
            {
                ref TexureParameter param = ref TextureParameters.At(i);
                if (param.name == name)
                {
                    param.texture = tex.ResourceRef;
                }
            }

            TextureParameters.Add(new TexureParameter { name = name, texture = tex.ResourceRef });

        }

    }

}
