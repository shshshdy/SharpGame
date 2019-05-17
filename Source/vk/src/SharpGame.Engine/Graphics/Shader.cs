using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Vulkan;

namespace SharpGame
{
    using static Builder;

    [DataContract]
    public class Shader : Resource
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<ShaderParameter> Properties { get; set; }

        [DataMember]
        public List<Pass> Passes { get; set; } = new List<Pass>();

        public Shader()
        {
        }

        public Shader(params Pass[] passes)
        {
            foreach(var pass in passes)
            {
                AddPass(pass);
            }
        }

        public void AddPass(Pass pass)
        {
            Passes.Add(pass);
        }

        [IgnoreDataMember]
        public Pass Main
        {
            get
            {
                return GetPass(0);
            }
        }

        public Pass GetPass(int id)
        {
            foreach (var pass in Passes)
            {
                if (pass.passID == id)
                {
                    return pass;
                }
            }

            return null;
        }

        public Pass GetPass(StringID name)
        {
            foreach(var pass in Passes)
            {
                if(pass.Name == name)
                {
                    return pass;
                }
            }

            return null;
        }

        protected override void OnBuild()
        {
            foreach (var pass in Passes)
            {
                pass.Build();
            }
        }

        protected override void Destroy()
        {
            foreach (var pass in Passes)
            {
                pass.Dispose();
            }

            Passes.Clear();

            base.Destroy();
        }
    }

    public class Pass : DisposeBase
    {
        public static readonly StringID Shadow = "shadow";
        public static readonly StringID Depth = "depth";
        public static readonly StringID Clear = "clear";
        public static readonly StringID Main = "main";

        private StringID name_;
        public StringID Name
        {
            get => name_;
            set
            {
                name_ = value;
                passID = GetID(value);
            }
        }

        [IgnoreDataMember]
        public int passID;

        [DataMember]
        public ShaderModule VertexShader { get; set; }
        [DataMember]
        public ShaderModule GeometryShader { get; set; }
        [DataMember]
        public ShaderModule PixelShader { get; set; }
        [DataMember]
        public ShaderModule HullShader { get; set; }
        [DataMember]
        public ShaderModule DomainShader { get; set; }
        [DataMember]
        public ShaderModule ComputeShader { get; set; }

        //[IgnoreDataMember]
        public ResourceLayout ResourceLayout { get; set; }

        [IgnoreDataMember]
        public bool IsComputeShader => ComputeShader != null;
        private bool builded_ = false;

        static List<StringID> passList = new List<StringID>();
        static Pass()
        {
            passList.Add(Main);
        }

        public static int GetID(StringID pass)
        {
            if(pass.IsNullOrEmpty)
            {
                return 0;
            }

            for(int i = 0; i < passList.Count; i++)
            {
                if(passList[i] == pass)
                {
                    return i;
                }
            }
            passList.Add(pass);
            return passList.Count - 1;
        }

        public Pass()
        {            
        }

        public Pass(string vertexShader = null, string pixelShader = null, string geometryShader = null,
            string hullShader = null, string domainShader = null, string computeShader = null)
        {
            if (!string.IsNullOrEmpty(vertexShader))
            {
                VertexShader = new ShaderModule(VkShaderStageFlags.Vertex, vertexShader);
            }

            if (!string.IsNullOrEmpty(pixelShader))
            {
                PixelShader = new ShaderModule(VkShaderStageFlags.Fragment, pixelShader);
            }

            if (!string.IsNullOrEmpty(geometryShader))
            {
                GeometryShader = new ShaderModule(VkShaderStageFlags.Geometry, geometryShader);
            }

            if (!string.IsNullOrEmpty(hullShader))
            {
                HullShader = new ShaderModule(VkShaderStageFlags.TessellationControl, hullShader);
            }

            if (!string.IsNullOrEmpty(domainShader))
            {
                DomainShader = new ShaderModule(VkShaderStageFlags.TessellationEvaluation, domainShader);
            }

            if (!string.IsNullOrEmpty(computeShader))
            {
                ComputeShader = new ShaderModule(VkShaderStageFlags.Compute, computeShader);
            }

            Build();
        }
        
        public Pass(string name, params ShaderModule[] shaderModules)
        {
            foreach(var sm in shaderModules)
            {
                switch (sm.Stage)
                {
                    case VkShaderStageFlags.Vertex:
                        VertexShader = sm;
                        break;
                    case VkShaderStageFlags.Fragment:
                        PixelShader = sm;
                        break;
                    case VkShaderStageFlags.Geometry:
                        GeometryShader = sm;
                        break;
                    case VkShaderStageFlags.TessellationControl:
                        HullShader = sm;
                        break;
                    case VkShaderStageFlags.TessellationEvaluation:
                        DomainShader = sm;
                        break;
                    case VkShaderStageFlags.Compute:
                        ComputeShader = sm;
                        break;

                }
            }

            Build();
        }

        public IEnumerable<ShaderModule> GetShaderModules()
        {
            yield return VertexShader;
            yield return GeometryShader;
            yield return PixelShader;
            yield return HullShader;
            yield return DomainShader;
            yield return ComputeShader;
        }

        public void Build()
        {
            if(builded_)
            {
                return;
            }

            builded_ = true;

            VertexShader?.Build();
            GeometryShader?.Build();
            PixelShader?.Build();
            HullShader?.Build();
            DomainShader?.Build();
            ComputeShader?.Build();
        }

        protected override void Destroy()
        {
            foreach(var stage in this.GetShaderModules())
            {
                stage?.Dispose();
            }

        }

        public unsafe uint GetShaderStageCreateInfos(VkPipelineShaderStageCreateInfo* shaderStageCreateInfo)
        {
            uint count = 0;
            foreach(var sm in GetShaderModules())
            {
                if(sm != null)
                {
                    var shaderStage = VkPipelineShaderStageCreateInfo.New();
                    shaderStage.stage = sm.Stage;
                    shaderStage.module = sm.shaderModule;
                    shaderStage.pName = Strings.main;// sm.FuncName;
                    shaderStageCreateInfo[count++] = shaderStage;
                }
            }
            return count;
        }

        public unsafe VkPipelineShaderStageCreateInfo GetComputeStageCreateInfo()
        {
            if(ComputeShader != null)
            {
                var shaderStage = VkPipelineShaderStageCreateInfo.New();
                shaderStage.stage = VkShaderStageFlags.Compute;
                shaderStage.module = ComputeShader.shaderModule;
                shaderStage.pName = Strings.main;// sm.FuncName;
                return shaderStage;
            }

            return default;
        }
    }
    

}
