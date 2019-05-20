﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Vulkan;

namespace SharpGame
{

    public class Material : Resource
    {
        public string Name { get; set; }
        public string ShaderName { get; set; }

        public FastList<ShaderParameter> ShaderParameters { get; set; } = new FastList<ShaderParameter>();
        public FastList<TexureParameter> TextureParameters { get; set; } = new FastList<TexureParameter>();

        private Shader shader_;
        public Shader Shader
        {
            get => shader_;
            set
            {
                shader_ = value;
                resourceSet_ = new ResourceSet(shader_.Main.ResourceLayout);
            }
        }

        private ResourceSet resourceSet_;
        public ResourceSet ResourceSet => resourceSet_;

        internal MaterialRenderData materialRenderData;

        public Material()
        {
        }

        protected override void OnBuild()
        {
            base.OnBuild();

            Shader = ResourceCache.Instance.Load<Shader>(ShaderName);
            //materialRenderData = new MaterialRenderData();
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

        public void SetUniform<T>(StringID name, T val)
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

        public void SetTexture(StringID name, Texture tex)
        {
            for (int i = 0; i < TextureParameters.Count; i++)
            {
                ref TexureParameter param = ref TextureParameters.At(i);
                if (param.name == name)
                {
                    param.texture = tex;
                }
            }

            TextureParameters.Add(new TexureParameter { name = name, texture = tex });

        }

    }

    public class MaterialRenderData
    {
        public Shader shader;
        public ResourceSet[] resourceSets;

    }
}