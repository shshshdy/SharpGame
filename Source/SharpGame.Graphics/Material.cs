using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGame
{
    public enum BlendFlags : int
    {
        Solid = 1,
        AlphaTest = 2,
        AlphaBlend = 4,
        All = 7
    }

    public class Material : Resource
    {
        public string Name { get; set; }
        public ResourceRef ShaderResource { get; set; }
        public FastList<ShaderParameter> ShaderParameters { get; set; }
        public FastList<TextureParameter> TextureParameters { get; set; }
        public FastList<BufferParameter> BufferParameters { get; set; }

        public BlendFlags BlendType
        {
            get => (BlendFlags)(1 << blendType);
            set
            {
                switch (value)
                {
                    case BlendFlags.Solid:
                        blendType = 0;
                        break;
                    case BlendFlags.AlphaTest:
                        blendType = 1;
                        break;
                    case BlendFlags.AlphaBlend:
                        blendType = 2;
                        break;
                }
            }
        }
        public int blendType { get; private set; }

        [IgnoreDataMember]
        public Shader Shader { get => shader; set => SetShader(value); }
        private Shader shader;

        [IgnoreDataMember]
        public PipelineResourceSet[] PipelineResourceSet => pipelineResourceSet;
        PipelineResourceSet[] pipelineResourceSet;

        public Material()
        {
        }

        public Material(string shader)
        {
            ShaderResource = ResourceRef.Create<Shader>(shader);

            OnBuild();
        }

        public Material(Shader shader)
        {
            SetShader(shader);
        }

        protected override bool OnBuild()
        {
            if(ShaderResource != ResourceRef.Null)
            {
                Shader = ShaderResource.Load<Shader>();
            }

            return true;
        }

        public void SetShader(Shader shader)
        {
            if (shader == null)
            {
                return;
            }

            if (shader.Pass.Count <= 0)
            {
                return;
            }

            this.shader = shader;

            if(!pipelineResourceSet.IsNullOrEmpty())
            {
                foreach(var prs in pipelineResourceSet)
                    prs?.Dispose();
            }

            pipelineResourceSet = new PipelineResourceSet[shader.Pass.Count];

            for (int i = 0; i < shader.Pass.Count; i++)
            {
                var pass = shader.Pass[i];
                pipelineResourceSet[i] = new PipelineResourceSet(pass.PipelineLayout);
            }

            if (ShaderParameters != null)
            {
                for (int j = 0; j < ShaderParameters.Count; j++)
                {
                    ref ShaderParameter shaderParam = ref ShaderParameters.At(j);

                    foreach (var prs in pipelineResourceSet)
                    {
                        var addr = prs.GetPushConst(shaderParam.name);
                        if (addr != IntPtr.Zero)
                        {
                            shaderParam.Bind(addr);
                            break;
                        }

                    }

                }
            }

            if (TextureParameters != null)
            {
                for (int j = 0; j < TextureParameters.Count; j++)
                {
                    ref TextureParameter texParam = ref TextureParameters.At(j);
                    var tex = texParam.texture.Load<Texture>();
                    UpdateResourceSet(texParam.name, tex);
                }
            }
        }

        public ref ShaderParameter GetShaderParameter(StringID name)
        {
            if (ShaderParameters != null)
            {
                for (int i = 0; i < ShaderParameters.Count; i++)
                {
                    ref ShaderParameter param = ref ShaderParameters.At(i);
                    if (param.name == name)
                    {
                        return ref param;
                    }
                }
            }

            return ref ShaderParameter.Null;
        }

        public void SetShaderParameter<T>(StringID name, T val)
        {
            SetShaderParameter(name, ref val);
        }

        public void SetShaderParameter<T>(StringID name, ref T val)
        {
            ref ShaderParameter param = ref GetShaderParameter(name);
            if (!param.IsNull)
            {
                param.SetValue(ref val);
                param.inlineUniformBlock?.MarkDirty();
            }
            else
            {
                var shaderParam = new ShaderParameter
                {
                    name = name
                };

                shaderParam.SetValue(ref val);

                foreach (var prs in pipelineResourceSet)
                {
                    var addr = prs.GetInlineUniformMember(shaderParam.name, out var inlineUniformBlock);
                    if (addr != IntPtr.Zero)
                    {
                        shaderParam.Bind(addr);
                        shaderParam.inlineUniformBlock = inlineUniformBlock;
                        inlineUniformBlock.MarkDirty();
                        break;
                    }

                    addr = prs.GetPushConst(shaderParam.name);
                    if (addr != IntPtr.Zero)
                    {
                        shaderParam.Bind(addr);
                        break;
                    }

                }

                if (ShaderParameters == null)
                {
                    ShaderParameters = new FastList<ShaderParameter>();
                }

                ShaderParameters.Add(shaderParam);

            }
        }

        public ref TextureParameter GetTextureParameter(StringID name)
        {
            if (TextureParameters != null)
            {
                for (int i = 0; i < TextureParameters.Count; i++)
                {
                    ref TextureParameter param = ref TextureParameters.At(i);
                    if (param.name == name)
                    {
                        return ref param;
                    }
                }
            }

            return ref TextureParameter.Null;
        }

        public void SetTexture(StringID name, Texture tex)
        {
            if (TextureParameters == null)
            {
                TextureParameters = new FastList<TextureParameter>();
            }

            for (int i = 0; i < TextureParameters.Count; i++)
            {
                ref TextureParameter param = ref TextureParameters.At(i);
                if (param.name == name)
                {
                    if(tex != param.texture.resource)
                    {
                        param.texture = tex.ResourceRef;
                        UpdateResourceSet(name, tex);
                    }
                    return;
                }
            }

            TextureParameters.Add(new TextureParameter { name = name, texture = tex.ResourceRef });
            UpdateResourceSet(name, tex);
        }

        public void SetBuffer(StringID name, Buffer buf)
        {
            if (BufferParameters == null)
            {
                BufferParameters = new FastList<BufferParameter>();
            }

            for (int i = 0; i < BufferParameters.Count; i++)
            {
                ref BufferParameter param = ref BufferParameters.At(i);
                if (param.name == name)
                {
                    if (buf != param.buffer)
                    {
                        param.buffer = buf;
                        UpdateResourceSet(name, buf);
                    }
                    return;
                }
            }

            BufferParameters.Add(new BufferParameter { name = name, buffer = buf });
            UpdateResourceSet(name, buf);
        }

        void UpdateResourceSet(StringID name, IBindableResource tex)
        {
            foreach (var prs in pipelineResourceSet)
            {
                prs.UpdateResourceSet(name, tex);
            }
        }

        public void Bind(int pass, CommandBuffer cmd)
        {
            pipelineResourceSet[pass].PushConstants(cmd);
            pipelineResourceSet[pass].BindGraphicsResourceSet(cmd);
         }

        public void BindResourceSets(int pass, CommandBuffer cmd)
        {
            pipelineResourceSet[pass].BindGraphicsResourceSet(cmd);
        }

        public void PushConstants(int pass, CommandBuffer cmd)
        {
            pipelineResourceSet[pass].PushConstants(cmd);
        }

        protected override void Destroy(bool disposing)
        {
            if(pipelineResourceSet != null)
            {
                foreach (var prs in pipelineResourceSet)
                {
                    prs.Dispose();
                }

                pipelineResourceSet.Clear();
            }

            base.Destroy(disposing);

        }

    }


}
