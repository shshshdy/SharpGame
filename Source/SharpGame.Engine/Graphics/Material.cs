using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using Vulkan;

namespace SharpGame
{
    public enum ShadingMode
    {
        Unlit,
        Default,
        Pbr,
        Toon,
        Custom
    }


    public class Material : Resource
    {
        public ResourceRef ShaderResource { get; set; }
        public ShadingMode ShadingMode { get; set; } = ShadingMode.Default;
        public FastList<ShaderParameter> ShaderParameters { get; set; }
        public FastList<TextureParameter> TextureParameters { get; set; }
        public FastList<BufferParameter> BufferParameters { get; set; }

        public bool AlphaBlend { get; set; }
        public bool AlphaTest { get; set; }

        [IgnoreDataMember]
        public Shader Shader { get; set; }


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
            Shader = shader;

            OnBuild();
        }

        protected override bool OnBuild()
        {
            if(ShaderResource != null)
            {
                Shader = Resources.Instance.Load<Shader>(ShaderResource);
            }

            if(Shader == null)
            {
                return false;
            }

            if(Shader.Pass.Count <= 0)
            {
                return false;
            }

            pipelineResourceSet = new PipelineResourceSet[Shader.Pass.Count];

            for(int i = 0; i < Shader.Pass.Count; i++)
            {
                var pass = Shader.Pass[i];
                pipelineResourceSet[i] = new PipelineResourceSet();
                pipelineResourceSet[i].Init(pass.PipelineLayout);
            }

            if (ShaderParameters != null)
            {
                for (int j = 0; j < ShaderParameters.Count; j++)
                {
                    ref ShaderParameter shaderParam = ref ShaderParameters.At(j);

                    foreach(var prs in pipelineResourceSet )
                    {
                        var addr = prs.GetPushConst(shaderParam.name);
                        if(addr != IntPtr.Zero)
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
                    var res = texParam.texture.Load();
                    UpdateResourceSet(texParam.name, res as Texture);
                }
            }

            return true;
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
                    var addr = prs.GetPushConst(shaderParam.name);
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

        public void SetTexture(StringID name, ResourceRef texRef)
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
                    param.texture = texRef;
                    UpdateResourceSet(name, (Texture)texRef.resource);
                    break;
                }
            }

            TextureParameters.Add(new TextureParameter { name = name, texture = texRef });
            UpdateResourceSet(name, (Texture)texRef.resource);
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
                    if(tex.ResourceRef != param.texture)
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

        protected override void Destroy()
        {
            base.Destroy();

            foreach (var prs in pipelineResourceSet)
            {
                prs.Dispose();
            }

            pipelineResourceSet.Clear();

        }

    }


}
