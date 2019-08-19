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

        private List<ResourceSet> resourceSet = new List<ResourceSet>();
        [IgnoreDataMember]
        public List<ResourceSet> ResourceSet { get => resourceSet; set => resourceSet = value; }

        [IgnoreDataMember]
        public Shader Shader { get; set; }

        IntPtr pushConstBuffer;
        int minPushConstRange = 1000;
        int maxPushConstRange = 0;
        int maxPushConstantsSize;

        public Material()
        {
            maxPushConstantsSize = (int)Device.Properties.limits.maxPushConstantsSize;
            pushConstBuffer = Utilities.Alloc(maxPushConstantsSize);
        }

        public Material(string shader)
        {
            maxPushConstantsSize = (int)Device.Properties.limits.maxPushConstantsSize;
            pushConstBuffer = Utilities.Alloc(maxPushConstantsSize);
            ShaderResource = ResourceRef.Create<Shader>(shader);

            OnBuild();
        }

        public Material(Shader shader)
        {
            maxPushConstantsSize = (int)Device.Properties.limits.maxPushConstantsSize;
            pushConstBuffer = Utilities.Alloc(maxPushConstantsSize);
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

            var mainPass = Shader.Main;
            if(mainPass == null)
            {
                return false;
            }

            foreach (var layout in mainPass.PipelineLayout.ResourceLayout)
            {
                //if (layout.PerMaterial)
                {
                    resourceSet.Add(new ResourceSet(layout));
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

            if(mainPass.PushConstantNames != null)
            {
                for (int i = 0; i < mainPass.PushConstantNames.Count; i++)
                {
                    var constName = mainPass.PushConstantNames[i];
                    var pushConst = mainPass.PipelineLayout.PushConstant[i];
                    if(pushConst.offset + pushConst.size > maxPushConstantsSize)
                    {
                        Log.Error("PushConst out of range" + constName);
                        continue;
                    }

                    if(pushConst.offset < minPushConstRange)
                    {
                        minPushConstRange = pushConst.offset;
                    }

                    if(pushConst.offset + pushConst.size > maxPushConstRange)
                    {
                        maxPushConstRange = pushConst.offset + pushConst.size;
                    }

                    if (ShaderParameters != null)
                    {
                        for (int j = 0; j < ShaderParameters.Count; j++)
                        {
                            ref ShaderParameter shaderParam = ref ShaderParameters.At(j);
                            if (shaderParam.name == constName)
                            {
                                shaderParam.Bind(pushConstBuffer + pushConst.offset);
                                break;
                            }

                        }
                    }
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

                var mainPass = Shader.Main;
                if (mainPass != null)
                {
                    if(mainPass.GetPushConstant(name, out var pushConst))
                    {
                        shaderParam.Bind(pushConstBuffer + pushConst.offset);
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

        public void SetBuffer(StringID name, DeviceBuffer buf)
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
            foreach (var rs in resourceSet)
            {
                foreach (var binding in rs.resourceLayout.Bindings)
                {
                    if (binding.name == name)
                    {
                        rs.Bind(binding.binding, tex);
                        rs.UpdateSets();
                        return;
                    }
                }
            }
        }

        public void PushConstants(PipelineLayout pipelineLayout, CommandBuffer cmd)
        {
            int size = maxPushConstRange - minPushConstRange;
            if (size > 0)
            {
                ShaderStage shaderStage = ShaderStage.None;
                int minRange = minPushConstRange;
                int currentSize = 0;
                for (int i = 0; i < pipelineLayout.PushConstant.Length; i++ )
                {
                    if(i == 0)
                    {
                        shaderStage = pipelineLayout.PushConstant[0].stageFlags;
                    }

                    currentSize += pipelineLayout.PushConstant[i].size;

                    if ((pipelineLayout.PushConstant[i].stageFlags != shaderStage) || (i == pipelineLayout.PushConstant.Length - 1))
                    {
                        cmd.PushConstants(pipelineLayout, shaderStage, minRange, currentSize, pushConstBuffer + minRange);

                        shaderStage = pipelineLayout.PushConstant[i].stageFlags;
                        minRange += currentSize;
                        currentSize = 0;
                    }
                }

            }
        }

        protected override void Destroy()
        {
            base.Destroy();

            Utilities.Free(pushConstBuffer);
        }

    }

}
