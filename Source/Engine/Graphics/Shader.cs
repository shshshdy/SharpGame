using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using VulkanCore;

namespace SharpGame
{
    [DataContract]
    public class Shader : Resource
    {
        [DataMember]
        public StringID Name { get; set; }
        [DataMember]
        public Dictionary<StringID, Pass> Passes { get; set; } = new Dictionary<StringID, Pass>();

        public Shader()
        {
        }

        public Shader(string name, Pass pass)
        {
            Name = name;

            AddPass(pass);
        }

        public void AddPass(Pass pass)
        {
            if(pass.Name.IsNullOrEmpty)
            {
                pass.Name = Pass.main;
            }

            Passes.Add(pass.Name, pass);
        }

        public Pass this[StringID pass]
        {
            get
            {
                return Passes[pass];
            }

            set
            {
                Passes[pass] = value;
            }
        }

        [IgnoreDataMember]
        public Pass Main
        {
            get
            {
                return GetPass(Pass.main);
            }
        }

        public Pass GetPass(StringID name)
        {
            if(Passes.TryGetValue(name, out Pass pass))
            {
                return pass;
            }

            return null;
        }

        protected override void OnBuild()
        {
            var it = Passes.GetEnumerator();
            while(it.MoveNext())
            {
                it.Current.Value.Build();
            }

        }

        protected override void Destroy()
        {
            var it = Passes.GetEnumerator();
            while (it.MoveNext())
            {
                it.Current.Value.Dispose();
            }

            Passes.Clear();

            base.Dispose();
        }
    }

    public class Pass : IDisposable
    {
        private StringID name_;
        [IgnoreDataMember]
        public StringID Name { get => name_; set => name_ = value; }

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

        [IgnoreDataMember]
        public DescriptorSetLayout DescriptorSetLayout { get; set; }
        [IgnoreDataMember]
        public DescriptorPool DescriptorPool { get; set; }
        [IgnoreDataMember]
        public DescriptorSet DescriptorSet { get; set; }

        [IgnoreDataMember]
        public bool IsComputeShader => ComputeShader != null;
        private bool builded_ = false;

        public static readonly StringID shadow = "shadow";
        public static readonly StringID depth = "depth";
        public static readonly StringID clear = "clear";
        public static readonly StringID main = "main";
        
        public Pass()
        {            
        }

        public Pass(string vertexShader, string pixelShader, string geometryShader = null,
            string hullShader = null, string domainShader = null, string computeShader = null)
        {
            VertexShader = new ShaderModule(ShaderStages.Vertex, vertexShader);
            PixelShader = new ShaderModule(ShaderStages.Fragment, pixelShader);

            if (!string.IsNullOrEmpty(geometryShader))
            {
                GeometryShader = new ShaderModule(ShaderStages.Geometry, geometryShader);
            }

            if (!string.IsNullOrEmpty(hullShader))
            {
                HullShader = new ShaderModule(ShaderStages.TessellationControl, hullShader);
            }

            if (!string.IsNullOrEmpty(domainShader))
            {
                DomainShader = new ShaderModule(ShaderStages.TessellationEvaluation, domainShader);
            }

            if (!string.IsNullOrEmpty(computeShader))
            {
                ComputeShader = new ShaderModule(ShaderStages.Compute, computeShader);
            }

            Build();
        }
        
        public Pass(string name, params ShaderModule[] shaderModules)
        {
            foreach(var sm in shaderModules)
            {
                switch (sm.Stage)
                {
                    case ShaderStages.Vertex:
                        VertexShader = sm;
                        break;
                    case ShaderStages.Fragment:
                        PixelShader = sm;
                        break;
                    case ShaderStages.Geometry:
                        GeometryShader = sm;
                        break;
                    case ShaderStages.TessellationControl:
                        HullShader = sm;
                        break;
                    case ShaderStages.TessellationEvaluation:
                        DomainShader = sm;
                        break;
                    case ShaderStages.Compute:
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

        public void Dispose()
        {
            foreach(var stage in this.GetShaderModules())
            {
                stage?.Dispose();
            }

        }

        public PipelineShaderStageCreateInfo[] GetShaderStageCreateInfos()
        {
            var shaderStageCreateInfo = new List<PipelineShaderStageCreateInfo>();
            foreach(var sm in GetShaderModules())
            {
                if(sm != null)
                {
                    var shaderStage = new PipelineShaderStageCreateInfo(sm.Stage,
                        sm.shaderModule, sm.FuncName);
                    shaderStageCreateInfo.Add(shaderStage);
                }
            }
            return shaderStageCreateInfo.ToArray();
        }

        public PipelineShaderStageCreateInfo GetComputeStageCreateInfo()
        {
            if(ComputeShader != null)
            {
                return new PipelineShaderStageCreateInfo(ShaderStages.Compute, ComputeShader.shaderModule, ComputeShader.FuncName);
            }

            return default;
        }
    }
    

}
