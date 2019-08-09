using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Vulkan;

namespace SharpGame
{

    public class Material : Resource
    {
        public ResourceRef ShaderName { get; set; }

        public FastList<ShaderParameter> ShaderParameters { get; set; } = new FastList<ShaderParameter>();
        public FastList<TexureParameter> TextureParameters { get; set; } = new FastList<TexureParameter>();
        public FastList<BufferParameter> BufferParameters { get; set; } = new FastList<BufferParameter>();

        private List<ResourceSet> resourceSet = new List<ResourceSet>();
        public List<ResourceSet> ResourceSet { get => resourceSet; set => resourceSet = value; }

        public Shader Shader { get; set; }
        IntPtr pushConstBuffer;
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
            ShaderName = ResourceRef.Create<Shader>(shader);

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
            if(ShaderName != null)
            {
                Shader = Resources.Instance.Load<Shader>(ShaderName);
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

            foreach (var layout in mainPass.ResourceLayout)
            {
                if (layout.Dynamic)
                {
                    resourceSet.Add(new ResourceSet(layout));
                }

            }

            if(mainPass.PushConstantNames != null)
            {
                for (int i = 0; i < mainPass.PushConstantNames.Count; i++)
                {
                    var pushConst = mainPass.PushConstant[i];
                    if(pushConst.offset + pushConst.size > maxPushConstantsSize)
                    {
                        Log.Error("PushConst out of range" + mainPass.PushConstantNames[i]);
                        continue;
                    }


                }

            }

            return true;
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
                    UpdateResourceSet(name, (Texture)texRef.resource);
                    break;
                }
            }

            TextureParameters.Add(new TexureParameter { name = name, texture = texRef });
            UpdateResourceSet(name, (Texture)texRef.resource);
        }

        public void SetTexture(StringID name, Texture tex)
        {
            for (int i = 0; i < TextureParameters.Count; i++)
            {
                ref TexureParameter param = ref TextureParameters.At(i);
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

            TextureParameters.Add(new TexureParameter { name = name, texture = tex.ResourceRef });
            UpdateResourceSet(name, tex);
        }

        public void SetBuffer(StringID name, DeviceBuffer buf)
        {
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

        protected override void Destroy()
        {
            base.Destroy();

            Utilities.Free(pushConstBuffer);
        }

    }

}
